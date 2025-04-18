using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Channels;


namespace MQTTnet.Agent;

/// <summary>
/// 基于 MQTT 的消息代理
/// </summary>
internal class MqttClientMessageAgent : MqttClientMessagePublisher, IMessageAgent {
    private readonly IMqttClient client;
    private readonly JsonSerializerOptions? serializerOptions;
    private readonly ILogger<MqttClientMessageAgent> logger;
    private readonly Queue<Action> completeActions = new Queue<Action>();
    private const int DefaultChannelCapacity = 10;

    public MqttClientMessageAgent(IMqttClient client, IOptions<JsonOptions>? jsonOptions, ILogger<MqttClientMessageAgent> logger) : base(client, jsonOptions, logger) {
        this.client = client;
        this.serializerOptions = jsonOptions?.Value.SerializerOptions;
        this.logger = logger;
    }

    private Regex BuildTopicPattern(string topic) {
        var pattern = topic
                        .Replace("/", "\\/")
                        .Replace("+", "[^/]+")
                        .Replace("#", "(.+)");
        logger.LogTrace("build topic match pattern '{topic}' => '{pattern}'", topic, pattern);
        return new Regex(pattern, RegexOptions.Compiled);
    }

    private Channel<MessageArgs<T>> BuildChannel<T>(string topic, JsonTypeInfo<T>? typeInfo = null, int capacity = DefaultChannelCapacity) where T : class {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        return BuildChannel<T>(topic, channel, typeInfo, capacity);
    }

    private Channel<MessageArgs<T>> BuildChannel<T>(string topic, Channel<MessageArgs<T>> channel, JsonTypeInfo<T>? typeInfo = null, int capacity = DefaultChannelCapacity) where T : class {
        var pattern = BuildTopicPattern(topic);
        var convert = typeInfo != null ? typeInfo.GetDeserializer<T>() : serializerOptions.GetDeserializer<T>();
        client.ApplicationMessageReceivedAsync += async (args) => {
            var msg = args.ApplicationMessage;
            if (!pattern.IsMatch(topic)) {
                return;
            }
            try {
                await channel.Writer.WriteAsync(new MessageArgs<T>() {
                    Topic = msg.Topic,
                    Payload = msg.Payload.Length == 0 ? null : convert(msg.Payload.ToArray())
                });
            } catch (Exception ex) {
                logger.LogWarning(ex, "解析 {topic} 消息发生异常,{msg}", msg.Topic, ex.Message);
                logger.LogTrace("topic:'{topic}' payload:{payload}", msg.Topic, msg.Payload);
            }
        };
        completeActions.Enqueue(() => channel.Writer.Complete());
        return channel;
    }

    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, JsonTypeInfo<T>? typeInfo = null, CancellationToken cancellationToken = default) where T : class {
        var channel = BuildChannel<T>(topic);
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        return channel.Reader;
    }

    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string[] topics, JsonTypeInfo<T>? typeInfo = null, CancellationToken cancellationToken = default) where T : class {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        foreach (var topic in topics) {
            BuildChannel<T>(topic, channel, typeInfo);
            var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
            logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        }
        return channel.Reader;
    }

    public void Dispose() {
        client.Dispose();
        while (completeActions.Any()) {
            completeActions.Dequeue()();
        }
    }
}

internal readonly struct TokenOf<T> { }

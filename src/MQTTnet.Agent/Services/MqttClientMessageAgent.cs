using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;


namespace MQTTnet.Agent;

/// <summary>
/// 基于 MQTT 的消息代理
/// </summary>
internal class MqttClientMessageAgent : MqttClientMessagePublisher, IMessageAgent {
    private readonly IMqttClient client;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly ILogger<MqttClientMessageAgent> logger;
    private const int DefaultChannelCapacity = 10;

    public MqttClientMessageAgent(IMqttClient client, IOptions<JsonOptions> jsonOptions, ILogger<MqttClientMessageAgent> logger):base(client,jsonOptions,logger) {
        this.client = client;
        this.serializerOptions = jsonOptions.Value.SerializerOptions;
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

    private Channel<MessageArgs<T>> BuildChannel<T>(string topic, int capacity = DefaultChannelCapacity) where T : class {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        var pattern = BuildTopicPattern(topic);
        var convert = GetDeserializer<T>();
        client.ApplicationMessageReceivedAsync += async (args) => {
            var msg = args.ApplicationMessage;
            if (!pattern.IsMatch(topic)) {
                return;
            }
            await channel.Writer.WriteAsync(new MessageArgs<T>() {
                Topic = msg.Topic,
                Payload = msg.Payload == null ? null : convert(msg.Payload)
            });
        };
        return channel;
    }

    private Func<byte[], T?> GetDeserializer<T>() where T : class {
        var token = new TokenOf<T>();
        switch (token) {
            case TokenOf<string>:
                return p => {
                    return Encoding.UTF8.GetString(p) as T;
                };
            case TokenOf<byte[]>:
                return p => p as T;
            default:
                return payload => JsonSerializer.Deserialize<T>(payload, serializerOptions);
        }
    }

    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class {
        var channel = BuildChannel<T>(topic);
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, result.Items.First());
        return channel.Reader;
    }

    public void Dispose() {
        client.Dispose();
    }
}

internal readonly struct TokenOf<T> { }
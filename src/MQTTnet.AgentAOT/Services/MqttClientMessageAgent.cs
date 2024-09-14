using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using System.Text.RegularExpressions;
using System.Threading.Channels;


namespace MQTTnet.Agent;

/// <summary>
/// 基于 MQTT 的消息代理
/// </summary>
internal partial class MqttClientMessageAgent : MqttClientMessagePublisher, IMessageAgent {
    private readonly IMqttClient client;
    private readonly ILogger<MqttClientMessageAgent> logger;

    private readonly Queue<Action> completeActions = new Queue<Action>();
    private const int DefaultChannelCapacity = 10;

    public MqttClientMessageAgent(IMqttClient client, ILogger<MqttClientMessageAgent> logger) : base(client, logger) {
        this.client = client;
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

    public void Dispose() {
        client.Dispose();
        while (completeActions.Any()) {
            completeActions.Dequeue()();
        }
    }

    public async Task<ChannelReader<MessageArgs<ArraySegment<byte>>>> GetChannelAsync(string topic, CancellationToken cancellationToken = default) {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<ArraySegment<byte>>>(DefaultChannelCapacity);
        BuildChannel(topic, channel);
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        return channel.Reader;
    }

    private Channel<MessageArgs<ArraySegment<byte>>> BuildChannel(string topic, Channel<MessageArgs<ArraySegment<byte>>> channel, int capacity = DefaultChannelCapacity) {
        var pattern = BuildTopicPattern(topic);
        client.ApplicationMessageReceivedAsync += async (args) => {
            var msg = args.ApplicationMessage;
            if (!pattern.IsMatch(topic)) {
                return;
            }
            try {
                await channel.Writer.WriteAsync(new MessageArgs<ArraySegment<byte>>() {
                    Topic = msg.Topic,
                    Payload = msg.PayloadSegment.Count == 0 ? default : msg.PayloadSegment
                });
            } catch (Exception ex) {
                logger.LogWarning(ex, "解析 {topic} 消息发生异常,{msg}", msg.Topic, ex.Message);
                logger.LogTrace("topic:'{topic}' payload:{payload}", msg.Topic, msg.PayloadSegment);
            }
        };
        completeActions.Enqueue(() => channel.Writer.Complete());
        return channel;
    }

}

internal readonly struct TokenOf<T> { }

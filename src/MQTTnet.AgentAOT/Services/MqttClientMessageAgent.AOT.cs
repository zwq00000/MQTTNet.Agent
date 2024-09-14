using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Channels;

namespace MQTTnet.Agent;

/// <summary>
/// 基于 MQTT 的消息代理
/// </summary>
internal partial class MqttClientMessageAgent {

    private Channel<MessageArgs<T>> BuildChannel<T>(string topic, Channel<MessageArgs<T>> channel, JsonTypeInfo<T> typeInfo, int capacity = DefaultChannelCapacity)  {
        var pattern = BuildTopicPattern(topic);
        var convert = typeInfo.GetConverter<T>();
        client.ApplicationMessageReceivedAsync += async (args) => {
            var msg = args.ApplicationMessage;
            if (!pattern.IsMatch(topic)) {
                return;
            }
            try {
                await channel.Writer.WriteAsync(new MessageArgs<T>() {
                    Topic = msg.Topic,
                    Payload = (T?)(msg.PayloadSegment.Count == 0 ? default : convert(msg.PayloadSegment.Array!))
                });
            } catch (Exception ex) {
                logger.LogWarning(ex, "解析 {topic} 消息发生异常,{msg}", msg.Topic, ex.Message);
                logger.LogTrace("topic:'{topic}' payload:{payload}", msg.Topic, msg.PayloadSegment);
            }
        };
        completeActions.Enqueue(() => channel.Writer.Complete());
        return channel;
    }

    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken = default) {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        BuildChannel<T>(topic, channel, typeInfo);
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        return channel.Reader;
    }

    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string[] topics, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken = default) {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        foreach (var topic in topics) {
            BuildChannel<T>(topic, channel, typeInfo);
            var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
            logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        }
        return channel.Reader;
    }
}
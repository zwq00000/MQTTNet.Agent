using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MqttClientMessageAgent> logger;

    private readonly Queue<Action> completeActions = new Queue<Action>();
    private const int DefaultChannelCapacity = 10;

    public MqttClientMessageAgent(IMqttClient client, ILogger<MqttClientMessageAgent> logger) : base(client, logger) {
        this.client = client;
        this.logger = logger;
    }

    /// <summary>
    /// 构建主题匹配模式
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    private Regex BuildTopicPattern(string topic) {
        var pattern = topic
                        .Replace("/", "\\/")
                        .Replace("+", "[^/]+")
                        .Replace("#", "(.+)");
        logger.LogTrace("build topic match pattern '{topic}' => '{pattern}'", topic, pattern);
        return new Regex(pattern, RegexOptions.Compiled);
    }

    /// <summary>
    /// 构建消息接收通道
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">主题</param>
    /// <param name="channel">消息通道</param>
    /// <param name="convert">消息转换函数</param>
    /// <param name="capacity">通道容量</param>
    /// <returns></returns>
    private Channel<MessageArgs<T>> BuildChannel<T>(string topic, Channel<MessageArgs<T>> channel, Func<ReadOnlySequence<byte>, T?> convert, int capacity = DefaultChannelCapacity) {
        var pattern = BuildTopicPattern(topic);
        client.ApplicationMessageReceivedAsync += async (args) => {
            var msg = args.ApplicationMessage;
            if (!pattern.IsMatch(msg.Topic)) {
                return;
            }
            try {
                await channel.Writer.WriteAsync(new MessageArgs<T>() {
                    Topic = msg.Topic,
                    Payload = msg.Payload.Length == 0 ? default : convert(msg.Payload)
                });
            } catch (Exception ex) {
                logger.LogWarning(ex, "解析 {topic} 消息发生异常,{msg}", msg.Topic, ex.Message);
                logger.LogTrace("topic:'{topic}' payload:{payload}", msg.Topic, msg.Payload);
            }
        };
        completeActions.Enqueue(() => channel.Writer.Complete());
        return channel;
    }

    /// <summary>
    /// 获取消息通道
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="options">Json 序列化选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, JsonSerializerOptions options, CancellationToken cancellationToken = default) where T : class {
        var convert = options.GetDeserializer<T>();
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        BuildChannel<T>(topic, channel, convert);
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        return channel.Reader;
    }

    /// <summary>
    /// 获取消息通道
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topics">主题数组</param>
    /// <param name="options">Json 序列化选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string[] topics, JsonSerializerOptions options, CancellationToken cancellationToken = default) where T : class {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        var convert = options.GetDeserializer<T>();
        foreach (var topic in topics) {
            BuildChannel<T>(topic, channel, convert);
            var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
            logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        }
        return channel.Reader;
    }

    public void Dispose() {
        client.Dispose();
        while (completeActions.Count != 0) {
            completeActions.Dequeue()();
        }
    }

    /// <summary>
    /// 获取消息通道
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="jsonTypeInfo">Json 类型信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default) where T : class {
        var convert = jsonTypeInfo.GetDeserializer<T>();
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        BuildChannel<T>(topic, channel, convert);
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        return channel.Reader;
    }

    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string[] topics, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default) where T : class {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        var convert = jsonTypeInfo.GetDeserializer<T>();
        foreach (var topic in topics) {
            BuildChannel<T>(topic, channel, convert);
            var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
            logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        }
        return channel.Reader;
    }

    /// <summary>
    /// 获取消息通道
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topics">主题数组</param>
    /// <param name="convert">消息转换函数</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string[] topics, Func<ReadOnlySequence<byte>, T?> convert, CancellationToken cancellationToken = default) {
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        foreach (var topic in topics) {
            BuildChannel<T>(topic, channel, convert);
            var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
            logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        }
        return channel.Reader;
    }

    /// <summary>
    /// 获取消息通道
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">订阅主题</param>
    /// <param name="convert">消息转换函数</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, Func<ReadOnlySequence<byte>, T?> convert, CancellationToken cancellationToken = default) {
        if (convert is null) {
            throw new ArgumentNullException(nameof(convert));
        }
        var channel = System.Threading.Channels.Channel.CreateBounded<MessageArgs<T>>(DefaultChannelCapacity);
        BuildChannel<T>(topic, channel, convert);
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, string.Join(',', result.Items.Select(r => r.ResultCode)));
        return channel.Reader;
    }

}

internal readonly struct TokenOf<T> { }

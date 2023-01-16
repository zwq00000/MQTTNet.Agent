#if TEST
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace MQTTnet.Agent;

/// <summary>
/// MQTT client 扩展方法
/// </summary>
public static class MqttClientExtensions {

    /// <summary>
    /// 发布数据对象为 Json数据格式
    /// 序列化方法为 <see cref="JsonSerializer.SerializeToUtf8Bytes{TValue}(TValue, JsonSerializerOptions?)"/>
    /// </summary>
    /// <param name="mqttClient"></param>
    /// <param name="topic">消息主题</param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="qualityOfServiceLevel"></param>
    /// <param name="retain"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<MqttClientPublishResult> PublishJsonAsync<T>(this IMqttClient mqttClient,
                                                                          string topic,
                                                                          T payload,
                                                                          JsonSerializerOptions? options = null,
                                                                          MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                                                                          bool retain = false,
                                                                          CancellationToken cancellationToken = default(CancellationToken)) {
        if (mqttClient == null) {
            throw new ArgumentNullException(nameof(mqttClient));
        }
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes<T>(payload, options ?? defaultJsonOptions);
        var msg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(bytes)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(qualityOfServiceLevel)
            .WithContentType("application/json")
            .Build();
        return await mqttClient.PublishAsync(msg, cancellationToken);
    }

    /// <summary>
    /// 发布 对象为 Json数据格式
    /// 序列化方法为 <see cref="JsonSerializer.SerializeToUtf8Bytes(object?, Type, JsonSerializerOptions?)"/>
    /// </summary>
    /// <param name="mqttClient"></param>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="qualityOfServiceLevel"></param>
    /// <param name="retain"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<MqttClientPublishResult> PublishJsonAsync(this IMqttClient mqttClient,
                                                                       string topic,
                                                                       object payload,
                                                                       JsonSerializerOptions? options = null,
                                                                       MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                                                                       bool retain = false,
                                                                       CancellationToken cancellationToken = default(CancellationToken)) {
        if (mqttClient == null) {
            throw new ArgumentNullException(nameof(mqttClient));
        }
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), options ?? defaultJsonOptions);
        var msg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(bytes)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(qualityOfServiceLevel)
            .WithContentType("application/json")
            .Build();
        return await mqttClient.PublishAsync(msg, cancellationToken);
    }

    /// <summary>
    /// 订阅消息 并返回 <see cref="ChannelReader{T}"/>
    /// </summary>
    /// <typeparam name="T?"></typeparam>
    /// <param name="client">MQTT 客户端</param>
    /// <param name="topic">订阅主题,默认此主题下消息类型相同</param>
    /// <param name="capacity">通道容量</param>
    public static async Task<ChannelReader<Message<T>>> SubscribeChannelAsync<T>(this IMqttClient client,
                                                                                 string topic,
                                                                                 int capacity = 10) where T : class {
        var channel = Channel.CreateBounded<Message<T>>(capacity);
        Func<byte[], T?> convert = GetConverter<T>();
        client.ApplicationMessageReceivedAsync += async (arg) => {
            var msg = arg.ApplicationMessage;
            await channel.Writer.WriteAsync(new Message<T>() {
                Topic = msg.Topic,
                Payload = msg.Payload == null ? null : JsonSerializer.Deserialize<T>(msg.Payload, defaultJsonOptions)
            });
        };
        var result = await client.SubscribeAsync(topic);
        return channel.Reader;
    }

    // wildcard

    /// <summary>
    /// 订阅消息 并返回 <see cref="ChannelReader{T}"/>,支持通配符,支持多次订阅
    /// </summary>
    /// <typeparam name="T?"></typeparam>
    /// <param name="client">MQTT 客户端</param>
    /// <param name="rootTopic">顶级订阅主题,默认此主题下消息类型相同</param>
    /// <param name="wildcard">通配符 {rootTopic}/{wildcard}</param>
    /// <param name="capacity">通道容量</param>
    public static async Task<ChannelReader<Message<T>>> SubscribeChannelAsync<T>(this IMqttClient client,
                                                                                 string rootTopic,
                                                                                 string wildcard,
                                                                                 int capacity = 10) where T : class {
        var channel = Channel.CreateBounded<Message<T>>(capacity);
        Func<byte[], T?> convert = GetConverter<T>();
        client.ApplicationMessageReceivedAsync += async (arg) => {
            var msg = arg.ApplicationMessage;
            if (!msg.Topic.StartsWith(rootTopic)) {
                return;
            }
            if (msg.Payload == null) {
                await channel.Writer.WriteAsync(new Message<T>() { Topic = rootTopic });
            } else {
                await channel.Writer.WriteAsync(new Message<T>() {
                    Topic = rootTopic,
                    Payload = convert(msg.Payload)
                });
            }
        };
        var result = await client.SubscribeAsync($"{rootTopic}/{wildcard}");
        return channel.Reader;
    }

    private static Func<byte[], T?> GetConverter<T>() where T : class {
        var token = new TokenOf<T>();
        switch (token) {
            case TokenOf<string>:
                return p => {
                    return Encoding.UTF8.GetString(p) as T;
                };
            default:
                return payload => JsonSerializer.Deserialize<T>(payload, defaultJsonOptions);
        }
    }

    struct TokenOf<T> { }
}

public readonly struct Message<T> {

    /// <summary>
    /// 主题
    /// </summary>
    /// <value></value>
    public string Topic { get; init; }

    /// <summary>
    /// 有效载荷
    /// </summary>
    /// <value></value>

    public T? Payload { get; init; }
}
#endif
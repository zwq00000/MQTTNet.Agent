using System.Text.Json.Serialization.Metadata;
using System.Threading.Channels;

namespace MQTTnet.Agent;
/// <summary>
/// MQTT 消息读取 Channel
/// </summary>
public interface IMessageReader {

    /// <summary>
    /// 订阅原始消息
    /// </summary>
    /// <param name="topic">订阅主题</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChannelReader<MessageArgs<ArraySegment<byte>>>> GetChannelAsync(string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">订阅主题</param>
    /// <param name="typeInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅多个主题
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topics">订阅主题</param>
    /// <param name="typeInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string[] topics, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken = default);
}

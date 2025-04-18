using System.Text.Json.Serialization.Metadata;
using System.Threading.Channels;

namespace MQTTnet.Agent;
/// <summary>
/// MQTT 消息读取 Channel
/// </summary>
public interface IMessageReader {

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="topic">订阅主题</param>
    /// <param name="typeInfo"></param>
    /// <param name="cancellationToken"></param>
    Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, JsonTypeInfo<T>? typeInfo = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 多主题订阅
    /// </summary>
    /// <typeparam name="T"></typeparam>    
    /// <param name="topics">相同消息类型的多个订阅主题</param>
    /// <param name="typeInfo"></param>
    /// <param name="cancellationToken"></param>
    Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string[] topics, JsonTypeInfo<T>? typeInfo = null, CancellationToken cancellationToken = default) where T : class;
}

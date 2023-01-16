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
    /// <param name="cancellationToken"></param>
    Task<ChannelReader<MessageArgs<T>>> GetChannelAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class;
}

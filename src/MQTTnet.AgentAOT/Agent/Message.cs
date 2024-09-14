
namespace MQTTnet.Agent;


/// <summary>
/// 泛型消息
/// </summary>
/// <value></value>
public record Message<T> {
    public Message() {
        Id = SequentialGuid.NewGuid();
        Time = DateTime.Now;
    }

    public Message(string source, T payload) : this() {
        Source = source;
        Payload = payload;
    }

    /// <summary>
    /// 消息ID
    /// </summary>
    /// <value></value>
    public Guid Id { get; init; }

    /// <summary>
    /// 发布时间
    /// </summary>
    /// <value></value>
    public DateTime Time { get; init; }

    /// <summary>
    /// 消息来源
    /// </summary>
    /// <value></value>
    public string Source { get; init; }

    /// <summary>
    /// 消息体
    /// </summary>
    /// <value></value>
    public T Payload { get; init; }
}
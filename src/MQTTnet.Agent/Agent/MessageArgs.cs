namespace MQTTnet.Agent;

/// <summary>
/// 消息传递参数
/// </summary>
/// <typeparam name="T">消息载荷类型</typeparam>
public readonly struct MessageArgs<T> {

    /// <summary>
    /// Profile 主题
    /// </summary>
    /// <value></value>
    public string Topic { get; init; }

    /// <summary>
    /// 设备配置
    /// </summary>
    /// <value></value>
    public T? Payload { get; init; }
}
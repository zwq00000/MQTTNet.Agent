
namespace MQTTnet.Agent;

/// <summary>
/// 消息订阅器
/// </summary>
public interface IMessageSubscriber : IDisposable {

    // /// <summary>
    // /// 获取主题订阅
    // /// </summary>
    // /// <typeparam name="T"></typeparam>
    // IObservable<MessageArgs<T>> GetSubject<T>(string topic) where T : class;

    /// <summary>
    /// 订阅主题
    /// </summary>
    /// <typeparam name="T"></typeparam>
    Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 包含处理程序的主题订阅
    /// </summary>
    /// <typeparam name="T">订阅消息类型</typeparam>
    Task<IDisposable> SubscribeAsync<T>(string topic, Action<MessageArgs<T>> onNext, CancellationToken cancellationToken = default) where T : class;
}

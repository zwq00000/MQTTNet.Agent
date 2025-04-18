
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet.Agent;

/// <summary>
/// 消息订阅器
/// </summary>
public interface IMessageSubscriber : IDisposable {

    /// <summary>
    /// 订阅主题
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">订阅主题</param>
    /// <param name="typeInfo">json 序列化类型</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, JsonTypeInfo<T>? typeInfo = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 包含处理程序的主题订阅
    /// </summary>
    /// <typeparam name="T">订阅消息类型</typeparam>
    /// <param name="topic">订阅主题</param>
    /// <param name="onNext">处理程序</param>
    /// <param name="typeInfo">json 序列化类型</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IDisposable> SubscribeAsync<T>(string topic, Action<MessageArgs<T>> onNext, JsonTypeInfo<T>? typeInfo = null, CancellationToken cancellationToken = default) where T : class;
}

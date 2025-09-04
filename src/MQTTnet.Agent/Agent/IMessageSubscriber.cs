
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet.Agent;

/// <summary>
/// 消息订阅器
/// </summary>
public interface IMessageSubscriber : IDisposable {

    /// <summary>
    /// 订阅 自定义 类型 消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">主题</param>
    /// <param name="payloadConvert">自定义 类型 转换函数</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, Func<ReadOnlySequence<byte>, T?> payloadConvert, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅 字符串 类型 消息
    /// </summary>
    /// <param name="topic">主题</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<IObservable<MessageArgs<string>>> SubscribeAsync(string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅主题
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">主题</param>
    /// <param name="options">Json 序列化选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, JsonSerializerOptions options, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 包含处理程序的主题订阅
    /// </summary>
    /// <typeparam name="T">订阅消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="options">Json 序列化选项</param>
    /// <param name="onNext">处理程序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    [Obsolete("请使用 SubscribeAsync<T>(string topic, JsonSerializerOptions options, CancellationToken cancellationToken = default) where T : class;")]
    Task<IDisposable> SubscribeAsync<T>(string topic, JsonSerializerOptions options, Action<MessageArgs<T>> onNext, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 订阅主题
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">主题</param>
    /// <param name="options">Json 序列化选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, JsonTypeInfo<T> options, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 包含处理程序的主题订阅
    /// </summary>
    /// <typeparam name="T">订阅消息类型</typeparam>
    /// <param name="topic">主题</param>
    /// <param name="options">Json 序列化选项</param>
    /// <param name="onNext">处理程序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    [Obsolete("请使用 SubscribeAsync<T>(string topic, JsonTypeInfo<T> options, CancellationToken cancellationToken = default) where T : class;")]
    Task<IDisposable> SubscribeAsync<T>(string topic, JsonTypeInfo<T> options, Action<MessageArgs<T>> onNext, CancellationToken cancellationToken = default) where T : class;


}

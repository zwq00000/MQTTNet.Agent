MQTTnet.Agent MQTT 消息代理
================

# 概述
## 目标
1. 建立类型化 订阅/发布接口
2. 通过 Pub/Sub 接口 简化 MQTTnet Client 消息订阅和发布方法

# 接口定义

## 消息订阅接口

- IMessageReader 
  - 接口返回 ChannelReader<T> 方便异步处理消息
```c#
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
```

- IMessageSubscriber
  - Reactive 风格的订阅接口
```c#
/// <summary>
/// 消息订阅器
/// </summary>
public interface IMessageSubscriber : IDisposable {

    /// <summary>
    /// 获取主题订阅
    /// </summary>
    /// <typeparam name="T"></typeparam>
    IObservable<MessageArgs<T>> GetSubject<T>(string topic) where T : class;

    /// <summary>
    /// 订阅主题
    /// </summary>
    /// <typeparam name="T"></typeparam>
    Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class;
}
```

- 发布接口

```c#
public interface IMessagePublisher {
    /// <summary>
    /// 发布消息
    /// </summary>
    /// <param name="topic">发布主题</param>
    /// <param name="payload">载荷对象</param>
    /// <param name="retain">消息保留标志,默认为 <see langword="false"/></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task PublishAsync<T>(string topic, T? payload, JsonSerializerOptions? options = null, bool retain = false, CancellationToken cancellationToken = default(CancellationToken)) where T : class;

    /// <summary>
    /// 发布文本消息
    /// </summary>
    /// <param name="topic">发布主题</param>
    /// <param name="payload">载荷内容</param>
    /// <param name="retain">消息保留标志,默认为 <see langword="false"/></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task PublishStringAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default(CancellationToken));
}
```

- 合并接口

```c#
public interface IMessageAgent : IMessageReader, IMessagePublisher, IDisposable {

}

public interface IMessageHub : IMessageSubscriber, IMessagePublisher {

}
```
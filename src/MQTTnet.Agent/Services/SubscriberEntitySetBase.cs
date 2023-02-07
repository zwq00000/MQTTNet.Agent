using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MQTTnet.Agent;

/// <summary>
/// 基于消息订阅的实体数据字典 基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SubscriberEntitySetBase<T> : IDisposable where T : class {
    private readonly IMessageSubscriber subscriber;
    private ILogger logger;
    private ConcurrentDictionary<string, T> entityMap = new ConcurrentDictionary<string, T>();
    private IDisposable? disposeSub;

    protected SubscriberEntitySetBase(IMessageSubscriber subscriber, string subscribeTopic, ILogger<SubscriberEntitySetBase<T>> logger) {
        this.subscriber = subscriber;
        this.logger = logger;
        Start(subscribeTopic);
    }

    private async void Start(string topic) {
        logger.LogInformation("start SubscriberEntitySetBase<{type}>", typeof(T).Name);
        this.disposeSub = await subscriber.SubscribeAsync<T>(topic, OnReceived).ConfigureAwait(false);
    }

    public void Dispose() {
        this.subscriber.Dispose();
        if (disposeSub != null) {
            disposeSub.Dispose();
        }
        logger.LogInformation("SubscriberEntitySetBase<{type}> disposed", typeof(T).Name);
    }

    public IEnumerable<T> GetAll() {
        return entityMap.Values;
    }

    /// <summary>
    /// 查找主题
    /// </summary>
    /// <param name="match"></param>
    /// <returns></returns>
    public IEnumerable<T> FindAll(Func<string, bool> match) {
        return entityMap.Where(kv => match(kv.Key)).Select(kv => kv.Value);
    }

    public T? GetById(string id) {
        if (entityMap.TryGetValue(id, out var entity)) {
            return entity;
        }
        return null;
    }

    private void OnReceived(MessageArgs<T> msg) {
        if (msg.Payload == null) {
            entityMap.Remove(msg.Topic, out _);
        } else {
            entityMap.AddOrUpdate(msg.Topic, msg.Payload, (_, _) => msg.Payload);
        }
        logger.LogTrace("SubscriberEntitySetBase<{type}> Received {topic}", typeof(T).Name, msg.Topic);
    }
}
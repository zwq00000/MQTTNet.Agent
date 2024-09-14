using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet.Agent;

/// <summary>
/// 基于 MQTT Client 的 消息订阅器
/// </summary>
internal partial class MqttMessageHub {

    ///<summary>
    /// 构造 消息订阅
    ///</summary>
    private Subject<MessageArgs<T>> BuildSubject<T>(string topic, JsonTypeInfo<T> typeInfo) where T : class {
        var pattern = BuildTopicPattern(topic);
        var subject = new Subject<MessageArgs<T>>();
        var convert = typeInfo.GetConverter<T>();
        processMap.Add(pattern, msg => {
            try {
                subject.OnNext(new MessageArgs<T>() {
                    Topic = msg.Topic,
                    Payload = (T?)(msg.PayloadSegment.Count == 0 ? null : convert(msg.PayloadSegment.Array!))
                });
            } catch (JsonException ex) {
                logger.LogWarning(ex, "订阅 {topic} 解析 {type} 发生异常,{msg}", topic, typeof(T).Name, ex.Message);
                logger.LogInformation("source:{payload}", Encoding.UTF8.GetString(msg.PayloadSegment));
            }
            return Task.CompletedTask;
        });
        return subject;
    }

    public IObservable<MessageArgs<T>> GetSubject<T>(string topic, JsonTypeInfo<T> typeInfo) where T : class {
        if (this.subjectMap.TryGetValue(topic, out var disposable)) {
            return (IObservable<MessageArgs<T>>)disposable;
        }
        var subject = BuildSubject<T>(topic, typeInfo);
        subjectMap.Add(topic, subject);
        return subject;
    }

    /// <summary>
    /// 订阅主题
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public async Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken = default) where T : class {
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, result.Items.First());
        return GetSubject<T>(topic, typeInfo);
    }

    /// <summary>
    /// 包含处理程序的主题订阅
    /// </summary>
    /// <typeparam name="T">订阅消息类型</typeparam>
    public async Task<IDisposable> SubscribeAsync<T>(string topic, JsonTypeInfo<T> typeInfo, Action<MessageArgs<T>> onNext, CancellationToken cancellationToken = default) where T : class {
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, result.Items.First());
        return GetSubject<T>(topic, typeInfo).Subscribe(onNext);
    }
}
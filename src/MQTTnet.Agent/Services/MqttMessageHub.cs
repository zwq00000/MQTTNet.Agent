using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace MQTTnet.Agent;

/// <summary>
/// 基于 MQTT Client 的 消息订阅器
/// </summary>
internal class MqttMessageHub : MqttClientMessagePublisher, IMessageHub {
    private readonly IMqttClient client;
    private readonly JsonSerializerOptions? serializerOptions;
    private readonly ILogger<MqttMessageHub> logger;
    private readonly IDictionary<string, IDisposable> subjectMap = new Dictionary<string, IDisposable>();

    private readonly IDictionary<Regex, Func<MqttApplicationMessage, Task>> processMap = new Dictionary<Regex, Func<MqttApplicationMessage, Task>>();
    private bool _isDisposed = false;

    public MqttMessageHub(IMqttClient client, IOptions<JsonOptions>? jsonOptions, ILogger<MqttMessageHub> logger) : base(client, jsonOptions, logger) {
        this.client = client;
        this.serializerOptions = jsonOptions?.Value.SerializerOptions;
        this.logger = logger;
        client.ApplicationMessageReceivedAsync += OnMessageReceived;
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args) {
        var msg = args.ApplicationMessage;
        foreach (var kv in processMap) {
            if (kv.Key.IsMatch(msg.Topic)) {
                try {
                    return kv.Value(msg);
                } catch (Exception ex) {
                    logger.LogWarning(ex, "解析 {topic} 消息发生异常,{msg}", msg.Topic, ex.Message);
                    logger.LogTrace("topic:'{topic}' payload:{payload}", msg.Topic, msg.Payload);
                }
            }
        }
        return Task.CompletedTask;
    }

    private Regex BuildTopicPattern(string topic) {
        var pattern = topic
                        .Replace("/", "\\/")
                        .Replace("+", "[^/]+")
                        .Replace("#", "(.+)");
        logger.LogTrace("build topic match pattern '{topic}' => '{pattern}'", topic, pattern);
        return new Regex(pattern, RegexOptions.Compiled);
    }

    public IObservable<MessageArgs<T>> GetSubject<T>(string topic, JsonTypeInfo<T>? typeInfo = null) where T : class {
        if (this.subjectMap.TryGetValue(topic, out var disposable)) {
            return (IObservable<MessageArgs<T>>)disposable;
        }
        var subject = BuildSubject<T>(topic, typeInfo);
        subjectMap.Add(topic, subject);
        return subject;
    }

    public async Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, JsonTypeInfo<T>? typeInfo = null, CancellationToken cancellationToken = default) where T : class {
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, result.Items.First());
        return GetSubject<T>(topic);
    }

    public async Task<IDisposable> SubscribeAsync<T>(string topic, Action<MessageArgs<T>> onNext, JsonTypeInfo<T>? typeInfo = null, CancellationToken cancellationToken = default) where T : class {
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, result.Items.First());
        return GetSubject<T>(topic).Subscribe(onNext);
    }

    ///<summary>
    /// 构造 消息订阅
    ///</summary>
    private Subject<MessageArgs<T>> BuildSubject<T>(string topic, JsonTypeInfo<T>? typeInfo = null) where T : class {
        var pattern = BuildTopicPattern(topic);
        var subject = new Subject<MessageArgs<T>>();
        var convert = typeInfo != null ? typeInfo.GetDeserializer<T>() : this.serializerOptions.GetDeserializer<T>();
        processMap.Add(pattern, msg => {
            try {
                subject.OnNext(new MessageArgs<T>() {
                    Topic = msg.Topic,
                    Payload = msg.Payload.Length == 0 ? null : convert(msg.Payload.ToArray())
                });
            } catch (JsonException ex) {
                logger.LogWarning(ex, "订阅 {topic} 解析 {type} 发生异常,{msg}", topic, typeof(T).Name, ex.Message);
                logger.LogInformation("source:{payload}", Encoding.UTF8.GetString(msg.Payload));
            }
            return Task.CompletedTask;
        });
        return subject;
    }

    public void Dispose() {
        if (_isDisposed) {
            return;
        }
        this._isDisposed = true;
        client.ApplicationMessageReceivedAsync -= OnMessageReceived;
        client.Dispose();
        foreach (var item in subjectMap.Values) {
            item.Dispose();
        }
    }
}
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MQTTnet.Agent;

/// <summary>
/// 基于 MQTT Client 的 消息订阅器
/// </summary>
internal class MqttMessageHub : MqttClientMessagePublisher, IMessageHub {
    private readonly IMqttClient client;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly ILogger<MqttMessageHub> logger;
    private readonly IDictionary<string, IDisposable> subjectMap = new Dictionary<string, IDisposable>();

    private readonly IDictionary<Regex, Func<MqttApplicationMessage, Task>> processMap = new Dictionary<Regex, Func<MqttApplicationMessage, Task>>();
    private bool _isDisposed = false;

    public MqttMessageHub(IMqttClient client, IOptions<JsonOptions> jsonOptions, ILogger<MqttMessageHub> logger) : base(client, jsonOptions, logger) {
        this.client = client;
        this.serializerOptions = jsonOptions.Value.SerializerOptions;
        this.logger = logger;

        client.DisconnectedAsync += OnDisconnected;
        client.ApplicationMessageReceivedAsync += OnMessageReceived;
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args) {
        var msg = args.ApplicationMessage;
        foreach (var kv in processMap) {
            if (kv.Key.IsMatch(msg.Topic)) {
                return kv.Value(msg);
            }
        }
        return Task.CompletedTask;
    }

    private async Task OnDisconnected(MqttClientDisconnectedEventArgs arg) {
        if (!_isDisposed) {
            logger.LogWarning("mqtt client {clientId} 断开连接,{reason}", client.Options.ClientId, arg.ReasonString);
            //重新连接
            await Task.Delay(TimeSpan.FromSeconds(5));
            await this.client.ConnectAsync(client.Options);
        }
    }

    private Func<byte[], T?> GetDeserializer<T>() where T : class {
        var token = new TokenOf<T>();
        switch (token) {
            case TokenOf<string>:
                return p => {
                    return Encoding.UTF8.GetString(p) as T;
                };
            case TokenOf<byte[]>:
                return p => p as T;
            default:
                return payload => JsonSerializer.Deserialize<T>(payload, serializerOptions);
        }
    }

    private Regex BuildTopicPattern(string topic) {
        var pattern = topic
                        .Replace("/", "\\/")
                        .Replace("+", "[^/]+")
                        .Replace("#", "(.+)");
        logger.LogTrace("build topic match pattern '{topic}' => '{pattern}'", topic, pattern);
        return new Regex(pattern, RegexOptions.Compiled);
    }

    public IObservable<MessageArgs<T>> GetSubject<T>(string topic) where T : class {
        if (this.subjectMap.TryGetValue(topic, out var disposable)) {
            return (IObservable<MessageArgs<T>>)disposable;
        }
        var subject = BuildSubject<T>(topic);
        subjectMap.Add(topic, subject);
        return subject;
    }

    public async Task<IObservable<MessageArgs<T>>> SubscribeAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class {
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, result.Items.First());
        return GetSubject<T>(topic);
    }

    ///<summary>
    /// 构造 消息订阅
    ///</summary>
    private Subject<MessageArgs<T>> BuildSubject<T>(string topic) where T : class {
        var pattern = BuildTopicPattern(topic);
        var subject = new Subject<MessageArgs<T>>();
        var convert = GetDeserializer<T>();
        processMap.Add(pattern, msg => {
            try {
                subject.OnNext(new MessageArgs<T>() {
                    Topic = msg.Topic,
                    Payload = msg.Payload == null ? null : convert(msg.Payload)
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
        client.DisconnectedAsync -= OnDisconnected;
        client.ApplicationMessageReceivedAsync -= OnMessageReceived;
        client.Dispose();
        foreach (var item in subjectMap.Values) {
            item.Dispose();
        }
    }
}
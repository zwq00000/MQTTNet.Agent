using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MQTTnet.Agent;

/// <summary>
/// 基于 MQTT Client 的 消息订阅器
/// </summary>
internal partial class MqttMessageHub : MqttClientMessagePublisher, IMessageHub {
    private readonly IMqttClient client;
    private readonly ILogger<MqttMessageHub> logger;
    private readonly IDictionary<string, IDisposable> subjectMap = new Dictionary<string, IDisposable>();

    private readonly IDictionary<Regex, Func<MqttApplicationMessage, Task>> processMap = new Dictionary<Regex, Func<MqttApplicationMessage, Task>>();
    private bool _isDisposed = false;

    public MqttMessageHub(IMqttClient client, ILogger<MqttMessageHub> logger) : base(client, logger) {
        this.client = client;
        this.logger = logger;

        // client.ConnectedAsync += OnConnected;
        // client.DisconnectedAsync += OnDisconnected;
        client.ApplicationMessageReceivedAsync += OnMessageReceived;
    }

    private async Task OnConnected(MqttClientConnectedEventArgs args) {
        //恢复 subjectMap
        foreach (var topic in subjectMap.Keys) {
            logger.LogInformation("恢复订阅 {topic}", topic);
            await client.SubscribeAsync(topic);
        }
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args) {
        var msg = args.ApplicationMessage;
        foreach (var kv in processMap) {
            if (kv.Key.IsMatch(msg.Topic)) {
                try {
                    return kv.Value(msg);
                } catch (Exception ex) {
                    logger.LogWarning(ex, "解析 {topic} 消息发生异常,{msg}", msg.Topic, ex.Message);
                    logger.LogTrace("topic:'{topic}' payload:{payload}", msg.Topic, msg.PayloadSegment);
                }
            }
        }
        return Task.CompletedTask;
    }

    private async Task OnDisconnected(MqttClientDisconnectedEventArgs arg) {
        if (!_isDisposed) {
            logger.LogWarning("mqtt client {clientId} 断开连接", client.Options.ClientId);
            //重新连接
            logger.LogInformation("5秒后尝试重新连接 MQTT Server");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await this.client.ConnectAsync(client.Options);
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

    private Subject<MessageArgs<ArraySegment<byte>>> BuildSubject(string topic) {
        var pattern = BuildTopicPattern(topic);
        var subject = new Subject<MessageArgs<ArraySegment<byte>>>();
        processMap.Add(pattern, msg => {
            try {
                subject.OnNext(new MessageArgs<ArraySegment<byte>>() {
                    Topic = msg.Topic,
                    Payload = msg.PayloadSegment.Count == 0 ? default : msg.PayloadSegment
                });
            } catch (JsonException ex) {
                logger.LogWarning(ex, "订阅 {topic} 发生异常,{msg}", topic, ex.Message);
                logger.LogInformation("source:{payload}", Encoding.UTF8.GetString(msg.PayloadSegment));
            }
            return Task.CompletedTask;
        });
        return subject;
    }

    public async Task<IObservable<MessageArgs<ArraySegment<byte>>>> SubscribeAsync(string topic, CancellationToken cancellationToken = default) {
        var result = await client.SubscribeAsync(topic, cancellationToken: cancellationToken);
        logger.LogInformation("订阅 {topic} result:{result}", topic, result.Items.First());
        if (this.subjectMap.TryGetValue(topic, out var disposable)) {
            return (IObservable<MessageArgs<ArraySegment<byte>>>)disposable;
        }
        var subject = BuildSubject(topic);
        subjectMap.Add(topic, subject);
        return subject;
    }
}
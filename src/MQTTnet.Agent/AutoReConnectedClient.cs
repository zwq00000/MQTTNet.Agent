using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics.PacketInspection;

namespace MQTTnet.Agent;

/// <summary>
/// 自动重新连接的 MQTT Client
/// </summary>
internal class AutoReConnectedClient : IMqttClient {
    private readonly IMqttClient innerClient;
    private readonly ILogger logger;

    private readonly ISet<string> topics = new HashSet<string>();

    public AutoReConnectedClient(MqttClientOptions options, ILogger<AutoReConnectedClient> logger) {
        this.innerClient = new MqttClientFactory(new InternalMqttNetLogger(logger)).CreateMqttClient();
        this.logger = logger;
        
        innerClient.ConnectAsync(options).Wait();

        innerClient.ConnectedAsync += OnConnected;
        innerClient.DisconnectedAsync += OnDisconnected;
    }

    private async Task OnConnected(MqttClientConnectedEventArgs args) {
        //恢复 subjectMap
        foreach (var topic in topics) {
            logger.LogInformation("恢复订阅 {topic}", topic);
            await innerClient.SubscribeAsync(topic);
        }
    }
    private async Task OnDisconnected(MqttClientDisconnectedEventArgs arg) {
        logger.LogWarning("mqtt client {clientId} 断开连接", innerClient.Options.ClientId);
        //重新连接
        logger.LogInformation("5秒后尝试重新连接 MQTT Server");
        await Task.Delay(TimeSpan.FromSeconds(5));
        await this.innerClient.ConnectAsync(innerClient.Options);
    }

    public bool IsConnected => innerClient.IsConnected;

    public MqttClientOptions Options => innerClient.Options;

    public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync {
        add => innerClient.ApplicationMessageReceivedAsync += value;
        remove => innerClient.ApplicationMessageReceivedAsync -= value;
    }
    public event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync {
        add => innerClient.ConnectedAsync += value;
        remove => innerClient.ConnectedAsync -= value;
    }
    public event Func<MqttClientConnectingEventArgs, Task> ConnectingAsync {
        add => innerClient.ConnectingAsync += value;
        remove => innerClient.ConnectingAsync -= value;
    }
    public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync {
        add => innerClient.DisconnectedAsync += value;
        remove => innerClient.DisconnectedAsync -= value;
    }
    public event Func<InspectMqttPacketEventArgs, Task> InspectPacketAsync {
        add => innerClient.InspectPacketAsync += value;
        remove => innerClient.InspectPacketAsync -= value;
    }

    public Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default) {
        return innerClient.ConnectAsync(options, cancellationToken);
    }

    public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default) {
        return innerClient.DisconnectAsync(options, cancellationToken);
    }

    public void Dispose() {
        innerClient.Dispose();
        topics.Clear();
    }

    public Task PingAsync(CancellationToken cancellationToken = default) {
        return innerClient.PingAsync(cancellationToken);
    }

    public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default) {
        return innerClient.PublishAsync(applicationMessage, cancellationToken);
    }

    public Task SendExtendedAuthenticationExchangeDataAsync(MqttEnhancedAuthenticationExchangeData data, CancellationToken cancellationToken = default) {
        return innerClient.SendEnhancedAuthenticationExchangeDataAsync(data, cancellationToken);
    }

    public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default) {
        foreach (var item in options.TopicFilters) {
            topics.Add(item.Topic);
        }
        return innerClient.SubscribeAsync(options, cancellationToken);
    }

    public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default) {
        foreach (var topic in options.TopicFilters) {
            topics.Remove(topic);
        }
        return innerClient.UnsubscribeAsync(options, cancellationToken);
    }

    public Task SendEnhancedAuthenticationExchangeDataAsync(MqttEnhancedAuthenticationExchangeData data, CancellationToken cancellationToken = default) {
        return innerClient.SendEnhancedAuthenticationExchangeDataAsync(data, cancellationToken);
    }

}
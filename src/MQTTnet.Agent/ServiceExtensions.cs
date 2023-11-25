using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Agent;

public static class ServiceExtensions {

    public static IServiceCollection AddMessageAgent(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        services.Add(new ServiceDescriptor(typeof(IMessagePublisher), typeof(MqttClientMessagePublisher), lifetime));
        services.Add(new ServiceDescriptor(typeof(IMessageSubscriber), typeof(MqttMessageHub), lifetime));
        services.Add(new ServiceDescriptor(typeof(IMessageHub), typeof(MqttMessageHub), lifetime));
        services.Add(new ServiceDescriptor(typeof(IMessageReader), typeof(MqttClientMessageAgent), lifetime));
        services.Add(new ServiceDescriptor(typeof(IMessageAgent), typeof(MqttClientMessageAgent), lifetime));

        return services;
    }

    /// <summary>
    /// 增加 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionBuilder">MQTT Connection Options 构建方法</param>
    /// <param name="lifetime">服务生命周期,默认为<see cref="ServiceLifetime.Transient"/></param>
    /// <returns></returns>
    public static IServiceCollection AddMessageAgent(this IServiceCollection services, Action<MqttConnectionOptions> optionBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        services.AddMqttClient(optionBuilder, ServiceLifetime.Transient);
        services.AddMessageAgent(lifetime);

        return services;
    }

    /// <summary>
    /// 注册 <see cref="IMqttClient">MQTT 客户端</see>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionBuilder"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddMqttClient(this IServiceCollection services, Action<MqttConnectionOptions> optionBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        if (optionBuilder == null) {
            throw new ArgumentNullException(nameof(optionBuilder));
        }
        services.AddOptions<MqttConnectionOptions>().Configure(optionBuilder);
        var factory = new MqttFactory();
        services.AddSingleton<IMqttNetLogger, InternalMqttNetLogger>();

        //注册 默认 IMqttClient,已经连接
        services.Add(new ServiceDescriptor(typeof(IMqttClient), s => {
            var options = s.GetRequiredService<IOptions<MqttConnectionOptions>>();
            var clientOptions = options.Value.BuildClientOptions();
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new  AutoReConnectedClient(clientOptions,logger);
        }, lifetime));

        return services;
    }

    internal class AutoReConnectedClient : IMqttClient {
        private readonly IMqttClient innerClient;
        private readonly ILogger logger;

        private readonly ISet<string> topics = new HashSet<string>();

        public AutoReConnectedClient(MqttClientOptions options, ILogger<AutoReConnectedClient> logger) {
            // options.ValidateFeatures = false;
            this.innerClient = new MqttFactory().CreateMqttClient(new InternalMqttNetLogger(logger));
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
        }

        public Task PingAsync(CancellationToken cancellationToken = default) {
            return innerClient.PingAsync(cancellationToken);
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default) {
            return innerClient.PublishAsync(applicationMessage, cancellationToken);
        }

        public Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken = default) {
            return innerClient.SendExtendedAuthenticationExchangeDataAsync(data, cancellationToken);
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
    }
}
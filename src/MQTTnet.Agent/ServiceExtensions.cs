using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Agent;

public static partial class ServiceExtensions {

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
}
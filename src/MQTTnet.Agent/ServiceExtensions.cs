using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        ArgumentNullException.ThrowIfNull(optionBuilder);
        services.AddOptions<MqttConnectionOptions>().Configure(optionBuilder);

        //注册 默认 IMqttClient,已经连接
        services.Add(new ServiceDescriptor(typeof(IMqttClient), s => {
            var options = s.GetRequiredService<IOptions<MqttConnectionOptions>>();
            var clientOptions = options.Value.CreateOptionsBuilder().Build();
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new AutoReConnectedClient(clientOptions, logger);
        }, lifetime));

        return services;
    }

    /// <summary>
    /// 注册 <see cref="IMqttClient">MQTT 客户端</see>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionsBuilder">自定义 MQTT 客户端选项构建器</param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddMqttClient(this IServiceCollection services, MqttClientOptionsBuilder optionsBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        //注册 默认 IMqttClient,已经连接
        services.Add(new ServiceDescriptor(typeof(IMqttClient), s => {
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new AutoReConnectedClient(optionsBuilder.Build(), logger);
        }, lifetime));
        return services;
    }

    public static IServiceCollection AddKeyedMqttClient(this IServiceCollection services, object serviceKey, MqttClientOptionsBuilder optionsBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        //注册 默认 IMqttClient,已经连接
        services.Add(new ServiceDescriptor(typeof(IMqttClient), serviceKey, (s, k) => {
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new AutoReConnectedClient(optionsBuilder.Build(), logger);
        }, lifetime));
        return services;
    }
}
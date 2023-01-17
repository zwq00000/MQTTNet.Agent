using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Agent;

public static class ServiceExtensions {

    public static IServiceCollection AddMessageAgent(this IServiceCollection services,ServiceLifetime lifetime = ServiceLifetime.Transient) {
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
    /// <returns></returns>
    public static IServiceCollection AddMessageAgent(this IServiceCollection services, Action<MqttConnectionOptions> optionBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        services.AddMqttClient(optionBuilder, ServiceLifetime.Transient);
        services.AddMessageAgent(lifetime);

        return services;
    }

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
            var connect = options.Value.Build(new MqttClientOptionsBuilder());
            var logger = s.GetService<IMqttNetLogger>();
            var client = factory.CreateMqttClient(logger);
            client.ConnectAsync(connect).Wait();
            return client;
        }, lifetime));

        return services;
    }
}
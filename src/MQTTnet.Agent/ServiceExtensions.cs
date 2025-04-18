using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace MQTTnet.Agent;

public static partial class ServiceExtensions {

    /// <summary>
    /// 增加 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/>,<see cref="IMessageHub"/>,<see cref="IMessageReader" />,<see cref="IMessageAgent" /> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
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

        //注册 默认 IMqttClient,已经连接
        services.Add(new ServiceDescriptor(typeof(IMqttClient), s => {
            var options = s.GetRequiredService<IOptions<MqttConnectionOptions>>();
            var clientOptions = options.Value.BuildClientOptions();
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new AutoReConnectedClient(clientOptions, logger);
        }, lifetime));

        return services;
    }

    /// <summary>
    /// 增加 包含 serviceKey 的 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/>,<see cref="IMessageHub"/>,<see cref="IMessageReader" />,<see cref="IMessageAgent" /> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceKey"></param>
    /// <param name="optionBuilder"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddKeyedMessageAgent(this IServiceCollection services, string serviceKey, Action<MqttConnectionOptions> optionBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        services.AddKeyedMqttClient(serviceKey, optionBuilder, ServiceLifetime.Transient);
        services.AddKeyedMessageAgent(serviceKey, lifetime);

        return services;
    }


    /// <summary>
    /// /// 增加 包含 serviceKey 的 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/>,<see cref="IMessageHub"/>,<see cref="IMessageReader" />,<see cref="IMessageAgent" /> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceKey"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddKeyedMessageAgent(this IServiceCollection services, string serviceKey, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        services.Add(new ServiceDescriptor(typeof(IMessagePublisher), serviceKey, (s, key) => {
            var client = s.GetRequiredKeyedService<IMqttClient>(serviceKey);
            var options = s.GetService<IOptions<JsonOptions>>();
            return new MqttClientMessagePublisher(client, options, s.GetRequiredService<ILogger<MqttClientMessagePublisher>>());
        }, lifetime));

        services.Add(new ServiceDescriptor(typeof(IMessageSubscriber), serviceKey, (s, key) => {
            var client = s.GetRequiredKeyedService<IMqttClient>(serviceKey);
            var options = s.GetService<IOptions<JsonOptions>>();
            return new MqttMessageHub(client, options, s.GetRequiredService<ILogger<MqttMessageHub>>());
        }, lifetime));

        services.Add(new ServiceDescriptor(typeof(IMessageHub), serviceKey, (s, key) => {
            var client = s.GetRequiredKeyedService<IMqttClient>(serviceKey);
            var options = s.GetService<IOptions<JsonOptions>>();
            return new MqttMessageHub(client, options, s.GetRequiredService<ILogger<MqttMessageHub>>());
        }, lifetime));

        services.Add(new ServiceDescriptor(typeof(IMessageReader), serviceKey, (s, key) => {
            var client = s.GetRequiredKeyedService<IMqttClient>(serviceKey);
            var options = s.GetService<IOptions<JsonOptions>>();
            return new MqttClientMessageAgent(client, options, s.GetRequiredService<ILogger<MqttClientMessageAgent>>());
        }, lifetime));

        services.Add(new ServiceDescriptor(typeof(IMessageAgent), serviceKey, (s, key) => {
            var client = s.GetRequiredKeyedService<IMqttClient>(serviceKey);
            var options = s.GetService<IOptions<JsonOptions>>();
            return new MqttClientMessageAgent(client, options, s.GetRequiredService<ILogger<MqttClientMessageAgent>>());
        }, lifetime));

        return services;
    }

    /// <summary>
    /// 注册 <see cref="IMqttClient">MQTT 客户端</see>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceKey"></param>
    /// <param name="optionBuilder"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddKeyedMqttClient(this IServiceCollection services, string serviceKey, Action<MqttConnectionOptions> optionBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        if (optionBuilder == null) {
            throw new ArgumentNullException(nameof(optionBuilder));
        }

        services.AddOptions<MqttConnectionOptions>(serviceKey).Configure(optionBuilder);

        //注册 默认 IMqttClient,已经连接
        services.Add(new ServiceDescriptor(typeof(IMqttClient), serviceKey, (s, key) => {
            var options = s.GetRequiredKeyedService<IOptions<MqttConnectionOptions>>(key);
            var clientOptions = options.Value.BuildClientOptions();
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new AutoReConnectedClient(clientOptions, logger);
        }, lifetime));

        return services;
    }
}
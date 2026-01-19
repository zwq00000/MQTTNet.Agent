using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MQTTnet.Agent;

public static partial class ServiceExtensions {

    /// <summary>
    /// Add message agent services to the specified <see cref="IServiceCollection"/>.
    /// 增加 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/>,<see cref="IMessagePublisher"/>,<see cref="IMessageReader"/> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddMessageAgent(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        switch (lifetime) {
            case ServiceLifetime.Transient:
                services.AddTransient<IMessagePublisher, MqttClientMessagePublisher>();
                services.AddTransient<IMessageSubscriber, MqttMessageHub>();
                services.AddTransient<IMessageHub, MqttMessageHub>();
                services.AddTransient<IMessageReader, MqttClientMessageAgent>();
                services.AddTransient<IMessageAgent, MqttClientMessageAgent>();
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<IMessagePublisher, MqttClientMessagePublisher>();
                services.AddScoped<IMessageSubscriber, MqttMessageHub>();
                services.AddScoped<IMessageHub, MqttMessageHub>();
                services.AddScoped<IMessageReader, MqttClientMessageAgent>();
                services.AddScoped<IMessageAgent, MqttClientMessageAgent>();
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<IMessagePublisher, MqttClientMessagePublisher>();
                services.AddSingleton<IMessageSubscriber, MqttMessageHub>();
                services.AddSingleton<IMessageHub, MqttMessageHub>();
                services.AddSingleton<IMessageReader, MqttClientMessageAgent>();
                services.AddSingleton<IMessageAgent, MqttClientMessageAgent>();
                break;
        }

        return services;
    }

    /// <summary>
    /// Add message agent services to the specified <see cref="IServiceCollection"/>.
    /// 增加 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/>,<see cref="IMessagePublisher"/>,<see cref="IMessageReader"/> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionBuilder"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddMessageAgent(this IServiceCollection services, Action<MqttConnectionOptions> optionBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        services.AddMqttClient(optionBuilder, lifetime);
        services.AddMessageAgent(lifetime);

        return services;
    }

    /// <summary>
    /// 增加 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/>,<see cref="IMessagePublisher"/>,<see cref="IMessageReader"/> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceKey"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddKeyedMessageAgent(this IServiceCollection services, object? serviceKey, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        switch (lifetime) {
            case ServiceLifetime.Transient:
                services.AddKeyedTransient<IMessagePublisher>(serviceKey, (sp, k) => new MqttClientMessagePublisher(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessagePublisher>>()));
                services.AddKeyedTransient<IMessageSubscriber>(serviceKey, (sp, k) => new MqttMessageHub(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttMessageHub>>()));
                services.AddKeyedTransient<IMessageHub>(serviceKey, (sp, k) => new MqttMessageHub(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttMessageHub>>()));
                services.AddKeyedTransient<IMessageReader>(serviceKey, (sp, k) => new MqttClientMessageAgent(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessageAgent>>()));
                services.AddKeyedTransient<IMessageAgent>(serviceKey, (sp, k) => new MqttClientMessageAgent(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessageAgent>>()));
                break;
            case ServiceLifetime.Scoped:
                services.AddKeyedScoped<IMessagePublisher>(serviceKey, (sp, k) => new MqttClientMessagePublisher(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessagePublisher>>()));
                services.AddKeyedScoped<IMessageSubscriber>(serviceKey, (sp, k) => new MqttMessageHub(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttMessageHub>>()));
                services.AddKeyedScoped<IMessageHub>(serviceKey, (sp, k) => new MqttMessageHub(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttMessageHub>>()));
                services.AddKeyedScoped<IMessageReader>(serviceKey, (sp, k) => new MqttClientMessageAgent(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessageAgent>>()));
                services.AddKeyedScoped<IMessageAgent>(serviceKey, (sp, k) => new MqttClientMessageAgent(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessageAgent>>()));
                break;
            case ServiceLifetime.Singleton:
                services.AddKeyedSingleton<IMessagePublisher>(serviceKey, (sp, k) => new MqttClientMessagePublisher(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessagePublisher>>()));
                services.AddKeyedSingleton<IMessageSubscriber>(serviceKey, (sp, k) => new MqttMessageHub(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttMessageHub>>()));
                services.AddKeyedSingleton<IMessageHub>(serviceKey, (sp, k) => new MqttMessageHub(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttMessageHub>>()));
                services.AddKeyedSingleton<IMessageReader>(serviceKey, (sp, k) => new MqttClientMessageAgent(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessageAgent>>()));
                services.AddKeyedSingleton<IMessageAgent>(serviceKey, (sp, k) => new MqttClientMessageAgent(sp.GetRequiredKeyedService<IMqttClient>(k), sp.GetRequiredService<ILogger<MqttClientMessageAgent>>()));
                break;
        }


        return services;
    }

    /// <summary>
    /// 增加 IMqttClient
    /// 增加 <see cref="IMessageAgent"/>,<see cref="IMessageSubscriber"/>,<see cref="IMessagePublisher"/>,<see cref="IMessageReader"/> 服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionBuilder">MQTT Connection Options 构建方法</param>
    /// <param name="serviceKey"></param>
    /// <param name="lifetime">服务生命周期,默认为<see cref="ServiceLifetime.Transient"/></param>
    /// <returns></returns>
    public static IServiceCollection AddKeyedMessageAgent(this IServiceCollection services, object? serviceKey, Action<MqttConnectionOptions> optionBuilder, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        services.AddKeyedMqttClient(optionBuilder, serviceKey, lifetime);
        services.AddKeyedMessageAgent(serviceKey, lifetime);
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

        Func<IServiceProvider, IMqttClient> resolve = s => {
            var options = s.GetRequiredService<IOptions<MqttConnectionOptions>>();
            var clientOptions = options.Value.CreateOptionsBuilder().Build();
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new AutoReConnectedClient(clientOptions, logger);
        };

        switch (lifetime) {
            case ServiceLifetime.Transient:
                services.AddTransient<IMqttClient>(resolve);
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<IMqttClient>(resolve);
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<IMqttClient>(resolve);
                break;
        }
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
        Func<IServiceProvider, IMqttClient> resolve = s => {
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new AutoReConnectedClient(optionsBuilder.Build(), logger);
        };

        switch (lifetime) {
            case ServiceLifetime.Transient:
                services.AddTransient<IMqttClient>(resolve);
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<IMqttClient>(resolve);
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<IMqttClient>(resolve);
                break;
        }
        return services;
    }

    /// <summary>
    /// 注册 <see cref="IMqttClient">MQTT 客户端</see>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionsBuilder"></param>
    /// <param name="serviceKey"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddKeyedMqttClient(this IServiceCollection services, Action<MqttConnectionOptions> optionsBuilder, object? serviceKey, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        var options = new MqttConnectionOptions();
        optionsBuilder(options);
        return services.AddKeyedMqttClient(options.CreateOptionsBuilder(), serviceKey, lifetime);
    }

    /// <summary>
    /// 注册 <see cref="IMqttClient">MQTT 客户端</see>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceKey"></param>
    /// <param name="optionsBuilder"></param>
    /// <param name="lifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddKeyedMqttClient(this IServiceCollection services, MqttClientOptionsBuilder optionsBuilder, object? serviceKey, ServiceLifetime lifetime = ServiceLifetime.Transient) {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        Func<IServiceProvider, object?, IMqttClient> resolve = (s, k) => {
            var logger = s.GetRequiredService<ILogger<AutoReConnectedClient>>();
            return new AutoReConnectedClient(optionsBuilder.Build(), logger);
        };
        switch (lifetime) {
            case ServiceLifetime.Transient:
                services.AddKeyedTransient<IMqttClient>(serviceKey, resolve);
                break;
            case ServiceLifetime.Scoped:
                services.AddKeyedScoped<IMqttClient>(serviceKey, resolve);
                break;
            case ServiceLifetime.Singleton:
                services.AddKeyedSingleton<IMqttClient>(serviceKey, resolve);
                break;
        }
        return services;
    }
}
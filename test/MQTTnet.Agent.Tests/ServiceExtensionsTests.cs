using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// ServiceExtensions 单元测试
/// 测试依赖注入扩展方法的正确性，确保MQTT相关服务能够正确注册到DI容器中
/// 验证服务注册、生命周期管理和配置选项的正确性
/// </summary>
public class ServiceExtensionsTests {
    private readonly ITestOutputHelper output;
    private readonly ServiceCollection services;

    public ServiceExtensionsTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSimpleConsole());
    }

    /// <summary>
    /// 测试AddMessageAgent服务注册的正确性
    /// 验证调用AddMessageAgent方法后，所有必要的MQTT消息处理服务都被正确注册到DI容器中
    /// 包括消息发布者、订阅者、消息中心、读取器和代理等核心服务
    /// </summary>
    [Fact]
    public void AddMessageAgent_ShouldRegisterAllRequiredServices() {
        // Arrange
        services.AddMqttClient(opt => opt.ConnectionUri = new Uri("mqtt://localhost:1883"));
        
        // Act
        services.AddMessageAgent();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IMessagePublisher>());
        Assert.NotNull(serviceProvider.GetService<IMessageSubscriber>());
        Assert.NotNull(serviceProvider.GetService<IMessageHub>());
        Assert.NotNull(serviceProvider.GetService<IMessageReader>());
        Assert.NotNull(serviceProvider.GetService<IMessageAgent>());
    }

    [Fact]
    public void AddMessageAgent_WithTransientLifetime_ShouldRegisterServicesWithTransientLifetime() {
        // Act
        services.AddMessageAgent(ServiceLifetime.Transient);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Get two instances and verify they are different
        var agent1 = serviceProvider.GetService<IMessageAgent>();
        var agent2 = serviceProvider.GetService<IMessageAgent>();
        
        Assert.NotSame(agent1, agent2);
    }

    [Fact]
    public void AddMessageAgent_WithSingletonLifetime_ShouldRegisterServicesWithSingletonLifetime() {
        // Act
        services.AddMessageAgent(ServiceLifetime.Singleton);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Get two instances and verify they are the same
        var agent1 = serviceProvider.GetService<IMessageAgent>();
        var agent2 = serviceProvider.GetService<IMessageAgent>();
        
        Assert.Same(agent1, agent2);
    }

    [Fact]
    public void AddMessageAgent_WithOptionBuilder_ShouldConfigureMqttClient() {
        // Arrange
        var testUri = new Uri("mqtt://test:1883");
        var testClientId = "test-client-id";

        // Act
        services.AddMessageAgent(options => {
            options.ConnectionUri = testUri;
            options.ClientId = testClientId;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mqttOptions = serviceProvider.GetService<IOptions<MqttConnectionOptions>>();
        Assert.NotNull(mqttOptions);
        Assert.Equal(testUri, mqttOptions.Value.ConnectionUri);
        Assert.Equal(testClientId, mqttOptions.Value.ClientId);
    }

    [Fact]
    public void AddMessageAgent_WithOptionBuilder_ShouldRegisterMqttClient() {
        // Act
        services.AddMessageAgent(options => {
            options.ConnectionUri = new Uri("mqtt://test:1883");
            options.ClientId = "test-client";
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mqttClient = serviceProvider.GetService<IMqttClient>();
        Assert.NotNull(mqttClient);
    }

    [Fact]
    public void AddMqttClient_ShouldRegisterMqttClientWithOptions() {
        // Arrange
        var testUri = new Uri("mqtt://test:1883");
        var testClientId = "test-client-id";

        // Act
        services.AddMqttClient(options => {
            options.ConnectionUri = testUri;
            options.ClientId = testClientId;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mqttOptions = serviceProvider.GetService<IOptions<MqttConnectionOptions>>();
        Assert.NotNull(mqttOptions);
        Assert.Equal(testUri, mqttOptions.Value.ConnectionUri);
        Assert.Equal(testClientId, mqttOptions.Value.ClientId);

        var mqttClient = serviceProvider.GetService<IMqttClient>();
        Assert.NotNull(mqttClient);
    }

    [Fact]
    public void AddMqttClient_WithTransientLifetime_ShouldUseTransientLifetime() {
        // Act
        services.AddMqttClient(options => {
            options.ConnectionUri = new Uri("mqtt://test:1883");
            options.ClientId = "test-client";
        }, ServiceLifetime.Transient);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client1 = serviceProvider.GetService<IMqttClient>();
        var client2 = serviceProvider.GetService<IMqttClient>();
        Assert.NotSame(client1, client2);
    }

    [Fact]
    public void AddMqttClient_WithSingletonLifetime_ShouldUseSingletonLifetime() {
        // Act
        services.AddMqttClient(options => {
            options.ConnectionUri = new Uri("mqtt://test:1883");
            options.ClientId = "test-client";
        }, ServiceLifetime.Singleton);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client1 = serviceProvider.GetService<IMqttClient>();
        var client2 = serviceProvider.GetService<IMqttClient>();
        Assert.Same(client1, client2);
    }

    [Fact]
    public void AddMqttClient_WithNullOptionBuilder_ShouldThrowArgumentNullException() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => {
            services.AddMqttClient((Action<MqttConnectionOptions>)null!);
        });
    }

    [Fact]
    public void AddMessageAgent_WithNullOptionBuilder_ShouldNotThrow() {
        // Act - This should not throw because AddMessageAgent without options doesn't validate the parameter
        services.AddMessageAgent((Action<MqttConnectionOptions>?)null!);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Services should still be registered
        Assert.NotNull(serviceProvider.GetService<IMessageAgent>());
    }

    [Fact]
    public void AddMessageAgent_ShouldRegisterAutoReConnectedClient() {
        // Act
        services.AddMessageAgent(options => {
            options.ConnectionUri = new Uri("mqtt://test:1883");
            options.ClientId = "test-client";
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mqttClient = serviceProvider.GetService<IMqttClient>();
        Assert.IsType<AutoReConnectedClient>(mqttClient);
    }

    [Fact]
    public void ServiceRegistration_ShouldHaveCorrectServiceTypes() {
        // Act
        services.AddMessageAgent();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var publisher = serviceProvider.GetService<IMessagePublisher>();
        var subscriber = serviceProvider.GetService<IMessageSubscriber>();
        var hub = serviceProvider.GetService<IMessageHub>();
        var reader = serviceProvider.GetService<IMessageReader>();
        var agent = serviceProvider.GetService<IMessageAgent>();

        Assert.IsType<MqttClientMessagePublisher>(publisher);
        Assert.IsType<MqttMessageHub>(subscriber);
        Assert.IsType<MqttMessageHub>(hub);
        Assert.IsType<MqttClientMessageAgent>(reader);
        Assert.IsType<MqttClientMessageAgent>(agent);
    }
}
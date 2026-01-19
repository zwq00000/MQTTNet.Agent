using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace MQTTnet.Agent.Tests.KeyedService;

public class ServiceResolveTest {
    private readonly ITestOutputHelper _output;
    public ServiceResolveTest(ITestOutputHelper outputHelper) {
        _output = outputHelper;
    }

    private ServiceProvider BuildServiceProvider(object? key) {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeyedMessageAgent(key, opt => {
            opt.ClientId = "client1";
            opt.ConnectionUri = new Uri("mqtt://localhost:1883");
        });
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("test1")]
    [InlineData("")]
    [InlineData(null)]
    public void TestServiceResolve(object? key) {
        var serviceProvider = BuildServiceProvider(key);
        Assert.NotNull(serviceProvider.GetRequiredKeyedService<IMessagePublisher>(key));
        Assert.NotNull(serviceProvider.GetRequiredKeyedService<IMessageSubscriber>(key));
        Assert.NotNull(serviceProvider.GetRequiredKeyedService<IMessageHub>(key));
        Assert.NotNull(serviceProvider.GetRequiredKeyedService<IMessageReader>(key));
        Assert.NotNull(serviceProvider.GetRequiredKeyedService<IMessageAgent>(key));
    }

    [Fact]
    public void TestServiceResolve_WithNullKey() {
        var serviceProvider = BuildServiceProvider(null);
        Assert.NotNull(serviceProvider.GetRequiredService<IMessagePublisher>());
        Assert.NotNull(serviceProvider.GetRequiredService<IMessageSubscriber>());
        Assert.NotNull(serviceProvider.GetRequiredService<IMessageHub>());
        Assert.NotNull(serviceProvider.GetRequiredService<IMessageReader>());
        Assert.NotNull(serviceProvider.GetRequiredService<IMessageAgent>());
    }
}
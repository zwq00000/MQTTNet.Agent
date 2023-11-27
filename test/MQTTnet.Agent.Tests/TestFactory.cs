using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Agent.Tests;

public class TestFactory {
    public IServiceScope Scope { get; private set; }
    private static Uri MqttUri = new Uri("mqtt://localhost:1883");

    private static void UseMqttClient(IServiceCollection s) {
        s.AddMqttClient(opt => opt.ConnectionUri = MqttUri);
    }

    public TestFactory() : this(UseMqttClient) { }

    public TestFactory(Action<IServiceCollection> serviceBuilder) {
        var services = new ServiceCollection();
        services.AddLogging(e=>e.AddSimpleConsole());
        services.AddMessageAgent();
        serviceBuilder?.Invoke(services);
        var Services = services.BuildServiceProvider();
        this.Scope = Services.CreateScope();
    }

    public IServiceProvider Services => Scope.ServiceProvider;

    public TService GetService<TService>() where TService : notnull{
        return Services.GetRequiredService<TService>();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MQTTnet.Agent.Tests;

public class TestFactory {
    public IServiceScope Scope { get; private set; }

    public static void UseMqttClient(IServiceCollection s) {
        s.AddMqttClient(opt => opt.ConnectionUri = new Uri("mqtt://localhost:1883"));
    }

    internal static void UseWebSocket(IServiceCollection s) {
        s.AddMqttClient(opt => opt.ConnectionUri = new Uri("ws://localhost:8083"));
    }
    

    public TestFactory() : this(UseMqttClient) { }

    public TestFactory(Action<IServiceCollection> serviceBuilder) {
        var services = new ServiceCollection();
        services.AddLogging(e => e.AddSimpleConsole());
        services.AddMessageAgent();
        serviceBuilder?.Invoke(services);
        var Services = services.BuildServiceProvider();
        this.Scope = Services.CreateScope();
    }

    public IServiceProvider Services => Scope.ServiceProvider;

    public TService GetService<TService>() where TService : notnull {
        return Services.GetRequiredService<TService>();
    }

    public static string GetTestTopic([CallerMemberName] string caller = "") {
        return $"test/{nameof(caller)}/{DateTime.Now.Ticks}";
    }
}

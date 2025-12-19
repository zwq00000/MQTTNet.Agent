using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MQTTnet.Agent.Tests;

public class TestFactory : IDisposable {
    public IServiceScope Scope { get; private set; }

    public IServiceScope NewScope { get => this.Services.CreateScope(); }
    private static Uri MqttUri = new Uri("mqtt://localhost:1883");
    private static void UseMqttClient(IServiceCollection s) {
        s.AddMqttClient(opt => {
            opt.ConnectionUri = MqttUri;
            opt.ClearSession = true;
        });
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
        return $"test/{caller}/{DateTime.Now.Ticks}";
    }

    public void Dispose() {
        Scope.Dispose();
    }

}

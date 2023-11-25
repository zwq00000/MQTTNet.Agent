
using System.Diagnostics;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// 测试 自动重新连接 MQTT 客户端
/// </summary>
public class TestAutoReconnectionClient {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;


    public TestAutoReconnectionClient(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory(s => s.AddMqttClient(opt => opt.ConnectionUri = new Uri("mqtt://localhost:1883")));
    }

    private void StopMqttBroker() {
        Process.Start(new ProcessStartInfo() {
            UseShellExecute = true,
            FileName = "docker",
            Arguments = "stop mqtt"
        });
    }

    private void RestartMqtt() {
        output.WriteLine("重新启动 MQTT Broker");
        Process.Start(new ProcessStartInfo() {
            UseShellExecute = true,
            FileName = "docker",
            Arguments = "restart mqtt"
        });
    }

    private async Task SendMsgAsync(string topic, int count = 1) {
        var pubs = factory.GetService<IMessagePublisher>();
        Assert.NotNull(pubs);
        for (var i = 0; i < count; i++) {
            await pubs.PublishStringAsync(topic, $"{DateTime.Now}");
            await Task.Delay(100);
        }
    }

    [Fact]
    public async Task TestSubscribe() {
        var testTopic = $"TEST/{DateTime.Now.Ticks}";
        var subs = factory.GetService<IMessageSubscriber>();
        Assert.NotNull(subs);

        var sub = await subs.SubscribeAsync<string>(testTopic);
        var reciveCount = 0;
        sub.Subscribe(e => {
            output.WriteJson(e);
            reciveCount++;
        });
        await SendMsgAsync(testTopic);
        await Task.Delay(TimeSpan.FromSeconds(1));
        RestartMqtt();
        await Task.Delay(TimeSpan.FromSeconds(6));
        await SendMsgAsync(testTopic,1);
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(2, reciveCount);
    }
}
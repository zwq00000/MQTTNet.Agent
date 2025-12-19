
using System.Diagnostics;
using System.Text;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// MQTT Broker重启测试
/// 测试自动重连MQTT客户端在MQTT broker重启时的行为和恢复能力
/// 验证客户端能够检测到broker中断、自动重连并恢复订阅关系
/// 确保系统在broker维护或故障时的可用性和数据完整性
/// 注意：此测试需要Docker环境运行MQTT broker
/// </summary>
public class MqttBrokerRestartTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;


    public MqttBrokerRestartTests(ITestOutputHelper outputHelper) {
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

    /// <summary>
    /// 测试MQTT broker重启后的订阅恢复功能
    /// 验证订阅者在broker重启后能够自动恢复订阅关系
    /// 测试场景：订阅主题 -> 发送消息 -> 重启broker -> 发送消息
    /// 确保重启前后都能收到消息，验证自动重连和订阅恢复机制
    /// </summary>
    [Fact]
    public async Task TestSubscribe() {
        var testTopic = $"TEST/{DateTime.Now.Ticks}";
        var subs = factory.GetService<IMessageSubscriber>();
        Assert.NotNull(subs);

        var sub = await subs.SubscribeAsync(testTopic);
        var reciveCount = 0;
        sub.Subscribe(e => {
            output.WriteJson(e);
            reciveCount++;
        });
        await SendMsgAsync(testTopic);
        await Task.Delay(TimeSpan.FromSeconds(1));
        RestartMqtt();
        await Task.Delay(TimeSpan.FromSeconds(6));
        await SendMsgAsync(testTopic, 1);
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(2, reciveCount);
    }

    [Fact]
    public async Task TestChannel() {
        var testTopic = $"TEST/{DateTime.Now.Ticks}";
        var subs = factory.GetService<IMessageAgent>();
        Assert.NotNull(subs);
        var cancellationSource = new CancellationTokenSource();
        var channel = await subs.GetChannelAsync([testTopic], e => Encoding.UTF8.GetString(e));
        var reciveCount = 0;
        var task1 = Task.Run(async () => {
            while (!cancellationSource.IsCancellationRequested) {
                var msg = await channel.ReadAsync();
                output.WriteJson(msg);
                reciveCount++;
            }
        });
        await SendMsgAsync(testTopic);
        await Task.Delay(TimeSpan.FromSeconds(1));
        RestartMqtt();
        await Task.Delay(TimeSpan.FromSeconds(6));
        await SendMsgAsync(testTopic, 1);
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(2, reciveCount);
        cancellationSource.Cancel();
    }
}
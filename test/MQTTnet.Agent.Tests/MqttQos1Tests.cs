using System.Diagnostics;
using System.Text;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// MQTT QoS 1 级别消息传递测试
/// 测试MQTT服务质量级别1（至少一次传递）的特性和行为
/// 验证在网络中断、客户端断开等异常情况下，消息的可靠传递机制
/// QoS 1确保消息至少传递一次，但可能重复传递
/// </summary>
public class MqttQos1Tests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public MqttQos1Tests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory(s => s.AddMqttClient(opt => {
            opt.ConnectionUri = new Uri("mqtt://localhost:1883");
            // opt.QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce;
        }));
    }

    private void ExecProcess(string command, string arguments) {
        var process = new Process() {
            StartInfo = new ProcessStartInfo() {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = command,
                Arguments = arguments
            }
        };

        process.Start();
        string outputStr = process.StandardOutput.ReadToEnd();
        string errorStr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // 输出内容可用于调试
        output.WriteLine($"Output: {outputStr}");
        output.WriteLine($"Error: {errorStr}");
   
    }

    /// <summary>
    /// 测试QoS 1级别消息在网络中断时的传递可靠性
    /// 验证在MQTT broker停止和重启过程中，消息的存储和转发机制
    /// 模拟真实网络中断场景，确保消息不会因为网络问题而丢失
    /// 注意：此测试需要Docker环境运行MQTT broker
    /// </summary>
    [Fact]
    public async Task TestQos1MessageDelivery() {
        var testTopic = $"TEST/QOS1/{DateTime.Now.Ticks}";
        var subs = factory.GetService<IMessageSubscriber>();
        Assert.NotNull(subs);

        var receivedMessages = new List<string>();
        var sub = await subs.SubscribeAsync(testTopic);
        sub.Subscribe(e => {
            var message = e.Payload!;
            receivedMessages.Add(message);
            output.WriteLine($"Received: {message}");
        });

        // 发送第一条消息
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);
        var firstMessage = $"First message at {DateTime.Now}";
        await publisher.PublishStringAsync(testTopic, firstMessage);

        // 模拟网络中断
        ExecProcess("docker", "stop mqtt_broker");

        await Task.Delay(TimeSpan.FromSeconds(5));

        // 恢复网络
       ExecProcess("docker", "start mqtt_broker");

        // 发送第二条消息
        var secondMessage = $"Second message at {DateTime.Now}";
        await publisher.PublishStringAsync(testTopic, secondMessage);

        // 等待消息处理
        await Task.Delay(TimeSpan.FromSeconds(2));

        // 验证两条消息都收到
        Assert.Contains(firstMessage, receivedMessages);
        Assert.Contains(secondMessage, receivedMessages);
    }

    [Fact]
    public async Task TestQos1MessageDeliveryWithClientDisconnect() {
        var testTopic = $"TEST/QOS1/{DateTime.Now.Ticks}";

        // 创建第一个客户端用于订阅
        using var factory1 = new TestFactory(s => s.AddMqttClient(opt => {
            opt.ConnectionUri = new Uri("mqtt://localhost:1883");
            opt.ClientId = "test-subscriber";
        }));
        var subs = factory1.GetService<IMessageSubscriber>();
        Assert.NotNull(subs);

        var receivedMessages = new List<string>();
        var sub = await subs.SubscribeAsync(testTopic);
        using var subscription = sub.Subscribe(e => {
            var message = e.Payload!;
            receivedMessages.Add(message);
            output.WriteLine($"Received: {message}");
        });

        // 创建第二个客户端用于发布
        using var factory2 = new TestFactory(s => s.AddMqttClient(opt => {
            opt.ConnectionUri = new Uri("mqtt://localhost:1883");
            opt.ClientId = "test-publisher";
        }));
        var publisher = factory2.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // 发送第一条消息
        var firstMessage = $"First message at {DateTime.Now}";
        await publisher.PublishStringAsync(testTopic, firstMessage, qos: 1);
        await Task.Delay(TimeSpan.FromSeconds(1));

        // 验证第一条消息已收到
        Assert.Contains(firstMessage, receivedMessages);

        // 模拟断网 - 直接销毁发布客户端
        factory2.Dispose();
        await Task.Delay(TimeSpan.FromSeconds(2));

        // 重新创建发布客户端
        using var factory3 = new TestFactory(s => s.AddMqttClient(opt => {
            opt.ConnectionUri = new Uri("mqtt://localhost:1883");
            opt.ClientId = "test-publisher-new";
        }));
        publisher = factory3.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        // 发送第二条消息
        var secondMessage = $"Second message at {DateTime.Now}";
        await publisher.PublishStringAsync(testTopic, secondMessage, qos: 1);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // 验证两条消息都收到
        Assert.Contains(firstMessage, receivedMessages);
        Assert.Contains(secondMessage, receivedMessages);
    }

}
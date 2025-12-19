using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Agent.Tests.Models;
using System.Buffers;
using System.Text.Json;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// IMessageHub 单元测试
/// 测试消息中心的发布订阅功能，包括主题订阅、消息转换、资源管理等
/// 验证消息中心能够正确处理不同类型的消息和订阅模式
/// </summary>
public class IMessageHubTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageHubTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }

    /// <summary>
    /// 测试消息中心的资源释放功能
    /// 验证消息中心在Dispose后能够正确释放资源，同时新的实例能够正常工作
    /// 确保资源管理不会影响消息订阅和发布功能
    /// </summary>
    [Fact]
    public async Task TestDispose() {
        var topic = TestFactory.GetTestTopic();
        using (var agent1 = factory.GetService<IMessageHub>()) {
            Assert.NotNull(agent1);
        }

        var agent = factory.GetService<IMessageHub>();
        int count = 0;
        agent.SubscribeAsync(topic + "/+").Result.Subscribe(s => {
            Assert.StartsWith(topic, s.Topic);
            Assert.NotNull(s.Payload);
            count++;
        });

        for (var i = 0; i < 10; i++) {
            await agent.PublishStringAsync(topic + "/" + i, i.ToString());
        }
        await Task.Delay(100);

        Assert.Equal(10, count);
    }

    [Fact]
    public async Task TestSubscribe() {
        var topic = TestFactory.GetTestTopic();
        var agent = factory.NewScope.ServiceProvider.GetService<IMessageHub>();
        Assert.NotNull(agent);
        int count = 0;
        using var subs = (await agent.SubscribeAsync(topic + "/+")).Subscribe(s => {
            Assert.StartsWith(topic, s.Topic);
            Assert.NotNull(s.Payload);
            count++;
        });
        for (var i = 0; i < 10; i++) {
            await agent.PublishStringAsync(topic + "/" + i, i.ToString());
        }
        await Task.Delay(100);
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task TestSubscribeWithConvert() {
        var topic = TestFactory.GetTestTopic();
        var agent = factory.NewScope.ServiceProvider.GetService<IMessageHub>();
        Assert.NotNull(agent);
        int count = 0;
        using var subs = (await agent.SubscribeAsync<Msg>(topic + "/+", b => JsonSerializer.Deserialize<Msg>(b.ToArray())))
                              .Subscribe(s => {
                                  Assert.StartsWith(topic, s.Topic);
                                  Assert.NotNull(s.Payload);
                                  Assert.Equal("test", s.Payload.Name);
                                  count++;
                              });
        // 发布消息
        for (var i = 0; i < 10; i++) {
            var msg = new Msg {
                Id = i,
                Name = "test"
            };
            await agent.PublishAsync(topic + "/" + i, msg, AppJsonContext.Default.Msg);
        }
        await Task.Delay(100);
        Assert.Equal(10, count);
    }

   
}

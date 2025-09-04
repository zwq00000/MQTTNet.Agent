using Microsoft.Extensions.DependencyInjection;

namespace MQTTnet.Agent.Tests;

public class IMessageHubTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageHubTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }

    [Fact]
    public async void TestDispose() {
        var topic = TestFactory.GetTestTopic();
        using var agent = factory.NewScope.ServiceProvider.GetRequiredService<IMessageHub>();
        Assert.NotNull(agent);

        int count = 0;
        agent.SubscribeAsync(topic).Result.Subscribe(s => {
            Assert.Equal(topic, s.Topic);
            Assert.NotNull(s.Payload);
            count++;
        });

        for (var i = 0; i < 10; i++) {
            await agent.PublishStringAsync(topic, i.ToString());
        }
        await Task.Delay(100);

        Assert.Equal(10, count);
    }

    [Fact]
    public async void TestSubscribeOnNext() {
        var topic = TestFactory.GetTestTopic();
        var agent = factory.NewScope.ServiceProvider.GetService<IMessageHub>();
        Assert.NotNull(agent);
        int count = 0;
        using var subs = agent.SubscribeAsync(topic).Result.Subscribe(s => {
            Assert.Equal(topic, s.Topic);
            Assert.NotNull(s.Payload);
            count++;
        });
        for (var i = 0; i < 10; i++) {
            await agent.PublishStringAsync(topic, i.ToString());
        }
        await Task.Delay(100);
        Assert.Equal(10, count);
    }
}

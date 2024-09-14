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
        var agent = factory.GetService<IMessageHub>();
        Assert.NotNull(agent);
        var subs = await agent.SubscribeAsync<string>(topic, MockJsonSerializerContext.Default.String);
        int count = 0;
        subs.Subscribe(s => {
            output.WriteLine(s.Payload);
            count++;
        });
        for (var i = 0; i < 10; i++) {
            await agent.PublishAsync<string>(topic, i.ToString(), MockJsonSerializerContext.Default.String);
        }
        await Task.Delay(10);
        agent.Dispose();

        Assert.Equal(10, count);
    }
    [Fact]
    public async void TestSubscribe() {
        var topic = TestFactory.GetTestTopic();
        var agent = factory.GetService<IMessageHub>();
        Assert.NotNull(agent);
        int count = 0;
        using var subs = await agent.SubscribeAsync<string>(topic, MockJsonSerializerContext.Default.String, s => {
            output.WriteLine(s.Payload);
            count++;
        });
        for (var i = 0; i < 10; i++) {
            await agent.PublishAsync<string>(topic, i.ToString(),MockJsonSerializerContext.Default.String);
        }
        await Task.Delay(100);
        Assert.Equal(10, count);
    }
}

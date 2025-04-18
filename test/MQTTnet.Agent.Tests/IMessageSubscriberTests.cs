namespace MQTTnet.Agent.Tests;

public class IMessageSubscriberTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageSubscriberTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory(TestFactory.UseWebSocket);
    }

    [Fact]
    public async Task TestUseJsonTypeInfoAsync() {
        var subs = factory.GetService<IMessageSubscriber>();
        Assert.NotNull(subs);
        var expected = 0;
        var obs = await subs.SubscribeAsync<TestData>("test", TestJsonSerializerContext.Default.TestData);
        obs.Subscribe(msg => {
            Assert.NotNull(msg.Payload);
            output.WriteJson(msg.Payload);
            expected++;
        });
        var count = 10;
        await factory.PublishTestDataAsync("test", count);
        Assert.Equal(count, expected);
    }
}
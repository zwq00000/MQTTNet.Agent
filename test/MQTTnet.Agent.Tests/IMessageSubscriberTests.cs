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
        await subs.SubscribeAsync<TestData>("test", msg => {
            Assert.NotNull(msg.Payload);
            output.WriteJson(msg.Payload);
            expected++;
        }, TestJsonSerializerContext.Default.TestData);
        var count = 10;
        await factory.PublishTestDataAsync("test", count);

        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(count, expected);
    }
}
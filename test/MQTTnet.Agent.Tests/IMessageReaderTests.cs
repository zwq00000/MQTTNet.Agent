using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Threading.Tasks;

namespace MQTTnet.Agent.Tests;

public class IMessageReaderTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageReaderTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory(TestFactory.UseWebSocket);
    }

    [Fact]
    public async Task TestUseJsonTypeInfoAsync() {
        var agent = factory.GetService<IMessageReader>();
        Assert.NotNull(agent);
        var channel = await agent.GetChannelAsync<TestData>("test", TestJsonSerializerContext.Default.TestData);
        Assert.NotNull(channel);

        var count = 10;
        var sendTask = factory.PublishTestDataAsync("test", count);
        var reciveTask = Task.Run(async () => {
            for (int i = 0; i < count; i++) {
                var msg = await channel.ReadAsync();
                Assert.NotNull(msg.Payload);
                output.WriteJson(msg.Payload);
            }
        });
        Task.WaitAll(new[] { sendTask, reciveTask }, TimeSpan.FromSeconds(30));

    }
}

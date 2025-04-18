using System.Runtime.CompilerServices;

namespace MQTTnet.Agent.Tests;

public class IMessageAgentTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageAgentTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory(TestFactory.UseWebSocket);
    }


    [Fact]
    public async void TestDispose() {
        var topic = TestFactory.GetTestTopic();
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var task = Task.Factory.StartNew(async () => {
            await Task.Delay(100);
            for (var i = 0; i < 10; i++) {
                await agent.PublishAsync<string>(topic, i.ToString());
            }
            await Task.Delay(1000);
            agent.Dispose();
        });
        var reader = await agent.GetChannelAsync<string>(topic);
        int count = 0;
        await foreach (var item in reader.ReadAllAsync()) {
            output.WriteLine(item.Payload);
            count++;
        }
        Assert.Equal(10, count);
    }

    [Fact]
    public async void TestGetChannelAsync() {
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var topics = BuildTestTopics().ToArray();
        var cancellationSource = new CancellationTokenSource();
        var channel = await agent.GetChannelAsync<Message<string>>(topics);

        var task1 = Task.Run(async () => {
            await Task.Delay(100);
            for (int i = 0; i < 10; i++) {
                foreach (var topic in topics) {
                    await agent.PublishAsync<Message<string>>(topic, new Message<string>(topic, $"{topic}/{i}"));
                    await Task.Delay(10);
                }
            }
            cancellationSource.Cancel();
        });
        var task2 = Task.Run(async () => {
            while (!cancellationSource.Token.IsCancellationRequested) {
                var msg = await channel.ReadAsync(cancellationSource.Token);
                output.WriteJson(msg);
            }
        });
        // Assert.Throws<TaskCanceledException>(() => {
            Task.WaitAll(task1, task2);
        // });
        // await Task.Delay(1000);
    }

    private IEnumerable<string> BuildTestTopics(string perfix = "test", int count = 10, [CallerMemberName] string caller = "") {
        for (var i = 0; i < count; i++) {
            yield return $"{perfix}/{caller}/{i}";
        }
    }
}

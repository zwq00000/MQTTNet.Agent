using MQTTnet.Agent.Tests.Models;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MQTTnet.Agent.Tests;

public class IMessageAgentTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageAgentTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }


    [Fact]
    public async void TestDispose() {
        var topic = TestFactory.GetTestTopic();
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var task = Task.Factory.StartNew(async () => {
            await Task.Delay(100);
            for (var i = 0; i < 10; i++) {
                await agent.PublishStringAsync(topic, i.ToString());
            }
            await Task.Delay(1000);
            agent.Dispose();
        });
        var reader = await agent.GetChannelAsync([topic], (payload) => Encoding.UTF8.GetString(payload));
        int count = 0;
        await foreach (var item in reader.ReadAllAsync()) {
            output.WriteLine(item.Payload);
            count++;
        }
        Assert.Equal(10, count);
    }

    [Fact]
    public async void TestGetChannelAsync() {
        var jsonOptions = new JsonSerializerOptions() {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var topics = BuildTestTopics().ToArray();
        var cancellationSource = new CancellationTokenSource();
        var channel = await agent.GetChannelAsync<Message<string>>(topics, jsonOptions);

        var reciveCount = 0;

        var task1 = Task.Run(async () => {
            await Task.Delay(100);
            for (int i = 0; i < 10; i++) {
                foreach (var topic in topics) {
                    await agent.PublishAsync(topic, new Message<string>(topic, $"{topic}/{i}"), jsonOptions);
                    await Task.Delay(1);
                }
            }
        });
        var task2 = Task.Run(async () => {
            while (!cancellationSource.Token.IsCancellationRequested) {
                var msg = await channel.ReadAsync(cancellationSource.Token);
                Assert.NotNull(msg.Payload);
                Assert.IsType<Message<string>>(msg.Payload);
                reciveCount++;
            }
        });
        Task.WaitAll([task1, task2], 1000);
        Assert.Equal(1000, reciveCount);
    }

    private IEnumerable<string> BuildTestTopics(string perfix = "test", int count = 10, [CallerMemberName] string caller = "") {
        for (var i = 0; i < count; i++) {
            yield return $"{perfix}/{caller}/{i}";
        }
    }

    [Fact]
    public void TestGetChannelAsync_UseJsonTypeInfo() {
        var jsonOptions = new JsonSerializerOptions() {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var topics = BuildTestTopics().ToArray();
        var cancellationSource = new CancellationTokenSource();
       
        var reciveCount = 0;
        var task1 = Task.Run(async () => {
            await Task.Delay(100);
            var age = 0;
            for (int i = 0; i < 10; i++) {
                foreach (var topic in topics) {
                    await agent.PublishAsync(topic,new Msg() {
                        Id = i,
                        Name = $"name-{i}",
                        Age = age++,
                    },AppJsonContext.Default.Msg);
                    await Task.Delay(1);
                }
            }
        });
        var task2 = Task.Run(async () => {
             var channel = await agent.GetChannelAsync(topics, AppJsonContext.Default.Msg);
            while (!cancellationSource.Token.IsCancellationRequested) {
                var msg = await channel.ReadAsync(cancellationSource.Token);
                Assert.NotNull(msg.Payload);
                Assert.IsType<Msg>(msg.Payload);
                reciveCount++;
            }
        });

        Task.WaitAll([task1, task2], 1000);
        Assert.Equal(1000, reciveCount);
    }
}

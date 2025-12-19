using MQTTnet.Agent.Tests.Models;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// IMessageAgent 单元测试
/// 测试消息代理的核心功能，包括消息发布、订阅、通道创建等
/// 验证消息的序列化/反序列化、异步处理和资源管理
/// </summary>
public class IMessageAgentTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageAgentTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }


    /// <summary>
    /// 测试消息代理的资源释放功能
    /// 验证在Dispose调用后，消息通道能够正确关闭，所有待处理的消息都能被正确接收
    /// 确保资源清理不会导致消息丢失
    /// </summary>
    [Fact]
    public async Task TestDispose() {
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
    public async Task TestGetChannelAsync() {
        var jsonOptions = new JsonSerializerOptions() {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var topics = BuildTestTopics().ToArray();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var channel = await agent.GetChannelAsync<Message<string>>(topics, jsonOptions);

        var reciveCount = 0;

        var task1 = Task.Run(async () => {
            await Task.Delay(100);
            for (int i = 0; i < 100; i++) {
                foreach (var topic in topics) {
                    await agent.PublishAsync(topic, new Message<string>(topic, $"{topic}/{i}"), jsonOptions);
                    await Task.Delay(1);
                }
            }
        }, cancellationSource.Token);
        
        var task2 = Task.Run(async () => {
            try {
                while (!cancellationSource.Token.IsCancellationRequested) {
                    try {
                        var msg = await channel.ReadAsync(cancellationSource.Token);
                        Assert.NotNull(msg.Payload);
                        Assert.IsType<Message<string>>(msg.Payload);
                        reciveCount++;
                    }
                    catch (OperationCanceledException) {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) {
                // 正常的取消操作
            }
        }, cancellationSource.Token);
        
        await Task.WhenAll(task1, task2);
        Assert.Equal(1000, reciveCount); // 10 topics * 100 messages each
    }

    private IEnumerable<string> BuildTestTopics(string perfix = "test", int count = 10, [CallerMemberName] string caller = "") {
        for (var i = 0; i < count; i++) {
            yield return $"{perfix}/{caller}/{i}";
        }
    }

    [Fact]
    public async Task TestGetChannelAsync_UseJsonTypeInfo() {
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var topics = BuildTestTopics().ToArray();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
       
        var reciveCount = 0;
        var task1 = Task.Run(async () => {
            await Task.Delay(100);
            var age = 0;
            for (int i = 0; i < 100; i++) {
                foreach (var topic in topics) {
                    await agent.PublishAsync(topic,new Msg() {
                        Id = i,
                        Name = $"name-{i}",
                        Age = age++,
                    },AppJsonContext.Default.Msg);
                    await Task.Delay(1);
                }
            }
        }, cancellationSource.Token);
        
        var task2 = Task.Run(async () => {
             var channel = await agent.GetChannelAsync(topics, AppJsonContext.Default.Msg);
            try {
                while (!cancellationSource.Token.IsCancellationRequested) {
                    try {
                        var msg = await channel.ReadAsync(cancellationSource.Token);
                        Assert.NotNull(msg.Payload);
                        Assert.IsType<Msg>(msg.Payload);
                        reciveCount++;
                    }
                    catch (OperationCanceledException) {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) {
                // 正常的取消操作
            }
        }, cancellationSource.Token);

        await Task.WhenAll(task1, task2);
        Assert.Equal(1000, reciveCount); // 10 topics * 100 messages each
    }
}

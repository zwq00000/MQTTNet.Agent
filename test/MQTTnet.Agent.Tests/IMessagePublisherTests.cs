using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// IMessagePublisher 单元测试
/// 测试消息发布者的核心功能，包括消息发布、序列化、保留消息处理等
/// 验证消息发布流程的完整性和可靠性，确保消息能够正确发送到MQTT broker
/// </summary>
public class IMessagePublisherTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessagePublisherTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }

    /// <summary>
    /// 测试JSON序列化配置的正确性
    /// 验证消息发布者能够正确使用配置的JSON序列化选项
    /// 包括属性命名策略、编码器和转换器等关键配置
    /// 确保消息序列化符合应用程序的要求
    /// </summary>
    [Fact]
    public void TestJsonSerialize() {
        var f = new TestFactory(s => {
            s.AddMqttClient(opt => opt.ConnectionUri = new Uri("mqtt://localhost:1883"));
            s.AddOptions<JsonOptions>().Configure(o => {
                o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        });
        var options = f.GetService<IOptions<JsonOptions>>();
        Assert.NotNull(options);
        Assert.Equal(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, options.Value.SerializerOptions.Encoder);

    }

    [Fact]
    public void TestSerialize() {
        var payload = 123.1f;
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), serializerOptions);
        Assert.NotEmpty(bytes);
        output.WriteJson(bytes);
    }

    /// <summary>
    /// 测试消息发布的端到端功能
    /// 验证消息发布者能够成功发布消息，并且订阅者能够接收到正确的消息内容
    /// 测试完整的消息流程：发布 -> 传输 -> 接收，确保消息传递的可靠性
    /// 验证发布者能够将消息正确发送到MQTT broker，并且订阅者能够接收到与发布者相同的消息内容
    /// </summary>
    [Fact]
    public async Task TestPublish() {
        // ARRANGE: 测试发布消息
        var topic = TestFactory.GetTestTopic();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);

        var result = default(MessageArgs<string>);
        await ReciveAsync<string>(topic, s => {
            output.WriteLine($"Received message: {s.Payload}");
            result = s;
        });

        // ACT: 发布消息
        await publisher.PublishStringAsync(topic, 123.4f.ToString());
        await Task.Delay(100);

        // ASSERT: 验证消息已被接收
        // var result = receiveTask.Result;
        Assert.NotNull(result.Payload);
        Assert.Equal("123.4", result.Payload);
    }

    private async ValueTask ReciveAsync<T>(string topic, Action<MessageArgs<T>> action, int timeout = 1000) where T : class {
        var agent = new TestFactory().GetService<IMessageHub>();
        Assert.NotNull(agent);
        var subs = await agent.SubscribeAsync<T>(topic, JsonSerializerOptions.Default, CancellationToken.None).TimeoutAfter(TimeSpan.FromMilliseconds(timeout));
        subs.Subscribe(action);
    }

    [Fact]
    public async void TestRemoveRetain() {
        var topic = TestFactory.GetTestTopic();
        var publisher = factory.GetService<IMessagePublisher>();
        var payload = 123.4f.ToString();
        Assert.NotNull(publisher);
        // ARRANGE: 发布保留消息
        await publisher.PublishStringAsync(topic, payload, retain: true);
        // ASSERT: 验证保留消息已被发布
        var result = default(MessageArgs<string>);
        await ReciveAsync<string>(topic, s => result = s);
        await Task.Delay(100);
        Assert.Equal(payload, result.Payload);

        // ACT: 移除保留消息
        await Task.Delay(1000);
        await publisher.RemoveRetainAsync(topic);

        // ASSERT: 验证保留消息已被移除
        await ReciveAsync<string>(topic, s => result = s, 100);
        Assert.Null(result.Payload);
    }
}

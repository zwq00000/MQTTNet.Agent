using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet.Agent.Tests;

public class IMessagePublisherTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessagePublisherTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }

    // [Fact]
    // public void TestJsonSerialize() {
    //     var f = new TestFactory(s => {
    //         s.AddMqttClient(opt => opt.ConnectionUri = new Uri("mqtt://localhost:1883"));
    //         s.AddOptions<JsonOptions>().Configure(o => {
    //             o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    //             o.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    //             o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    //         });
    //     });
    //     Assert.Equal(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, options.Value.SerializerOptions.Encoder);
    // }

    [Fact]
    public void TestSerialize() {
        var payload = 123.1f;
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), serializerOptions);
        Assert.NotEmpty(bytes);
        output.WriteJson(bytes);
    }

    [Fact]
    public async void TestPublish() {
        var topic = TestFactory.GetTestTopic();
        var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(TimeSpan.FromSeconds(5));
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);
        MessageArgs<string> result = new MessageArgs<string>();
        var task = Task.Run(async () => {
            result = await ReciveAsync<string>(topic, MockJsonSerializerContext.Default.String, cancellationSource.Token);
        });
        await Task.Delay(100);
        await publisher.PublishAsync(topic, 123.4f, MockJsonSerializerContext.Default.Single);
        await Task.Delay(1000, cancellationSource.Token);

        Assert.NotNull(result.Payload);
        Assert.Equal("123.4", result.Payload);
    }

    private async Task<MessageArgs<T>> ReciveAsync<T>(string topic, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken) where T : class {
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var channel = await agent.GetChannelAsync<T>(topic, jsonTypeInfo, cancellationToken);
        return await channel.ReadAsync(cancellationToken);
    }
}

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MQTTnet.Agent.Tests;

public class IMessagePublisherTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessagePublisherTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }

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

    [Fact]
    public async void TestPublish() {
        var topic = TestFactory.GetTestTopic();
        var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(TimeSpan.FromSeconds(5));
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);
        MessageArgs<string> result = new MessageArgs<string>();
        var task = Task.Run(async () => {
            result = await ReciveAsync<string>(topic, cancellationSource.Token);
        });
        await Task.Delay(100);
        await publisher.PublishAsync<object>(topic, 123.4f);
        await Task.Delay(1000, cancellationSource.Token);

        Assert.NotNull(result.Payload);
        Assert.Equal("123.4", result.Payload);
    }

    private async Task<MessageArgs<T>> ReciveAsync<T>(string topic, CancellationToken cancellationToken) where T : class {
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var channel = await agent.GetChannelAsync<T>(topic);
        return await channel.ReadAsync(cancellationToken);
    }
}

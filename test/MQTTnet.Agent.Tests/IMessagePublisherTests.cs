using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
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
    public void TestJsonSerialize(){
        var f = new TestFactory(s=>{
            s.AddMqttClient(opt=>opt.ConnectionUri = new Uri("mqtt://192.168.1.15:1883"));
            s.AddOptions<JsonOptions>().Configure(o=>{
                o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        });
        var publisher = f.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);
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
    public async void TestPublish(){
        var publisher = factory.GetService<IMessagePublisher>();
        Assert.NotNull(publisher);
        await publisher.PublishAsync<Object>("test/float",123.4f);
        
    }
}

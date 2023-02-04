using System.Text.Json;

namespace MQTTnet.Agent.Tests;

public class IMessagePublisherTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessagePublisherTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
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

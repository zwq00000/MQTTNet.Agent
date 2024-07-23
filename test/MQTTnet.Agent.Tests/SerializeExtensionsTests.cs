using System.Text.Json;

namespace MQTTnet.Agent.Tests;

public class SerializeExtensionsTests {
    private readonly JsonSerializerOptions serializerOptions;
    public SerializeExtensionsTests() {
        this.serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    [Fact]
    public void TestDeserializerBytes() {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var serialized = SerializeExtensions.Serialize(data);
        var serializer = SerializeExtensions.GetDeserializer<byte[]>(serializerOptions);
        var result = serializer(serialized);
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    [Fact]
    public void TestDeserializerString() {
        var data = "Test DeSerialize";
        var serialized = SerializeExtensions.Serialize(data);
        var serializer = SerializeExtensions.GetDeserializer<string>(serializerOptions);
        var result = serializer(serialized);
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    [Fact]
    public void TestDeserializerNull() {
        string? data = null;
        var serialized = SerializeExtensions.Serialize(data);
        var serializer = SerializeExtensions.GetDeserializer<string>(serializerOptions);
        var result = serializer(serialized);
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    [Fact]
    public void TestDeserializerObject() {
        var data = new MockObject(1, DateTime.Now);
        var serialized = SerializeExtensions.Serialize(data);
        var serializer = SerializeExtensions.GetDeserializer<MockObject>(serializerOptions);
        var result = serializer(serialized);
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    public record MockObject(int Id,DateTime Time);

}
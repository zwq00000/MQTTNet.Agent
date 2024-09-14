using System.Text.Json;
using System.Text.Json.Serialization;

namespace MQTTnet.Agent.Tests;

public class SerializeExtensionsTests {

    public SerializeExtensionsTests() {

    }

    [Fact]
    public void TestDeserializerBytes() {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var serialized = SerializeExtensions.Serialize(data, MockJsonSerializerContext.Default.ByteArray);
        var serializer = SerializeExtensions.GetConverter<byte[]>(MockJsonSerializerContext.Default.ByteArray);
        var result = serializer(serialized);
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    [Fact]
    public void TestDeserializerString() {
        var data = "Test DeSerialize";
        var serialized = SerializeExtensions.Serialize(data, MockJsonSerializerContext.Default.String);
        var serializer = SerializeExtensions.GetConverter<string>(MockJsonSerializerContext.Default.String);
        var result = serializer(serialized);
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    [Fact]
    public void TestDeserializerNull() {
        string? data = string.Empty;
        var serialized = SerializeExtensions.Serialize(data, MockJsonSerializerContext.Default.String);
        var serializer = SerializeExtensions.GetConverter(MockJsonSerializerContext.Default.String);
        var result = serializer(serialized);
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    [Fact]
    public void TestDeserializerObject() {
        var data = new MockObject(1, DateTime.Now);
        var serialized = SerializeExtensions.Serialize(data, MockJsonSerializerContext.Default.MockObject);
        var serializer = SerializeExtensions.GetConverter<MockObject>(MockJsonSerializerContext.Default.MockObject);
        var result = serializer(serialized);
        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    [Fact]
    public void TestDeserializerJsonElement() {
        var data = new MockObject(1, DateTime.Now);
        var serialized = SerializeExtensions.Serialize(data, MockJsonSerializerContext.Default.MockObject);
        var serializer = SerializeExtensions.GetConverter(MockJsonSerializerContext.Default.JsonElement);
        var result = serializer(serialized);
        Assert.NotNull(result);
        // Assert.Equal(data, result);
        Assert.NotNull(result);
        Assert.IsType<JsonElement>(result);
    }
}

public record MockObject(int Id, DateTime Time);

[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(MockObject))]
[JsonSerializable(typeof(byte[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(Message<string>))]
[JsonSerializable(typeof(JsonElement))]

internal partial class MockJsonSerializerContext : JsonSerializerContext {

}
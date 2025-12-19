using System.Buffers;
using System.Text;
using System.Text.Json;

namespace MQTTnet.Agent.Tests;

/// <summary>
/// 序列化扩展方法单元测试
/// 测试MQTT消息的序列化和反序列化功能，包括JSON格式、二进制格式等
/// 验证不同数据类型的序列化正确性、性能和兼容性
/// 确保消息在传输过程中的数据完整性和类型安全
/// </summary>
public class SerializeExtensionsTests {
    private readonly JsonSerializerOptions serializerOptions;
    private readonly JsonSerializerOptions camelCaseOptions;

    public SerializeExtensionsTests() {
        this.serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        this.camelCaseOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <summary>
    /// 测试null值的序列化处理
    /// 验证当传入null值时，序列化扩展方法是否正确返回空数组
    /// 确保null值不会导致序列化异常，符合预期的空值处理逻辑
    /// </summary>
    [Fact]
    public void Serialize_WithNullValue_ShouldReturnEmptyArray() {
        // Arrange
        string? data = null;

        // Act
        var result = SerializeExtensions.Serialize(data);

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// 测试字符串的序列化功能
    /// 验证字符串能够正确序列化为UTF-8字节数组
    /// 确保序列化后的数据能够正确反序列化，保持原始内容的完整性
    /// </summary>
    [Fact]
    public void Serialize_WithString_ShouldReturnUtf8Bytes() {
        // Arrange
        var data = "Test string";

        // Act
        var result = SerializeExtensions.Serialize(data);

        // Assert
        Assert.Equal("Test string", Encoding.UTF8.GetString(result.Span));
    }

    [Fact]
    public void Serialize_WithByteArray_ShouldReturnSameArray() {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = SerializeExtensions.Serialize(data);

        // Assert
        Assert.True(data.AsSpan().SequenceEqual(result.Span));
    }

    [Fact]
    public void Serialize_WithObject_ShouldReturnJsonBytes() {
        // Arrange
        var data = new MockObject(1, DateTime.Now);

        // Act
        var result = SerializeExtensions.Serialize(data);

        // Assert
        Assert.False(result.IsEmpty);

        var json = Encoding.UTF8.GetString(result.Span);
        Assert.Contains("1", json); // Check if ID is serialized
    }

    [Fact]
    public void GetDeserializer_WithStringType_ShouldReturnStringDeserializer() {
        // Arrange
        var testData = "Test string";

        // Act
        var deserializer = SerializeExtensions.GetDeserializer<string>(serializerOptions);
        var bytes = Encoding.UTF8.GetBytes(testData);
        var result = deserializer(new ReadOnlySequence<byte>(bytes));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData, result);
    }

    [Fact]
    public void GetDeserializer_WithByteArrayType_ShouldReturnBytesDeserializer() {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var deserializer = SerializeExtensions.GetDeserializer<byte[]>(serializerOptions);
        var result = deserializer(new ReadOnlySequence<byte>(testData));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testData, result);
    }

    [Fact]
    public void GetDeserializer_WithComplexObject_ShouldReturnJsonDeserializer() {
        // Arrange
        var data = new MockObject(42, DateTime.Now);
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, serializerOptions);

        // Act
        var deserializer = SerializeExtensions.GetDeserializer<MockObject>(serializerOptions);
        var result = deserializer(new ReadOnlySequence<byte>(jsonBytes));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
    }

    [Fact]
    public void GetDeserializer_WithCamelCaseOptions_ShouldRespectNamingPolicy() {
        // Arrange
        var data = new MockObject(123, DateTime.Now);
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, camelCaseOptions);

        // Act
        var deserializer = SerializeExtensions.GetDeserializer<MockObject>(camelCaseOptions);
        var result = deserializer(new ReadOnlySequence<byte>(jsonBytes));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
    }

    [Fact]
    public void GetDeserializer_WithInvalidJson_ShouldReturnNull() {
        // Arrange
        var invalidJson = "{ invalid json }".Select(c => (byte)c).ToArray();

        // Act
        var deserializer = SerializeExtensions.GetDeserializer<MockObject>(serializerOptions);
        var result = deserializer(new ReadOnlySequence<byte>(invalidJson));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDeserializer_WithEmptyData_ShouldReturnNull() {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act
        var deserializer = SerializeExtensions.GetDeserializer<MockObject>(serializerOptions);
        var result = deserializer(new ReadOnlySequence<byte>(emptyData));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip_ShouldPreserveData() {
        // Arrange
        var original = new MockObject(999, DateTime.UtcNow);

        // Act
        var serialized = SerializeExtensions.Serialize(original);
        var deserializer = SerializeExtensions.GetDeserializer<MockObject>(serializerOptions);
        var deserialized = deserializer(new ReadOnlySequence<byte>(serialized.ToArray()));

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Time, deserialized.Time, TimeSpan.FromSeconds(1)); // Allow small time difference
    }

    [Fact]
    public void GetDeserializer_WithLargeObject_ShouldHandleCorrectly() {
        // Arrange
        var largeData = new string('x', 10000);
        var largeObject = new LargeObject(largeData);

        // Act
        var serialized = SerializeExtensions.Serialize(largeObject);
        var deserializer = SerializeExtensions.GetDeserializer<LargeObject>(serializerOptions);
        var result = deserializer(new ReadOnlySequence<byte>(serialized));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(largeData.Length, result.Data.Length);
    }

    [Fact]
    public void GetDeserializer_WithSpecialCharacters_ShouldHandleCorrectly() {
        // Arrange
        var specialData = "特殊字符: 测试 🚀 emoji";
        var bytes = Encoding.UTF8.GetBytes(specialData);

        // Act
        var deserializer = SerializeExtensions.GetDeserializer<string>(serializerOptions);
        var result = deserializer(new ReadOnlySequence<byte>(bytes));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(specialData, result);
    }

    [Fact]
    public void GetDeserializer_WithNullJson_ShouldReturnNull() {
        // Arrange
        var nullJson = "null".Select(c => (byte)c).ToArray();

        // Act
        var deserializer = SerializeExtensions.GetDeserializer<MockObject>(serializerOptions);
        var result = deserializer(new ReadOnlySequence<byte>(nullJson));

        // Assert
        Assert.Null(result);
    }

    public record MockObject(int Id, DateTime Time);
    public record LargeObject(string Data);
}

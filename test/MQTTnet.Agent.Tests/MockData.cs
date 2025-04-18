using System.Text.Json.Serialization;

namespace MQTTnet.Agent.Tests;

internal static class MockData {

    public static async Task PublishTestDataAsync(this TestFactory factory, string topic, int count, CancellationToken cancellationToken = default) {
        var publisher = factory.GetService<IMessagePublisher>();
        var datas = MockData.Generate(count).ToArray();
        foreach (var data in datas) {
            await publisher.PublishAsync<TestData>(topic, data, TestJsonSerializerContext.Default.TestData);
            await Task.Delay(100, cancellationToken);
        }
    }

    public static IEnumerable<TestData> Generate(int count) {

        foreach (var i in Enumerable.Range(0, count)) {
            yield return new TestData() {
                Id = Guid.NewGuid(),
                Name = $"test_{i}",
                Time = DateTime.Now,
                Content = $"test{i}"
            };
        }
    }

}

public record TestData {
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("time")]
    public DateTime? Time { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}



[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(TestData))]
internal partial class TestJsonSerializerContext : JsonSerializerContext {
}
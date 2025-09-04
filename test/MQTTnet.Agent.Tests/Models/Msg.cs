using System.Text.Json.Serialization;

namespace MQTTnet.Agent.Tests.Models;
public record Msg {
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; init; }
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Msg))]
[JsonSerializable(typeof(Message<Msg>))]
internal partial class AppJsonContext:JsonSerializerContext{

}
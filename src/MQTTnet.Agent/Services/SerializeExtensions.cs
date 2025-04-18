using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;


namespace MQTTnet.Agent;

internal static class SerializeExtensions {
    /// <summary>
    /// 默认的序列化选项
    /// </summary>
    private readonly static JsonSerializerOptions DefaultSerializerOptions = new(JsonSerializerDefaults.Web);

    public static byte[] Serialize<T>(T? payload, JsonSerializerOptions? options = null) {
        return payload switch {
            null => Array.Empty<byte>(),
            string s => Encoding.UTF8.GetBytes(s),
            byte[] bytes => bytes,
            JsonNode n => Encoding.UTF8.GetBytes(n.ToJsonString()),
            JsonElement e => Encoding.UTF8.GetBytes(e.GetRawText()),
            _ => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<T>(payload, options),
        };
    }

    internal static Func<byte[], T?> GetDeserializer<T>(this JsonSerializerOptions? serializerOptions) where T : class {
        var token = new TokenOf<T>();
        return token switch {
            TokenOf<string> => p => Encoding.UTF8.GetString(p) as T,
            TokenOf<byte[]> => p => p as T,
            TokenOf<JsonNode> => p => JsonNode.Parse(p) as T,
            TokenOf<JsonElement> => p => JsonDocument.Parse(p).RootElement as T,
            _ => payload => JsonSerializer.Deserialize<T>(payload, serializerOptions ?? DefaultSerializerOptions),
        };
    }

    internal static Func<byte[], T?> GetDeserializer<T>(this JsonTypeInfo<T> typeInfo) where T : class {
        var token = new TokenOf<T>();
        return token switch {
            TokenOf<string> => p => Encoding.UTF8.GetString(p) as T,
            TokenOf<byte[]> => p => p as T,
            TokenOf<JsonNode> => p => JsonNode.Parse(p) as T,
            TokenOf<JsonElement> => p => JsonDocument.Parse(p).RootElement as T,
            _ => payload => JsonSerializer.Deserialize<T>(payload, typeInfo),
        };
    }
}
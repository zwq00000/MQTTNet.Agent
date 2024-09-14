using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;


namespace MQTTnet.Agent;

internal static class SerializeExtensions {

    public static byte[] Serialize<T>(T? payload, JsonTypeInfo<T> options) {
        return payload switch {
            null => Array.Empty<byte>(),
            string s => Encoding.UTF8.GetBytes(s),
            byte[] bytes => bytes,
            _ => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<T>(payload, options),
        };
    }

    internal static Func<byte[], object?> GetConverter<T>(this JsonTypeInfo<T> options) {
        var token = new TokenOf<T>();
        return token switch {
            TokenOf<string> => p => p.Length == 0 ? string.Empty : Encoding.UTF8.GetString(p),
            TokenOf<byte[]> => p => p,
            _ => payload => payload.Length == 0 ? default : JsonSerializer.Deserialize<T>(payload, options),
        };
    }
}
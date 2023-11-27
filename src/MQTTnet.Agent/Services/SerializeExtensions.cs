using System.Text;
using System.Text.Json;


namespace MQTTnet.Agent;

internal static class SerializeExtensions {
    public static byte[] Serialize<T>(T? payload, JsonSerializerOptions? options = null) {
        if (payload == null) {
            return Array.Empty<byte>();
        }
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<T>(payload, options);
    }

    internal static Func<byte[], T?> GetDeserializer<T>(this JsonSerializerOptions serializerOptions) where T : class {
        var token = new TokenOf<T>();
        return token switch {
            TokenOf<string> => p => {
                return Encoding.UTF8.GetString(p) as T;
            }

            ,
            TokenOf<byte[]> => p => p as T,
            _ => payload => JsonSerializer.Deserialize<T>(payload, serializerOptions),
        };
    }

}
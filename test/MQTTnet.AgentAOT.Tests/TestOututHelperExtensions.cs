using System.Text.Json;

namespace Xunit.Abstractions;
public static class TestOututHelperExtensions {

    public static void WriteJson(this ITestOutputHelper output, object obj, JsonSerializerDefaults defaults = JsonSerializerDefaults.Web) {
        output.WriteJson(obj, new JsonSerializerOptions(defaults) {
            WriteIndented = true
        });
    }
    public static void WriteJson(this ITestOutputHelper output, object obj, JsonSerializerOptions options) {
        var json = System.Text.Json.JsonSerializer.Serialize(obj, options);
        output.WriteLine(json);
    }
}

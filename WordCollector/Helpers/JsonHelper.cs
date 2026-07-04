using System.Text.Json;

namespace WordCollector.Helpers;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static T? SafeDeserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch
        {
            return default;
        }
    }

    public static string SafeSerialize<T>(T obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, DefaultOptions);
        }
        catch
        {
            return string.Empty;
        }
    }
}

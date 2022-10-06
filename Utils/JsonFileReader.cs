using System.Text.Json;

namespace AutoEqApi.Utils;

public static class JsonFileReader
{
    public static async Task<T?> ReadAsync<T>(string filePath)
    {
        await using FileStream stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }
}
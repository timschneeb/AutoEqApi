using System.Text.Json.Serialization;

namespace AutoEqApi.Model;

public class AeqSearchResult
{ 
    [JsonPropertyName("n")]
    public string? Name { get; set; }
    [JsonPropertyName("s")]
    public string? Source { get; set; }
    [JsonPropertyName("r")]
    public int Rank { get; set; }
    [JsonPropertyName("i")]
    public long Id { get; set; }

    public string AsPath() => $"database/{Name}/{Source}/graphic.txt";
}
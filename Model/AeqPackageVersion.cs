using System.Text.Json.Serialization;

namespace AutoEqApi.Model;

public class AeqPackageVersion
{ 
    [JsonPropertyName("commit")]
    public string? Commit { get; set; }
    [JsonPropertyName("commit_time")]
    public string? CommitTime { get; set; }
    [JsonPropertyName("package_time")]
    public string? PackageTime { get; set; }
    [JsonPropertyName("package_url")]
    public string? PackageUrl { get; set; }
    [JsonPropertyName("type")]
    public string[]? Type { get; set; }
}
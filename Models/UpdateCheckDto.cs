using System.Text.Json.Serialization;

namespace ADIapp.Models;

public class UpdateCheckResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public UpdateCheckData? Data { get; set; }
}

public class UpdateCheckData
{
    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; set; } = "1.0.0";

    [JsonPropertyName("minVersion")]
    public string MinVersion { get; set; } = "1.0.0";

    [JsonPropertyName("currentVersion")]
    public string CurrentVersion { get; set; } = "1.0.0";

    [JsonPropertyName("hasUpdate")]
    public bool HasUpdate { get; set; }

    [JsonPropertyName("mandatory")]
    public bool Mandatory { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

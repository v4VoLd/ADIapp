using System.Text.Json.Serialization;

namespace ADIapp.Models;

public class CompanyInfoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public CompanyInfoDto? Data { get; set; }
}

public class CompanyInfoDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "contact@adi-performance.com";

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = "+213 559 80 85 01";

    [JsonPropertyName("phone2")]
    public string? Phone2 { get; set; }

    [JsonPropertyName("phone3")]
    public string? Phone3 { get; set; }

    [JsonPropertyName("whatsapp")]
    public string? Whatsapp { get; set; }

    [JsonPropertyName("viber")]
    public string? Viber { get; set; }

    [JsonPropertyName("skype")]
    public string? Skype { get; set; }

    [JsonPropertyName("facebook")]
    public string? Facebook { get; set; }

    [JsonPropertyName("website")]
    public string Website { get; set; } = "adi-performance.com";

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

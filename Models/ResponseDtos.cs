using System.Text.Json.Serialization;

namespace ADIapp.Models;

public class ApiResponseWrapper
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public LoginResponseData? Data { get; set; }
}

public class LoginResponseData
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserDto? User { get; set; }
}

public class UserDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("availableCredit")]
    public double AvailableCredit { get; set; }

    [JsonPropertyName("availableUnit")]
    public double AvailableUnit { get; set; }

    [JsonPropertyName("subscriptionEndDate")]
    public string? SubscriptionEndDate { get; set; }
}

public class EcuIdentifyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public EcuIdentifyData? Data { get; set; }
}

public class EcuIdentifyData
{
    [JsonPropertyName("ecu_brand")]
    public string EcuBrand { get; set; } = string.Empty;

    [JsonPropertyName("ecu_model")]
    public string EcuModel { get; set; } = string.Empty;

    [JsonPropertyName("hardware_id")]
    public string HardwareId { get; set; } = string.Empty;

    [JsonPropertyName("software_id")]
    public string SoftwareId { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_hash")]
    public string FileHash { get; set; } = string.Empty;
}

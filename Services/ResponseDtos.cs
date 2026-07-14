using System.Text.Json.Serialization;

namespace ADIapp.Services;

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

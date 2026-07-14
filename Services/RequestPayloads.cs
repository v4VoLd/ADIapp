using System.Text.Json.Serialization;

namespace ADIapp.Services;

public class LoginRequestPayload
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("hardware")]
    public HardwarePayload Hardware { get; set; } = new HardwarePayload();
}

public class HardwarePayload
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("cpu")]
    public string Cpu { get; set; } = string.Empty;

    [JsonPropertyName("hdd")]
    public string Hdd { get; set; } = string.Empty;

    [JsonPropertyName("motherboard_serial_number")]
    public string MotherboardSerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("hardwarehash")]
    public string HardwareHash { get; set; } = string.Empty;
}

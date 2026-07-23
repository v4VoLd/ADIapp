using System.Text.Json.Serialization;
using System.Collections.Generic;

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

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

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

public class DatabaseTuneDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("price")]
    public string? Price { get; set; }
}

public class EcuIdentifyData
{
    // Legacy / Fallback properties
    [JsonPropertyName("ecu_brand")]
    public string? EcuBrandRaw { get; set; }

    [JsonPropertyName("ecu_model")]
    public string? EcuModelRaw { get; set; }

    [JsonPropertyName("hardware_id")]
    public string? HardwareIdRaw { get; set; }

    [JsonPropertyName("software_id")]
    public string? SoftwareIdRaw { get; set; }

    // New Format - ECU Properties
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("ecuUse")]
    public string? EcuUse { get; set; }

    [JsonPropertyName("ecuBuild")]
    public string? EcuBuild { get; set; }

    [JsonPropertyName("ecuStgNr")]
    public string? EcuStgNr { get; set; }

    [JsonPropertyName("ecuProdNr")]
    public string? EcuProdNr { get; set; }

    [JsonPropertyName("ecuProducer")]
    public string? EcuProducer { get; set; }

    [JsonPropertyName("ecuChecksum")]
    public string? EcuChecksum { get; set; }

    [JsonPropertyName("ecuSoftwareSize")]
    public string? EcuSoftwareSize { get; set; }

    [JsonPropertyName("ecuSoftwareVersion")]
    public string? EcuSoftwareVersion { get; set; }

    [JsonPropertyName("ecuSoftwareVersionVersion")]
    public string? EcuSoftwareVersionVersion { get; set; }

    // New Format - Vehicle Properties
    [JsonPropertyName("vehicleProducer")]
    public string? VehicleProducer { get; set; }

    [JsonPropertyName("vehicleModel")]
    public string? VehicleModel { get; set; }

    [JsonPropertyName("vehicleBuild")]
    public string? VehicleBuild { get; set; }

    [JsonPropertyName("vehicleChassis")]
    public string? VehicleChassis { get; set; }

    [JsonPropertyName("vehicleModelyear")]
    public string? VehicleModelyear { get; set; }

    [JsonPropertyName("vehicleVIN")]
    public string? VehicleVIN { get; set; }

    [JsonPropertyName("vehicleType")]
    public string? VehicleType { get; set; }

    [JsonPropertyName("vehicleCharacteristic")]
    public string? VehicleCharacteristic { get; set; }

    // New Format - Engine Properties
    [JsonPropertyName("engineName")]
    public string? EngineName { get; set; }

    [JsonPropertyName("engineType")]
    public string? EngineType { get; set; }

    [JsonPropertyName("engineDisplacement")]
    public string? EngineDisplacement { get; set; }

    [JsonPropertyName("engineOutputKW")]
    public string? EngineOutputKW { get; set; }

    [JsonPropertyName("engineOutputPS")]
    public string? EngineOutputPS { get; set; }

    [JsonPropertyName("engineTorque")]
    public string? EngineTorque { get; set; }

    [JsonPropertyName("engineEmissionStd")]
    public string? EngineEmissionStd { get; set; }

    [JsonPropertyName("engineTransmission")]
    public string? EngineTransmission { get; set; }

    // General File Metadata
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_hash")]
    public string FileHash { get; set; } = string.Empty;

    [JsonPropertyName("is_supported")]
    public bool IsSupported { get; set; } = true;

    // Services / Tunes
    [JsonPropertyName("services")]
    public List<ServiceDto>? Services { get; set; }

    [JsonPropertyName("availableDatabaseTunes")]
    public List<DatabaseTuneDto>? AvailableDatabaseTunes { get; set; }

    // Computed / Helper Properties
    public string EcuBrand => !string.IsNullOrWhiteSpace(EcuProducer) ? EcuProducer : (EcuBrandRaw ?? "N/A");
    public string EcuModel => !string.IsNullOrWhiteSpace(EcuBuild) ? EcuBuild : (EcuModelRaw ?? "N/A");
    public string HardwareId => !string.IsNullOrWhiteSpace(EcuStgNr) ? EcuStgNr : (!string.IsNullOrWhiteSpace(EcuProdNr) ? EcuProdNr : (HardwareIdRaw ?? "N/A"));
    public string SoftwareId => !string.IsNullOrWhiteSpace(EcuSoftwareVersion) ? EcuSoftwareVersion : (SoftwareIdRaw ?? "N/A");

    public string FullVehicleTitle
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(VehicleProducer)) parts.Add(VehicleProducer);
            if (!string.IsNullOrWhiteSpace(VehicleModel)) parts.Add(VehicleModel);
            if (!string.IsNullOrWhiteSpace(EngineName) && (VehicleModel == null || !VehicleModel.Contains(EngineName))) parts.Add($"({EngineName})");
            return parts.Count > 0 ? string.Join(" ", parts) : "Vehicle Information";
        }
    }

    public List<ServiceDto> GetEffectiveServices()
    {
        if (Services != null && Services.Count > 0)
            return Services;

        var list = new List<ServiceDto>();
        if (AvailableDatabaseTunes != null && AvailableDatabaseTunes.Count > 0)
        {
            int idx = 1;
            foreach (var tune in AvailableDatabaseTunes)
            {
                list.Add(new ServiceDto
                {
                    Id = tune.Id ?? idx++,
                    Name = tune.Name,
                    Price = !string.IsNullOrEmpty(tune.Price) ? tune.Price : "Included",
                    WinolsSymbole = tune.Name
                });
            }
        }
        return list;
    }
}

public class ServiceDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty;

    [JsonPropertyName("winols_symbole")]
    public string WinolsSymbole { get; set; } = string.Empty;
}

public class ProcessingFileDto
{
    [JsonPropertyName("file_hash")]
    public string FileHash { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("order_id")]
    public int? OrderId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("file_sent")]
    public string? FileSent { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    public bool IsOrder => string.Equals(Type, "order", StringComparison.OrdinalIgnoreCase) || OrderId.HasValue;
}

public class SupportMessageDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("read_at")]
    public string? ReadAt { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}


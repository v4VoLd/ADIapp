using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ADIapp.Models;

public class ApiResponseWrapper
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, List<string>>? Errors { get; set; }

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

    [JsonPropertyName("reservedCredit")]
    public double ReservedCredit { get; set; }

    [JsonPropertyName("effectiveAvailableCredit")]
    public double EffectiveAvailableCredit { get; set; }

    [JsonPropertyName("availableUnit")]
    public double AvailableUnit { get; set; }

    [JsonPropertyName("subscriptionEndDate")]
    public string? SubscriptionEndDate { get; set; }

    [JsonPropertyName("order_limit")]
    public int? OrderLimit { get; set; }

    [JsonPropertyName("today_orders_count")]
    public int? TodayOrdersCount { get; set; }

    [JsonPropertyName("remaining_daily_limit")]
    public int? RemainingDailyLimit { get; set; }

    public bool HasDailyLimit => OrderLimit.HasValue && OrderLimit.Value > 0;
    public int RemainingDailyTunes => RemainingDailyLimit ?? (HasDailyLimit ? Math.Max(0, OrderLimit!.Value - (TodayOrdersCount ?? 0)) : int.MaxValue);
    public bool HasReachedDailyLimit => HasDailyLimit && RemainingDailyTunes <= 0;
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

    // Read Hardware & Original File Matches
    [JsonPropertyName("read_hardware")]
    public string? ReadHardware { get; set; }

    [JsonPropertyName("readHardware")]
    public string? ReadHardwareCamel { get; set; }

    public string EffectiveReadHardware => !string.IsNullOrWhiteSpace(ReadHardware) ? ReadHardware : (!string.IsNullOrWhiteSpace(ReadHardwareCamel) ? ReadHardwareCamel : "Standard / OBD");

    [JsonPropertyName("original_matches")]
    public List<OriginalMatchDto>? OriginalMatches { get; set; }

    [JsonPropertyName("originalMatches")]
    public List<OriginalMatchDto>? OriginalMatchesCamel { get; set; }

    [JsonPropertyName("is_original_available")]
    public bool IsOriginalAvailable { get; set; }

    public List<OriginalMatchDto> EffectiveOriginalMatches => OriginalMatches ?? OriginalMatchesCamel ?? new List<OriginalMatchDto>();

    // Computed / Helper Properties
    public string EcuBrand => !string.IsNullOrWhiteSpace(EcuProducer) ? EcuProducer : (EcuBrandRaw ?? "N/A");
    public string EcuModel => !string.IsNullOrWhiteSpace(EcuBuild) ? EcuBuild : (EcuModelRaw ?? "N/A");
    public string HardwareId => !string.IsNullOrWhiteSpace(EcuStgNr) ? EcuStgNr : (!string.IsNullOrWhiteSpace(EcuProdNr) ? EcuProdNr : (HardwareIdRaw ?? "N/A"));
    public string SoftwareId => !string.IsNullOrWhiteSpace(EcuSoftwareVersion) 
        ? EcuSoftwareVersion 
        : (!string.IsNullOrWhiteSpace(EcuSoftwareVersionVersion) ? EcuSoftwareVersionVersion : (SoftwareIdRaw ?? "N/A"));

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

    [JsonPropertyName("is_included_in_subscription")]
    public bool IsIncludedInSubscription { get; set; }

    [JsonPropertyName("remaining_quota")]
    public int? RemainingQuota { get; set; }

    [JsonIgnore]
    public bool IsCoveredBySubscription =>
        IsIncludedInSubscription && (!RemainingQuota.HasValue || RemainingQuota.Value > 0 || RemainingQuota.Value == -1);
}

public class OriginalMatchDto
{
    [JsonPropertyName("projectFile")]
    public string ProjectFile { get; set; } = string.Empty;

    [JsonPropertyName("percent")]
    public double Percent { get; set; }

    [JsonPropertyName("readHardware")]
    public string ReadHardware { get; set; } = string.Empty;

    [JsonPropertyName("ecuSoftwareVersion")]
    public string EcuSoftwareVersion { get; set; } = string.Empty;

    [JsonPropertyName("ecuProdNr")]
    public string EcuProdNr { get; set; } = string.Empty;

    [JsonPropertyName("ecuStgNr")]
    public string EcuStgNr { get; set; } = string.Empty;

    [JsonPropertyName("ecuBuild")]
    public string EcuBuild { get; set; } = string.Empty;

    [JsonPropertyName("softwareSize")]
    public string SoftwareSize { get; set; } = string.Empty;

    public string FormattedMatch => $"{Percent:0.#}%";
    public string FormattedSize
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SoftwareSize))
                return string.Empty;

            var raw = SoftwareSize.Trim();
            if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw.Substring(2);
                if (long.TryParse(raw, System.Globalization.NumberStyles.HexNumber, null, out long hexVal))
                {
                    return FormatBytes(hexVal);
                }
            }

            // WinOLS project property ePrjPropEcuSoftwaresize is exported in hex format (e.g. "200000" for 2MB, "400000" for 4MB)
            bool hasHexChar = raw.Any(c => (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
            if (hasHexChar && long.TryParse(raw, System.Globalization.NumberStyles.HexNumber, null, out long hexWithAlpha))
            {
                return FormatBytes(hexWithAlpha);
            }

            // Check if parsing as hex yields a standard ECU block size (>= 64KB and divisible by 1024)
            if (long.TryParse(raw, System.Globalization.NumberStyles.HexNumber, null, out long hexNum) && hexNum >= 65536 && hexNum % 1024 == 0)
            {
                return FormatBytes(hexNum);
            }

            // Fallback: decimal bytes (e.g. 2097152)
            if (long.TryParse(raw, out long decBytes))
            {
                return FormatBytes(decBytes);
            }

            if (long.TryParse(raw, System.Globalization.NumberStyles.HexNumber, null, out long anyHex))
            {
                return FormatBytes(anyHex);
            }

            return SoftwareSize;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return string.Empty;
        if (bytes >= 1024 * 1024)
        {
            double mb = (double)bytes / (1024.0 * 1024.0);
            return $"{mb:0.##} MB";
        }
        if (bytes >= 1024)
        {
            double kb = (double)bytes / 1024.0;
            return $"{kb:0.##} KB";
        }
        return $"{bytes} B";
    }
}

public class ProcessingFileDto
{
    [JsonPropertyName("file_hash")]
    public string FileHash { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("status_label")]
    public string? StatusLabel { get; set; }

    public string DisplayStatus => !string.IsNullOrWhiteSpace(StatusLabel) ? StatusLabel.ToUpper() : Status.Replace("_", " ").ToUpper();

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("order_id")]
    public int? OrderId { get; set; }

    [JsonPropertyName("ticket_id")]
    public int? TicketId { get; set; }

    [JsonPropertyName("ticket_number")]
    public string? TicketNumber { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("file_sent")]
    public string? FileSent { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    public bool IsOrder => string.Equals(Type, "order", StringComparison.OrdinalIgnoreCase) || OrderId.HasValue;
    public bool IsTicket => string.Equals(Type, "ticket", StringComparison.OrdinalIgnoreCase) || TicketId.HasValue;
}

public class OrderHistoryItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("is_original")]
    public bool IsOriginal { get; set; }

    [JsonPropertyName("read_hardware")]
    public string? ReadHardware { get; set; }

    [JsonPropertyName("file_received")]
    public string? FileReceived { get; set; }

    [JsonPropertyName("original_filename")]
    public string? OriginalFilename { get; set; }

    [JsonPropertyName("file_sent")]
    public string? FileSent { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("total_price")]
    public string TotalPrice { get; set; } = "0";

    [JsonPropertyName("services")]
    public List<ServiceDto>? Services { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }

    [JsonPropertyName("is_download_expired")]
    public bool IsDownloadExpired { get; set; }

    [JsonPropertyName("ticket_number")]
    public string? TicketNumber { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("ecu_brand")]
    public string? EcuBrand { get; set; }

    [JsonPropertyName("ecu_model")]
    public string? EcuModel { get; set; }

    [JsonPropertyName("hardware_id")]
    public string? HardwareId { get; set; }

    [JsonPropertyName("software_id")]
    public string? SoftwareId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    public bool IsCompleted => string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase) || StatusCode == 1;
    public bool IsCanceled => string.Equals(Status, "canceled", StringComparison.OrdinalIgnoreCase) || StatusCode == 2;
    public bool IsPending => string.Equals(Status, "pending", StringComparison.OrdinalIgnoreCase) || StatusCode == 0 || string.Equals(Status, "processing", StringComparison.OrdinalIgnoreCase) || StatusCode == 3;
}

public class OrderHistoryResponseDto
{
    [JsonPropertyName("orders")]
    public List<OrderHistoryItemDto>? Orders { get; set; }

    [JsonPropertyName("ecu_tickets")]
    public List<OrderHistoryItemDto>? EcuTickets { get; set; }
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


using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ADIapp.Helpers;
using ADIapp.Models;
using ADIapp.Config;

namespace ADIapp.Services;

/// <summary>
/// Handles all HTTP communication with the backend API.
/// </summary>
public class ApiService
{
    private static readonly HttpClient _httpClient = new HttpClient();

    static ApiService()
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public static string? AccessToken { get; private set; }
    public static UserDto? CurrentUser { get; private set; }

    // ─────────────────────────────────────────────────────────────
    // Auth
    // ─────────────────────────────────────────────────────────────

    public static async Task<(bool Success, string Message, UserDto? currentUser)> LoginAsync(string email, string password)
    {
        try
        {
            var payload = new LoginRequestPayload
            {
                Email = email,
                Password = password,
                Hardware = new HardwarePayload
                {
                    DeviceId  = HardwareHelper.GetDeviceId(),
                    Cpu       = HardwareHelper.GetCpuName(),
                    Hdd       = HardwareHelper.GetHddSerial(),
                    MotherboardSerialNumber = HardwareHelper.GetMotherboardSerial(),
                    Platform  = HardwareHelper.GetPlatform(),
                    Version   = HardwareHelper.GetVersion(),
                    Manufacturer = HardwareHelper.GetManufacturer(),
                    DeviceName   = HardwareHelper.GetDeviceName(),
                    HardwareHash = HardwareHelper.GetHardwareHash()
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };

            var jsonContent = JsonSerializer.Serialize(payload, jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "login");
            var response = await _httpClient.PostAsync(requestUri, content);

            var responseString = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
            {
                AccessToken  = apiResponse.Data.AccessToken;
                CurrentUser  = apiResponse.Data.User;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", AccessToken);

                return (true, "Login successful", CurrentUser);
            }

            return (false, apiResponse?.Message ?? "Login failed. Please check your credentials.", null);
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}", null);
        }
    }

    public static void Logout()
    {
        AccessToken = null;
        CurrentUser = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    // ─────────────────────────────────────────────────────────────
    // Profile
    // ─────────────────────────────────────────────────────────────

    public static async Task<(bool Success, string Message)> FetchProfileAsync()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return (false, "Not authenticated.");

        try
        {
            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "profile");
            var response = await _httpClient.GetAsync(requestUri);
            var responseString = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse != null && apiResponse.Success && apiResponse.Data?.User != null)
            {
                CurrentUser = apiResponse.Data.User;
                return (true, "Profile loaded");
            }

            return (false, apiResponse?.Message ?? "Failed to load profile.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ECU Identification
    // ─────────────────────────────────────────────────────────────

    public static async Task<EcuIdentifyResponse> UploadAndIdentifyEcuAsync(string filePath)
    {
        if (string.IsNullOrEmpty(AccessToken))
        {
            return new EcuIdentifyResponse
            {
                Success = false,
                Message = "Not authenticated."
            };
        }

        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                return new EcuIdentifyResponse
                {
                    Success = false,
                    Message = "File does not exist."
                };
            }

            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "file/identify");
            
            using var form = new MultipartFormDataContent();
            
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            
            form.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync(requestUri, form);
            var responseString = await response.Content.ReadAsStringAsync();

            var apiResponse = ParseEcuIdentifyResponse(responseString);
            return apiResponse;
        }
        catch (Exception ex)
        {
            return new EcuIdentifyResponse
            {
                Success = false,
                Message = $"Upload error: {ex.Message}"
            };
        }
    }

    private static EcuIdentifyResponse ParseEcuIdentifyResponse(string responseString)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResponse = JsonSerializer.Deserialize<EcuIdentifyResponse>(responseString, options) ?? new EcuIdentifyResponse();

            if (apiResponse.Data == null)
            {
                var rawData = JsonSerializer.Deserialize<EcuIdentifyData>(responseString, options);
                if (rawData != null && (!string.IsNullOrEmpty(rawData.EcuProducer) || !string.IsNullOrEmpty(rawData.VehicleProducer) || rawData.AvailableDatabaseTunes != null || !string.IsNullOrEmpty(rawData.EcuBuild) || !string.IsNullOrEmpty(rawData.Status)))
                {
                    apiResponse.Data = rawData;
                    if (rawData.Status?.Equals("success", StringComparison.OrdinalIgnoreCase) == true || rawData.Status?.Equals("completed", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        apiResponse.Success = true;
                        apiResponse.Status = "completed";
                    }
                }
            }
            else
            {
                if (apiResponse.Data.Status?.Equals("success", StringComparison.OrdinalIgnoreCase) == true || apiResponse.Status?.Equals("success", StringComparison.OrdinalIgnoreCase) == true || apiResponse.Status?.Equals("completed", StringComparison.OrdinalIgnoreCase) == true)
                {
                    apiResponse.Success = true;
                    apiResponse.Status = "completed";
                }
            }

            return apiResponse;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error in ParseEcuIdentifyResponse: {ex.Message}", ex);
            return new EcuIdentifyResponse { Success = false, Message = "Deserialization error" };
        }
    }

    public static async Task<List<NotificationModel>> FetchNotificationsAsync()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return new List<NotificationModel>();

        try
        {
            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "notifications");
            var response = await _httpClient.GetAsync(requestUri);
            var responseString = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean() && root.TryGetProperty("data", out var dataProp))
            {
                var list = JsonSerializer.Deserialize<List<NotificationModel>>(dataProp.GetRawText(), options);
                return list ?? new List<NotificationModel>();
            }
            return new List<NotificationModel>();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching notifications: {ex.Message}", ex);
            return new List<NotificationModel>();
        }
    }

    public static async Task<bool> MarkAllNotificationsAsReadAsync()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return false;

        try
        {
            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "notifications/read");
            var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error marking notifications as read: {ex.Message}", ex);
            return false;
        }
    }

    public static async Task<bool> ClearNotificationsAsync()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return false;

        try
        {
            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "notifications/clear");
            var response = await _httpClient.DeleteAsync(requestUri);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error clearing notifications: {ex.Message}", ex);
            return false;
        }
    }

    public static async Task<List<ProcessingFileDto>> GetProcessingFilesAsync()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return new List<ProcessingFileDto>();

        try
        {
            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "file/processing");
            var response = await _httpClient.GetAsync(requestUri);
            var responseString = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;
            if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean() && root.TryGetProperty("data", out var dataProp))
            {
                var list = JsonSerializer.Deserialize<List<ProcessingFileDto>>(dataProp.GetRawText(), options);
                return list ?? new List<ProcessingFileDto>();
            }
            return new List<ProcessingFileDto>();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting processing files: {ex.Message}", ex);
            return new List<ProcessingFileDto>();
        }
    }

    public static async Task<EcuIdentifyResponse> CheckStatusAsync(string hash)
    {
        if (string.IsNullOrEmpty(AccessToken))
            return new EcuIdentifyResponse { Success = false, Message = "Not authenticated." };

        try
        {
            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), $"file/identify/status/{hash}");
            var response = await _httpClient.GetAsync(requestUri);
            var responseString = await response.Content.ReadAsStringAsync();

            var apiResponse = ParseEcuIdentifyResponse(responseString);
            return apiResponse;
        }
        catch (Exception ex)
        {
            return new EcuIdentifyResponse { Success = false, Message = $"Error checking status: {ex.Message}" };
        }
    }

    public static async Task<(bool Success, string Message)> CreateOrderAsync(string fileHash, List<int> serviceIds, string comment = "Created from Desktop App")
    {
        if (string.IsNullOrEmpty(AccessToken))
            return (false, "Not authenticated.");

        try
        {
            var payload = new
            {
                file_hash = fileHash,
                services = serviceIds,
                comment = comment
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "order");
            var response = await _httpClient.PostAsync(requestUri, content);
            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            bool success = false;
            if (root.TryGetProperty("success", out var successProp))
            {
                success = successProp.GetBoolean();
            }

            string message = "";
            if (root.TryGetProperty("message", out var msgProp))
            {
                message = msgProp.GetString() ?? "";
            }

            if (success)
            {
                _ = FetchProfileAsync();
                return (true, "Order created successfully.");
            }

            return (false, string.IsNullOrEmpty(message) ? "Failed to create order." : message);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error creating order: {ex.Message}", ex);
            return (false, $"Connection error: {ex.Message}");
        }
    }

    public static async Task<List<SupportMessageDto>> GetSupportMessagesAsync()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return new List<SupportMessageDto>();

        try
        {
            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "support/messages");
            var response = await _httpClient.GetAsync(requestUri);
            var responseString = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean() && root.TryGetProperty("data", out var dataProp))
            {
                var list = JsonSerializer.Deserialize<List<SupportMessageDto>>(dataProp.GetRawText(), options);
                return list ?? new List<SupportMessageDto>();
            }

            return new List<SupportMessageDto>();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching support messages: {ex.Message}", ex);
            return new List<SupportMessageDto>();
        }
    }

    public static async Task<(bool Success, string Message)> SendSupportMessageAsync(string messageContent)
    {
        if (string.IsNullOrEmpty(AccessToken))
            return (false, "Not authenticated.");

        try
        {
            var payload = new { content = messageContent };
            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var requestUri = new Uri(new Uri(AppConfig.BaseUrl), "support/messages");
            var response = await _httpClient.PostAsync(requestUri, content);
            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            bool success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
            string message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "" : "";

            return (success, success ? "Support message sent successfully." : (string.IsNullOrEmpty(message) ? "Failed to send support message." : message));
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending support message: {ex.Message}", ex);
            return (false, $"Connection error: {ex.Message}");
        }
    }
}


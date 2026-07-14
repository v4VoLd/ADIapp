using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ADIapp.Helpers;

namespace ADIapp.Services;

/// <summary>
/// Handles all HTTP communication with the backend API.
/// </summary>
public class ApiService
{
    private static readonly HttpClient _httpClient = new HttpClient();

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
}

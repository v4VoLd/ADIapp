using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ADIapp.Helpers;
using PusherClient;

namespace ADIapp.Services
{
    public class ApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "http://localhost/api/desktop/";

        public static string? AccessToken { get; private set; }
        public static UserDto? CurrentUser { get; private set; }

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
                        DeviceId = HardwareHelper.GetDeviceId(),
                        Cpu = HardwareHelper.GetCpuName(),
                        Hdd = HardwareHelper.GetHddSerial(),
                        MotherboardSerialNumber = HardwareHelper.GetMotherboardSerial(),
                        Platform = HardwareHelper.GetPlatform(),
                        Version = HardwareHelper.GetVersion(),
                        Manufacturer = HardwareHelper.GetManufacturer(),
                        DeviceName = HardwareHelper.GetDeviceName(),
                        HardwareHash = HardwareHelper.GetHardwareHash()
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                var jsonContent = JsonSerializer.Serialize(payload, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var requestUri = new Uri(new Uri(BaseUrl), "login");
                var response = await _httpClient.PostAsync(requestUri, content);

                var responseString = await response.Content.ReadAsStringAsync();
                
                // Even if the HTTP status is not 200, the controller response might return 200 with success=false, or error codes.
                // Let's handle parsing the API response safely.
                var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper>(responseString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    AccessToken = apiResponse.Data.AccessToken;
                    CurrentUser = apiResponse.Data.User;
                    
                    // Set authorization header for future requests
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                    
                    return (true, "Login successful", CurrentUser);
                }
                
                return (false, apiResponse?.Message ?? "Login failed. Please check your credentials.",null);
            }
            catch (Exception ex)
            {
                return (false, $"Connection error: {ex.Message}", null);
            }
        }

        public static async Task<(bool Success, string Message)> FetchProfileAsync()
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                return (false, "Not authenticated.");
            }

            try
            {
                var requestUri = new Uri(new Uri(BaseUrl), "profile");
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

        public static void Logout()
        {
            AccessToken = null;
            CurrentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

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


    public static class WebSocketManager
    {
        // These properties are accessible from anywhere in your app
        public static Pusher Client { get; private set; }
        public static Channel UserChannel { get; private set; }

        public static async Task InitializeAsync(int userId)
        {
            // Prevent initializing twice
            if (Client != null && Client.State == ConnectionState.Connected) 
                return;

            var authorizer = new HttpAuthorizer("http://localhost/api/broadcasting/auth");

            Client = new Pusher("c05340c18ec069708baaqcsqscz", new PusherOptions 
            { 
                Host = "localhost:6001", 
                Encrypted = false, 
                Cluster = null,
                Authorizer = authorizer
            });

            await Client.ConnectAsync();
            
            UserChannel = await Client.SubscribeAsync($"App.Models.User.{userId}");
            
        }
    }

}

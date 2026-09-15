using System;
using System.IO;

namespace ADIapp.Config;

/// <summary>
/// Central place for all environment/configuration settings.
/// Supports both compile-time presets (Production vs Testing) and runtime .env overrides.
/// </summary>
public static class AppConfig
{
#if ENV_PROD
    public const string DefaultEnvironmentName = "Production";
    private const string BuildDefaultBaseHost = "https://adi-performance.com/";
    private const string BuildDefaultPusherAppKey = "c05340c18ec069708baaqcsqscz";
    private const string BuildDefaultWebSocketHost = "adi-performance.com";
    private const string BuildDefaultWebsocketPort = "443";
    private const bool BuildDefaultWebsocketEncrypted = true;
#else
    public const string DefaultEnvironmentName = "Testing";
    private const string BuildDefaultBaseHost = "https://adi-file-service.fb-technologies.com/";
    private const string BuildDefaultPusherAppKey = "lskldjfhskdjhf9283648792324";
    private const string BuildDefaultWebSocketHost = "adi-file-service.fb-technologies.com";
    private const string BuildDefaultWebsocketPort = "443";
    private const bool BuildDefaultWebsocketEncrypted = true;
#endif

    public static string EnvironmentName { get; }

    public static string BaseHost { get; }

    public static string BaseUrl => BaseHost.EndsWith("/") ? BaseHost + "api/desktop/" : BaseHost + "/api/desktop/";

    public static string BroadcastingAuthUrl => BaseHost.EndsWith("/") ? BaseHost + "broadcasting/auth" : BaseHost + "/broadcasting/auth";

    public static string PusherAppKey { get; }

    public static string WebSocketHost { get; }

    public static string WebsocketPort { get; }

    public static bool WebsocketEncrypted { get; }

    public static readonly string AppVersion =
        typeof(AppConfig).Assembly.GetName().Version is { } v
            ? $"{v.Major}.{v.Minor}.{v.Build}"
            : "1.0.4";

    static AppConfig()
    {
        // 1. Attempt to load local .env file if it exists
        LoadDotEnv();

        // 2. Read runtime environment overrides or fallback to build-time defaults
        EnvironmentName = GetEnv("ADI_ENV") ?? DefaultEnvironmentName;

        string configuredHost = GetEnv("ADI_BASE_HOST", "BASE_HOST", "APP_HOST") ?? BuildDefaultBaseHost;
        BaseHost = configuredHost.EndsWith("/") ? configuredHost : configuredHost + "/";

        PusherAppKey = GetEnv("ADI_PUSHER_KEY", "PUSHER_APP_KEY") ?? BuildDefaultPusherAppKey;
        WebSocketHost = GetEnv("ADI_WS_HOST", "WEBSOCKET_HOST") ?? BuildDefaultWebSocketHost;
        WebsocketPort = GetEnv("ADI_WS_PORT", "WEBSOCKET_PORT") ?? BuildDefaultWebsocketPort;

        string? enc = GetEnv("ADI_WS_ENCRYPTED", "WEBSOCKET_ENCRYPTED");
        if (!string.IsNullOrWhiteSpace(enc) && bool.TryParse(enc, out bool parsedEnc))
        {
            WebsocketEncrypted = parsedEnc;
        }
        else
        {
            WebsocketEncrypted = BuildDefaultWebsocketEncrypted;
        }
    }

    private static string? GetEnv(params string[] keys)
    {
        foreach (var key in keys)
        {
            string? val = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(val))
            {
                return val.Trim();
            }
        }
        return null;
    }

    private static void LoadDotEnv()
    {
        try
        {
            string[] possiblePaths =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                            continue;

                        int eqIdx = trimmed.IndexOf('=');
                        if (eqIdx > 0)
                        {
                            string k = trimmed.Substring(0, eqIdx).Trim();
                            string v = trimmed.Substring(eqIdx + 1).Trim().Trim('"', '\'');
                            if (!string.IsNullOrEmpty(k) && Environment.GetEnvironmentVariable(k) == null)
                            {
                                Environment.SetEnvironmentVariable(k, v);
                            }
                        }
                    }
                    break;
                }
            }
        }
        catch
        {
            // Silently ignore file access errors on read
        }
    }
}

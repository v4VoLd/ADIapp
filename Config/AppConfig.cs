namespace ADIapp.Config;

/// <summary>
/// Central place for all environment/configuration constants.
/// Change the base URL here once and it propagates everywhere.
/// </summary>
public static class AppConfig
{
    public const string BaseUrl = "http://localhost/api/desktop/";

    public const string BroadcastingAuthUrl = "http://localhost/api/broadcasting/auth";

    public const string PusherAppKey = "c05340c18ec069708baaqcsqscz";

    public const string WebSocketHost = "localhost:6001";
}

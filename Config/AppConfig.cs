namespace ADIapp.Config;

/// <summary>
/// Central place for all environment/configuration constants.
/// Change the base URL here once and it propagates everywhere.
/// </summary>
public static class AppConfig
{
    public const string BaseHost = "http://localhost/";

    public const string BaseUrl = BaseHost + "api/desktop/";

    public const string BroadcastingAuthUrl = BaseHost + "broadcasting/auth";

    public const string PusherAppKey = "c05340c18ec069708baaqcsqscz";

    public const string WebSocketHost = "localhost";

    public const string WebsocketPort = "6001";

    public const bool WebsocketEncrypted = false;
}

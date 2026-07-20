namespace ADIapp.Config;

/// <summary>
/// Central place for all environment/configuration constants.
/// Change the base URL here once and it propagates everywhere.
/// </summary>
public static class AppConfig
{
    public const string BaseHost = "https://adi-file-service.fb-technologies.com/";

    public const string BaseUrl = BaseHost + "api/desktop/";

    public const string BroadcastingAuthUrl = BaseHost + "broadcasting/auth";

    public const string PusherAppKey = "lskldjfhskdjhf9283648792324";

    public const string WebSocketHost = "adi-file-service.fb-technologies.com";

    public const string WebsocketPort = "443";

    public const bool WebsocketEncrypted = true;
}

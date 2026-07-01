public static class NetworkConfig
{
#if UNITY_EDITOR
    public const string ApiBaseUrl    = "http://localhost:8080";
    public const string GameServerUrl = "ws://localhost:3000";
#else
    public const string ApiBaseUrl    = "https://api.tudominio.com";
    public const string GameServerUrl = "wss://game.tudominio.com";
#endif
}
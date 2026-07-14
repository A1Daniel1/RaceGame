public static class NetworkConfig
{
    private const string ALB_HOST = "ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com";

    public static string AuthUrl        => $"http://{ALB_HOST}/api/auth";
    public static string UsersUrl       => $"http://{ALB_HOST}/api/users";
    public static string HistoryUrl     => $"http://{ALB_HOST}/api/history";
    public static string MatchmakingUrl => $"http://{ALB_HOST}/api/matchmaking";
    public static string GameServerUrl  => $"ws://{ALB_HOST}";
}

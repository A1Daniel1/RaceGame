using UnityEngine;

public static class SceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Object.FindObjectOfType<GameManager>() == null)
        {
            var gameManagerObject = new GameObject("[GameManager]");
            gameManagerObject.AddComponent<GameManager>();
        }

        if (Object.FindObjectOfType<AuthManager>() == null)
        {
            var authObject = new GameObject("[AuthManager]");
            authObject.AddComponent<AuthManager>();
        }

        if (Object.FindObjectOfType<NetworkManager>() == null)
        {
            var networkObject = new GameObject("[NetworkManager]");
            networkObject.AddComponent<NetworkManager>();
        }
    }
}

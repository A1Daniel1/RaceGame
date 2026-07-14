using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkBootstrap : MonoBehaviour
{
    public static NetworkBootstrap Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSingletons();
    }

    private void EnsureSingletons()
    {
        Ensure<ApiClient>();
        Ensure<WebSocketClient>();
        Ensure<AuthManager>();
        Ensure<GameNetworkManager>();
        Ensure<MatchmakingManager>();
    }

    private void Ensure<T>() where T : MonoBehaviour
    {
        if (FindFirstObjectByType<T>() == null)
        {
            GameObject go = new GameObject(typeof(T).Name);
            go.AddComponent<T>();
            DontDestroyOnLoad(go);
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public string PlayerId   { get; set; }
    public string Username   { get; set; }
    public string JWT        { get; set; }
    public string LobbyId    { get; set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    public void GoTo(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[GameManager] La escena '{sceneName}' no está en Build Settings. Intentando cargar por índice...");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName)
                {
                    SceneManager.LoadScene(i);
                    return;
                }
            }
        }

        SceneManager.LoadScene(sceneName);
    }
}
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
        SceneManager.LoadScene(sceneName);
    }
}
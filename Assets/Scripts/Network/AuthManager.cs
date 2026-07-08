using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Text;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    [Header("Login Panel")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_Text errorText;

    [Header("Menu Panel (se activa al iniciar sesion)")]
    [SerializeField] private GameObject menuPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        loginPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
        errorText.text = "";
        loginButton.onClick.AddListener(OnLoginClick);
    }

    private void OnLoginClick()
    {
        string user = usernameInput.text.Trim();
        string pass = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            errorText.text = "Completa todos los campos.";
            return;
        }

        errorText.text = "Iniciando sesion...";
        loginButton.interactable = false;
        StartCoroutine(LoginCoroutine(user, pass));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        var payload = new LoginPayload { username = username, password = password };
        string json = JsonUtility.ToJson(payload);

        using var request = new UnityWebRequest(NetworkConfig.AuthUrl, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);

            GameManager.Instance.JWT = response.token;
            GameManager.Instance.Username = username;
            PlayerPrefs.SetString("auth_token", response.token);
            PlayerPrefs.Save();

            loginPanel.SetActive(false);
            if (menuPanel != null) menuPanel.SetActive(true);
            errorText.text = "";

            NetworkManager.Instance.Connect();
        }
        else
        {
            errorText.text = ParseError(request);
            loginButton.interactable = true;
        }
    }

    private static string ParseError(UnityWebRequest request)
    {
        try
        {
            var err = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
            if (!string.IsNullOrEmpty(err.message)) return err.message;
            if (!string.IsNullOrEmpty(err.error))   return err.error;
        }
        catch { }
        return request.error ?? "Error desconocido";
    }

    [System.Serializable]
    private class LoginPayload   { public string username; public string password; }
    [System.Serializable]
    private class AuthResponse   { public string token; }
    [System.Serializable]
    private class ErrorResponse  { public string message; public string error; }
}

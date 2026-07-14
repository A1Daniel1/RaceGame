using System;
using System.Collections;
using System.Text;
using UnityEngine;

[Serializable]
public class AuthResponse
{
    public string id;
    public string username;
    public string token;
    public string message;
    public string error;
}

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public event Action<string> OnLoginSuccess;
    public event Action<string> OnLoginError;
    public event Action<string> OnRegisterSuccess;
    public event Action<string> OnRegisterError;

    public string PlayerId { get; private set; }
    public string Username { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(PlayerId);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSavedAuth();
    }

    public void Login(string username, string password)
    {
        string json = JsonUtility.ToJson(new AuthRequest { username = username, password = password });
        ApiClient.Instance.Post("/api/auth/login", json, OnLoginResponse, (err) => OnLoginError?.Invoke(err));
    }

    public void Register(string username, string password)
    {
        string json = JsonUtility.ToJson(new AuthRequest { username = username, password = password });
        ApiClient.Instance.Post("/api/auth/register", json, OnRegisterResponse, (err) => OnRegisterError?.Invoke(err));
    }

    public void Logout()
    {
        PlayerId = null;
        Username = null;
        ApiClient.Instance.AuthToken = null;
        PlayerPrefs.DeleteKey("auth_token");
        PlayerPrefs.DeleteKey("player_id");
        PlayerPrefs.DeleteKey("player_username");
        PlayerPrefs.Save();
    }

    private void OnLoginResponse(string response)
    {
        AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(response);

        if (!string.IsNullOrEmpty(authResponse.error))
        {
            OnLoginError?.Invoke(authResponse.error);
            return;
        }

        PlayerId = authResponse.id;
        Username = authResponse.username;
        ApiClient.Instance.AuthToken = authResponse.token;

        PlayerPrefs.SetString("auth_token", authResponse.token);
        PlayerPrefs.SetString("player_id", authResponse.id);
        PlayerPrefs.SetString("player_username", authResponse.username);
        PlayerPrefs.Save();

        OnLoginSuccess?.Invoke(Username);
    }

    private void OnRegisterResponse(string response)
    {
        AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(response);

        if (!string.IsNullOrEmpty(authResponse.error))
        {
            OnRegisterError?.Invoke(authResponse.error);
            return;
        }

        PlayerId = authResponse.id;
        Username = authResponse.username;
        ApiClient.Instance.AuthToken = authResponse.token;

        PlayerPrefs.SetString("auth_token", authResponse.token);
        PlayerPrefs.SetString("player_id", authResponse.id);
        PlayerPrefs.SetString("player_username", authResponse.username);
        PlayerPrefs.Save();

        OnRegisterSuccess?.Invoke(Username);
    }

    private void LoadSavedAuth()
    {
        string token = PlayerPrefs.GetString("auth_token", "");
        string playerId = PlayerPrefs.GetString("player_id", "");
        string username = PlayerPrefs.GetString("player_username", "");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(playerId))
        {
            PlayerId = playerId;
            Username = username;
            ApiClient.Instance.AuthToken = token;
        }
    }

    [Serializable]
    private class AuthRequest
    {
        public string username;
        public string password;
    }
}

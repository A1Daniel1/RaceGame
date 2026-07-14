using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [Header("Configuracion")]
    [SerializeField] private string baseUrl = "";

    public string BaseUrl => string.IsNullOrEmpty(baseUrl) ? NetworkConfig.AuthUrl.Replace("/api/auth", "") : baseUrl;
    public string AuthToken { get; set; }

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

    public void Get(string path, Action<string> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(path, onSuccess, onError));
    }

    public void Post(string path, string jsonBody, Action<string> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(PostRequest(path, jsonBody, onSuccess, onError));
    }

    public void Delete(string path, string jsonBody, Action<string> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(DeleteRequest(path, jsonBody, onSuccess, onError));
    }

    private IEnumerator GetRequest(string path, Action<string> onSuccess, Action<string> onError)
    {
        string url = baseUrl + path;
        using UnityWebRequest request = UnityWebRequest.Get(url);
        SetHeaders(request);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(request.downloadHandler.text);
        }
        else
        {
            onError?.Invoke(request.error);
        }
    }

    private IEnumerator PostRequest(string path, string jsonBody, Action<string> onSuccess, Action<string> onError)
    {
        string url = baseUrl + path;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        SetHeaders(request);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(request.downloadHandler.text);
        }
        else
        {
            onError?.Invoke(request.error);
        }
    }

    private IEnumerator DeleteRequest(string path, string jsonBody, Action<string> onSuccess, Action<string> onError)
    {
        string url = baseUrl + path;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using UnityWebRequest request = new UnityWebRequest(url, "DELETE");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        SetHeaders(request);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(request.downloadHandler.text);
        }
        else
        {
            onError?.Invoke(request.error);
        }
    }

    private void SetHeaders(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(AuthToken))
        {
            request.SetRequestHeader("Authorization", "Bearer " + AuthToken);
        }
    }
}

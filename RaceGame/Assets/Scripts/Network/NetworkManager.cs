using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using NativeWebSocket;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Menu UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button findMatchButton;

    private WebSocket webSocket;

    public bool IsConnected => webSocket != null && webSocket.State == WebSocketState.Open;

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
        if (statusText != null)   statusText.text = "Desconectado";
        if (findMatchButton != null)
        {
            findMatchButton.onClick.AddListener(OnFindMatchClick);
            findMatchButton.interactable = false;
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket?.DispatchMessageQueue();
#endif
    }

    public async void Connect()
    {
        if (IsConnected) return;

        SetStatus("Conectando...");

        webSocket = new WebSocket(NetworkConfig.GameServerUrl);

        webSocket.OnOpen += () =>
        {
            Debug.Log("[NetworkManager] WebSocket conectado");
            SetStatus("Conectado");
            SetFindButtonInteractable(true);
        };

        webSocket.OnError += (errorMsg) =>
        {
            Debug.LogError($"[NetworkManager] Error: {errorMsg}");
            SetStatus("Error de conexion");
        };

        webSocket.OnClose += (closeCode) =>
        {
            Debug.Log($"[NetworkManager] Desconectado (codigo: {closeCode})");
            SetStatus("Desconectado");
            SetFindButtonInteractable(false);
        };

        webSocket.OnMessage += (bytes) =>
        {
            string msg = Encoding.UTF8.GetString(bytes);
            Debug.Log($"[NetworkManager] Mensaje: {msg}");

            if (msg.Contains("match_found") || msg.Contains("opponent_found"))
                SetStatus("!Partida encontrada!");
            else if (msg.Contains("joined") || msg.Contains("waiting"))
                SetStatus("Esperando rival...");
            else if (msg.Contains("error"))
            {
                SetStatus("Error del servidor");
                SetFindButtonInteractable(true);
            }
        };

        await webSocket.Connect();
    }

    private void OnFindMatchClick()
    {
        SetStatus("Buscando rivales...");
        SetFindButtonInteractable(false);

        var payload = new JoinMatchPayload { eventType = "join_match" };
        string json = JsonUtility.ToJson(payload);
        Send(json);
    }

    public async void Send(string message)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[NetworkManager] No se puede enviar: no conectado");
            return;
        }

        try
        {
            await webSocket.SendText(message);
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] Error enviando: {e.Message}");
        }
    }

    public async void Disconnect()
    {
        if (webSocket != null)
        {
            await webSocket.Close();
            webSocket = null;
        }
    }

    private void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private void SetFindButtonInteractable(bool value)
    {
        if (findMatchButton != null) findMatchButton.interactable = value;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Disconnect();
    }

    [System.Serializable]
    private class JoinMatchPayload
    {
        public string eventType;
    }
}

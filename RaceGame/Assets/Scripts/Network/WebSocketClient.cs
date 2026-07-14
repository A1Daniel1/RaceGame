using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string, string> OnMessage;
    public event Action<string> OnError;

    private WebSocket _websocket;
    private string _url;
    private bool _isConnected;

    public bool IsConnected => _isConnected;

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

    private void Update()
    {
        _websocket?.DispatchMessageQueue();
    }

    public async void Connect(string url)
    {
        _url = url;
        _websocket = new WebSocket(url);

        _websocket.OnOpen += () =>
        {
            _isConnected = true;
            Debug.Log("[WebSocket] Conectado a " + _url);
            OnConnected?.Invoke();
        };

        _websocket.OnError += (e) =>
        {
            Debug.LogError("[WebSocket] Error: " + e);
            OnError?.Invoke(e);
        };

        _websocket.OnClose += (e) =>
        {
            _isConnected = false;
            Debug.Log("[WebSocket] Desconectado");
            OnDisconnected?.Invoke();
        };

        _websocket.OnMessage += (bytes) =>
        {
            string raw = Encoding.UTF8.GetString(bytes);
            HandleRawMessage(raw);
        };

        await _websocket.Connect();
    }

    public async void Disconnect()
    {
        if (_websocket != null)
        {
            await _websocket.Close();
            _websocket = null;
            _isConnected = false;
        }
    }

    public async void SendEvent(string eventName, string jsonData)
    {
        if (_websocket == null || !_isConnected)
        {
            Debug.LogWarning("[WebSocket] No se puede enviar: no conectado");
            return;
        }

        string payload = JsonUtility.ToJson(new WebSocketMessage
        {
            _event = eventName,
            data = jsonData
        });

        await _websocket.SendText(payload);
    }

    private void HandleRawMessage(string raw)
    {
        try
        {
            WebSocketMessage msg = JsonUtility.FromJson<WebSocketMessage>(raw);
            OnMessage?.Invoke(msg._event, msg.data);
        }
        catch (Exception e)
        {
            Debug.LogError("[WebSocket] Error parseando mensaje: " + e.Message);
            OnError?.Invoke(e.Message);
        }
    }

    private async void OnApplicationQuit()
    {
        if (_websocket != null)
        {
            await _websocket.Close();
        }
    }

    [Serializable]
    private class WebSocketMessage
    {
        public string _event;
        public string data;
    }
}

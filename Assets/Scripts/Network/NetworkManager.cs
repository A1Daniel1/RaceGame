using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using NativeWebSocket;
using System.Text;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Menu UI")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button createRoomButton;

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
        BuildVisualUIIfNeeded();
        if (statusText != null) statusText.text = "Desconectado";
        if (findMatchButton != null)
        {
            findMatchButton.onClick.AddListener(OnFindMatchClick);
            findMatchButton.interactable = false;
        }
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomClick);
            createRoomButton.interactable = false;
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket?.DispatchMessageQueue();
#endif
    }

    private void BuildVisualUIIfNeeded()
    {
        if (menuPanel == null)
            menuPanel = GameObject.Find("MenuPanel");

        if (menuPanel == null)
            return;

        if (statusText == null)
        {
            CreateText(menuPanel.transform, "Multijugador", new Vector2(260f, 48f), new Vector2(0f, 150f), 32, Color.white, true);
            CreateText(menuPanel.transform, "Conecta al game server y gestiona la sala", new Vector2(420f, 32f), new Vector2(0f, 95f), 20, new Color(1f, 1f, 1f, 0.8f));
            statusText = CreateText(menuPanel.transform, "Desconectado", new Vector2(320f, 38f), new Vector2(0f, 25f), 22, new Color(0.9f, 0.95f, 1f, 1f));
        }

        if (findMatchButton == null)
            findMatchButton = CreateButton(menuPanel.transform, "Buscar partida", new Vector2(250f, 64f), new Vector2(-120f, -80f), "FindMatchButton", new Color(0.16f, 0.58f, 1f, 1f));

        if (createRoomButton == null)
            createRoomButton = CreateButton(menuPanel.transform, "Crear sala", new Vector2(250f, 64f), new Vector2(120f, -80f), "CreateRoomButton", new Color(0.19f, 0.83f, 0.47f, 1f));
    }

    private TMP_Text CreateText(Transform parent, string text, Vector2 size, Vector2 position, int fontSize, Color color, bool header = false)
    {
        var textObject = new GameObject(header ? "TitleText" : "Text");
        textObject.transform.SetParent(parent, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateButton(Transform parent, string label, Vector2 size, Vector2 position, string name, Color color)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var image = buttonObject.AddComponent<Image>();
        image.color = color;

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = new ColorBlock
        {
            normalColor = color,
            highlightedColor = new Color(color.r + 0.05f, color.g + 0.05f, color.b + 0.05f, 1f),
            pressedColor = new Color(color.r - 0.04f, color.g - 0.04f, color.b - 0.04f, 1f),
            selectedColor = color,
            disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var labelText = textObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 22;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;
        return button;
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
            SetButtonsInteractable(true);
        };

        webSocket.OnError += (errorMsg) =>
        {
            Debug.LogError($"[NetworkManager] Error: {errorMsg}");
            SetStatus("Error de conexión");
            SetButtonsInteractable(false);
        };

        webSocket.OnClose += (closeCode) =>
        {
            Debug.Log($"[NetworkManager] Desconectado (codigo: {closeCode})");
            SetStatus("Desconectado");
            SetButtonsInteractable(false);
        };

        webSocket.OnMessage += (bytes) =>
        {
            string msg = Encoding.UTF8.GetString(bytes);
            Debug.Log($"[NetworkManager] Mensaje: {msg}");

            if (msg.Contains("match_found") || msg.Contains("opponent_found") || msg.Contains("room_created") || msg.Contains("room_joined"))
                SetStatus("¡Sala lista! Esperando jugadores...");
            else if (msg.Contains("joined") || msg.Contains("waiting"))
                SetStatus("Esperando rival...");
            else if (msg.Contains("error"))
            {
                SetStatus("Error del servidor");
                SetButtonsInteractable(true);
            }
        };

        await webSocket.Connect();
    }

    private void OnFindMatchClick()
    {
        SetStatus("Buscando rivales...");
        SetButtonsInteractable(false);
        SendJson(new MatchEventPayload { eventType = "join_match" });
    }

    private void OnCreateRoomClick()
    {
        SetStatus("Creando sala...");
        SetButtonsInteractable(false);
        SendJson(new MatchEventPayload { eventType = "create_room" });
    }

    public async void SendJson(object payload)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[NetworkManager] No se puede enviar: no conectado");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(payload);
            await webSocket.SendText(json);
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

    private void SetButtonsInteractable(bool value)
    {
        if (findMatchButton != null) findMatchButton.interactable = value;
        if (createRoomButton != null) createRoomButton.interactable = value;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Disconnect();
    }

    [System.Serializable]
    private class MatchEventPayload
    {
        public string eventType;
    }
}

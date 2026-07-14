using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject authPanel;
    [SerializeField] private GameObject menuPanel;

    [Header("Mode UI")]
    [SerializeField] private Button soloButton;
    [SerializeField] private Button multiplayerButton;

    [Header("Auth UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button switchModeButton;
    [SerializeField] private Button showPasswordButton;
    [SerializeField] private TMP_Text errorText;

    private bool isRegisterMode;
    private bool passwordVisible;

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
        EnsureCanvasAndEventSystem();
        BuildVisualUIIfNeeded();
        if (soloButton != null) soloButton.onClick.AddListener(StartSinglePlayer);
        if (multiplayerButton != null) multiplayerButton.onClick.AddListener(ShowAuthPanel);
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClick);
        if (switchModeButton != null) switchModeButton.onClick.AddListener(ToggleAuthMode);
        if (showPasswordButton != null) showPasswordButton.onClick.AddListener(TogglePasswordVisibility);

        ShowPanel(modePanel);
        RefreshAuthView();
        if (errorText != null) errorText.text = string.Empty;
    }

    private void StartSinglePlayer()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoTo("Forest");
        else
            SceneManager.LoadScene("Forest");
    }

    private void ShowAuthPanel()
    {
        isRegisterMode = false;
        RefreshAuthView();
        ShowPanel(authPanel);
    }

    private void ToggleAuthMode()
    {
        isRegisterMode = !isRegisterMode;
        RefreshAuthView();
    }

    private void TogglePasswordVisibility()
    {
        passwordVisible = !passwordVisible;
        SetPasswordVisibility(passwordInput, passwordVisible);
        SetPasswordVisibility(confirmPasswordInput, passwordVisible);
        RefreshAuthView();
    }

    private void RefreshAuthView()
    {
        if (titleText != null) titleText.text = isRegisterMode ? "Crear cuenta" : "Iniciar sesión";
        if (subtitleText != null) subtitleText.text = isRegisterMode ? "Crea tu perfil para entrar al lobby" : "Ingresa con tu usuario y contraseña";

        if (switchModeButton != null)
        {
            var label = switchModeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = isRegisterMode ? "¿Ya tienes cuenta? Inicia sesión" : "¿No tienes cuenta? Crear cuenta";
        }

        if (showPasswordButton != null)
        {
            var label = showPasswordButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = passwordVisible ? "Ocultar" : "Ver";
        }

        if (submitButton != null)
        {
            var label = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = isRegisterMode ? "Crear cuenta" : "Entrar";
        }

        if (confirmPasswordInput != null)
            confirmPasswordInput.gameObject.SetActive(isRegisterMode);
    }

    private void OnSubmitClick()
    {
        string user = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
        string pass = passwordInput != null ? passwordInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            if (errorText != null) errorText.text = "Completa todos los campos.";
            return;
        }

        if (isRegisterMode)
        {
            string confirm = confirmPasswordInput != null ? confirmPasswordInput.text.Trim() : string.Empty;
            if (!string.Equals(pass, confirm))
            {
                if (errorText != null) errorText.text = "Las contraseñas no coinciden.";
                return;
            }
        }

        if (errorText != null) errorText.text = isRegisterMode ? "Creando cuenta..." : "Iniciando sesión...";
        if (submitButton != null) submitButton.interactable = false;

        if (isRegisterMode)
            StartCoroutine(RegisterCoroutine(user, pass));
        else
            StartCoroutine(LoginCoroutine(user, pass));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        using var request = new UnityWebRequest(NetworkConfig.LoginUrl, "POST");
        var payload = new AuthPayload { username = username, password = password };
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = ParseAuthResponse(request.downloadHandler.text);
            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                GameManager.Instance.JWT = response.Token;
                GameManager.Instance.Username = username;
                PlayerPrefs.SetString("auth_token", response.Token);
                PlayerPrefs.Save();

                ShowPanel(menuPanel);
                if (errorText != null) errorText.text = string.Empty;
                NetworkManager.Instance.Connect();
            }
            else
            {
                if (errorText != null) errorText.text = "Respuesta inesperada del servidor.";
                if (submitButton != null) submitButton.interactable = true;
            }
        }
        else
        {
            if (errorText != null) errorText.text = ParseError(request);
            if (submitButton != null) submitButton.interactable = true;
        }
    }

    private IEnumerator RegisterCoroutine(string username, string password)
    {
        using var request = new UnityWebRequest(NetworkConfig.RegisterUrl, "POST");
        var payload = new AuthPayload { username = username, password = password };
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = ParseAuthResponse(request.downloadHandler.text);
            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                GameManager.Instance.JWT = response.Token;
                GameManager.Instance.Username = username;
                PlayerPrefs.SetString("auth_token", response.Token);
                PlayerPrefs.Save();

                ShowPanel(menuPanel);
                if (errorText != null) errorText.text = string.Empty;
                NetworkManager.Instance.Connect();
            }
            else
            {
                if (errorText != null) errorText.text = "Cuenta creada, pero no se recibió token.";
                if (submitButton != null) submitButton.interactable = true;
            }
        }
        else
        {
            if (errorText != null) errorText.text = ParseError(request);
            if (submitButton != null) submitButton.interactable = true;
        }
    }

    private void ShowPanel(GameObject panel)
    {
        if (modePanel != null) modePanel.SetActive(panel == modePanel);
        if (authPanel != null) authPanel.SetActive(panel == authPanel);
        if (menuPanel != null) menuPanel.SetActive(panel == menuPanel);
    }

    private void BuildVisualUIIfNeeded()
    {
        if (modePanel != null && authPanel != null && menuPanel != null)
            return;

        var canvas = FindOrCreateCanvas();

        if (modePanel == null)
            modePanel = CreatePanel(canvas.transform, "ModePanel", new Vector2(760f, 520f), Vector2.zero, new Color(0.04f, 0.07f, 0.13f, 0.97f));

        if (authPanel == null)
            authPanel = CreatePanel(canvas.transform, "AuthPanel", new Vector2(760f, 520f), Vector2.zero, new Color(0.04f, 0.07f, 0.13f, 0.97f));

        if (menuPanel == null)
            menuPanel = CreatePanel(canvas.transform, "MenuPanel", new Vector2(760f, 520f), Vector2.zero, new Color(0.04f, 0.07f, 0.13f, 0.97f));

        if (modePanel.transform.childCount == 0)
        {
            CreateDecorativeGlow(modePanel.transform);
            CreateText(modePanel.transform, "RaceGame", new Vector2(360f, 90f), new Vector2(0f, 170f), 46, Color.white, true);
            CreateText(modePanel.transform, "Elige tu modo de competición", new Vector2(460f, 40f), new Vector2(0f, 120f), 24, new Color(0.88f, 0.92f, 1f, 0.9f));
            soloButton = CreateButton(modePanel.transform, "Solitario", new Vector2(250f, 86f), new Vector2(-170f, 10f), "SoloButton", new Color(0.18f, 0.84f, 0.49f, 1f));
            multiplayerButton = CreateButton(modePanel.transform, "Multijugador", new Vector2(250f, 86f), new Vector2(170f, 10f), "MultiplayerButton", new Color(0.16f, 0.58f, 1f, 1f));
            CreateText(modePanel.transform, "Solitario: carrera local\nMultijugador: login, matchmaking y sala en tiempo real", new Vector2(560f, 80f), new Vector2(0f, -115f), 20, new Color(0.84f, 0.9f, 1f, 0.82f));
        }

        if (authPanel.transform.childCount == 0)
        {
            CreateDecorativeGlow(authPanel.transform);
            titleText = CreateText(authPanel.transform, "Iniciar sesión", new Vector2(320f, 60f), new Vector2(0f, 180f), 34, Color.white, true);
            subtitleText = CreateText(authPanel.transform, "Ingresa con tu usuario y contraseña", new Vector2(440f, 34f), new Vector2(0f, 130f), 20, new Color(1f, 1f, 1f, 0.8f));
            usernameInput = CreateInputField(authPanel.transform, "Usuario", new Vector2(360f, 58f), new Vector2(0f, 62f), "UsernameInput");
            passwordInput = CreateInputField(authPanel.transform, "Contraseña", new Vector2(360f, 58f), new Vector2(0f, -8f), "PasswordInput", true);
            confirmPasswordInput = CreateInputField(authPanel.transform, "Confirmar contraseña", new Vector2(360f, 58f), new Vector2(0f, -82f), "ConfirmPasswordInput", true);
            confirmPasswordInput.gameObject.SetActive(false);
            showPasswordButton = CreateButton(authPanel.transform, "Ver", new Vector2(100f, 44f), new Vector2(175f, -8f), "ShowPasswordButton", new Color(0.27f, 0.32f, 0.38f, 1f));
            submitButton = CreateButton(authPanel.transform, "Entrar", new Vector2(220f, 62f), new Vector2(0f, -160f), "SubmitButton", new Color(0.16f, 0.58f, 1f, 1f));
            switchModeButton = CreateButton(authPanel.transform, "¿No tienes cuenta? Crear cuenta", new Vector2(320f, 44f), new Vector2(0f, -225f), "SwitchModeButton", new Color(0.27f, 0.32f, 0.38f, 1f));
            errorText = CreateText(authPanel.transform, string.Empty, new Vector2(420f, 34f), new Vector2(0f, -275f), 18, new Color(1f, 0.45f, 0.45f, 1f));
        }

        if (menuPanel.transform.childCount == 0)
        {
            CreateDecorativeGlow(menuPanel.transform);
            CreateText(menuPanel.transform, "Multijugador", new Vector2(260f, 48f), new Vector2(0f, 150f), 32, Color.white, true);
            CreateText(menuPanel.transform, "Conecta al game server y gestiona la sala", new Vector2(420f, 32f), new Vector2(0f, 95f), 20, new Color(1f, 1f, 1f, 0.8f));
            CreateText(menuPanel.transform, "Estado: Desconectado", new Vector2(320f, 38f), new Vector2(0f, 25f), 22, new Color(0.9f, 0.95f, 1f, 1f), false);
        }
    }

    private void EnsureCanvasAndEventSystem()
    {
        var existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas == null)
        {
            var canvasObject = new GameObject("RaceCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private Canvas FindOrCreateCanvas()
    {
        var existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
            return existingCanvas;

        var canvasObject = new GameObject("RaceCanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel;
    }

    private void CreateDecorativeGlow(Transform parent)
    {
        var glow = new GameObject("Glow");
        glow.transform.SetParent(parent, false);
        var rect = glow.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(720f, 480f);
        rect.anchoredPosition = Vector2.zero;
        var image = glow.AddComponent<Image>();
        image.color = new Color(0.17f, 0.28f, 0.45f, 0.16f);
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
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholderText, Vector2 size, Vector2 position, string name, bool isPassword = false)
    {
        var inputObject = new GameObject(name);
        inputObject.transform.SetParent(parent, false);
        var rect = inputObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var image = inputObject.AddComponent<Image>();
        image.color = new Color(0.95f, 0.97f, 1f, 0.14f);

        var input = inputObject.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.transition = Selectable.Transition.None;
        input.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.inputType = isPassword ? TMP_InputField.InputType.Password : TMP_InputField.InputType.Standard;

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(inputObject.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        input.textComponent = text;

        var placeholderObject = new GameObject("Placeholder");
        placeholderObject.transform.SetParent(inputObject.transform, false);
        var placeholderRect = placeholderObject.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10f, 6f);
        placeholderRect.offsetMax = new Vector2(-10f, -6f);
        var placeholder = placeholderObject.AddComponent<TextMeshProUGUI>();
        placeholder.fontSize = 24;
        placeholder.color = new Color(1f, 1f, 1f, 0.6f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.text = placeholderText;
        placeholder.raycastTarget = false;
        input.placeholder = placeholder;

        return input;
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
            highlightedColor = new Color(Mathf.Min(1f, color.r + 0.06f), Mathf.Min(1f, color.g + 0.06f), Mathf.Min(1f, color.b + 0.06f), 1f),
            pressedColor = new Color(Mathf.Max(0f, color.r - 0.05f), Mathf.Max(0f, color.g - 0.05f), Mathf.Max(0f, color.b - 0.05f), 1f),
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

    private void SetPasswordVisibility(TMP_InputField input, bool visible)
    {
        if (input == null) return;
        input.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        input.inputType = visible ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.Password;
        input.ForceLabelUpdate();
    }

    private static string ParseError(UnityWebRequest request)
    {
        try
        {
            var err = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
            if (!string.IsNullOrEmpty(err.message)) return err.message;
            if (!string.IsNullOrEmpty(err.error)) return err.error;
        }
        catch { }
        return request.error ?? "Error desconocido";
    }

    private AuthResponse ParseAuthResponse(string payload)
    {
        try
        {
            var response = JsonUtility.FromJson<AuthResponse>(payload);
            return response;
        }
        catch { return null; }
    }

    [System.Serializable]
    private class AuthPayload { public string username; public string password; }

    [System.Serializable]
    private class AuthResponse { public string token; public string accessToken; public string jwt; public string message; public string error; public string Token => !string.IsNullOrEmpty(token) ? token : !string.IsNullOrEmpty(accessToken) ? accessToken : jwt; }

    [System.Serializable]
    private class ErrorResponse { public string message; public string error; }
}

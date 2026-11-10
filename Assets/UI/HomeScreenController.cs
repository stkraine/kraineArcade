using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controller for the main Home Screen UI. This acts as the central menu hub.
/// Builds a basic runtime GUI when UI references are not assigned in the scene,
/// and uses the AssetManager to access shared functionality.
/// </summary>
public class HomeScreenController : MonoBehaviour
{
    [Header("Optional UI References (auto-built at runtime if unset)")]
    public Text titleText;
    public Text searchInputText;
    public Text statusText;
    public Button selectGameButton;

    private const string DefaultTitle = "Kraine Arcade";
    private const string DefaultSubtitle = "Select a game to play";

    private void Start()
    {
        EnsureEventSystem();
        EnsureBasicGui();

        // --- Integration Test Hook ---
        // We test here that the shared manager is accessible.
        if (AssetManager.Instance == null)
        {
            Debug.LogError("HomeScreenController failed integration test: AssetManager is null!");
            SetStatus("AssetManager not found. Shared services unavailable.");
        }
        else
        {
            Debug.Log("HomeScreenController initialized successfully. Integration Test Passed: Shared Library Linked.");
        }

        SetupSearchFunction();
    }

    private void SetupSearchFunction()
    {
        // Connect UI elements to logic
        if (selectGameButton != null)
        {
            selectGameButton.onClick.RemoveListener(OnSelectGame);
            selectGameButton.onClick.AddListener(OnSelectGame);
        }
        else
        {
            Debug.LogWarning("HomeScreenController: selectGameButton is not assigned; no game selection action wired.");
        }
    }

    public void OnSelectGame()
    {
        SetStatus("Loading Tetris...");

        if (AssetManager.Instance == null)
        {
            Debug.LogError("Could not select game: AssetManager is null.");
            SetStatus("Cannot load game: AssetManager unavailable.");
            return;
        }

        // Example: Fetching a game asset via the shared manager
        object gameAsset = AssetManager.Instance.LoadAsset("TETRIS_GAME_MODULE");

        if (gameAsset != null)
        {
            Debug.Log("Navigating to game module using AssetManager.");
            SetStatus("Tetris module ready. Scene loading is not wired yet.");
            // Logic to load the game scene/module would go here
        }
        else
        {
            Debug.LogError("Could not find the game module asset!");
            SetStatus("Could not find Tetris module.");
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
        Debug.Log("HomeScreenController created a default EventSystem for UI input.");
    }

    private void EnsureBasicGui()
    {
        // If key elements are already wired (e.g., from a scene/prefab), keep them.
        if (titleText != null && selectGameButton != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = CreateCanvas();

        RectTransform root = CreatePanel(canvas.transform, "HomeScreenPanel",
            new Color(0.08f, 0.1f, 0.14f, 1f));

        if (titleText == null)
            titleText = CreateText(root, "TitleText", DefaultTitle, 42, FontStyle.Bold, TextAnchor.MiddleCenter);

        CreateText(root, "SubtitleText", DefaultSubtitle, 22, FontStyle.Normal, TextAnchor.MiddleCenter);

        if (searchInputText == null)
            searchInputText = CreateText(root, "SearchPlaceholderText", "Search: (coming soon)", 18, FontStyle.Italic, TextAnchor.MiddleCenter);

        if (selectGameButton == null)
            selectGameButton = CreateButton(root, "TetrisButton", "Play Tetris");

        if (statusText == null)
            statusText = CreateText(root, "StatusText", "Welcome to the arcade.", 18, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private static Canvas CreateCanvas()
    {
        var canvasGo = new GameObject("HomeScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        var panelGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelGo.transform.SetParent(parent, false);

        var image = panelGo.GetComponent<Image>();
        image.color = color;

        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var layout = panelGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 80, 80);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return rect;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.font = GetBuiltinFont();

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredHeight = Mathf.Max(36, fontSize * 1.6f);

        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.85f, 1f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredHeight = 72f;

        var labelText = CreateText(go.transform, "Label", label, 26, FontStyle.Bold, TextAnchor.MiddleCenter);
        var labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private static Font GetBuiltinFont()
    {
        // Unity 6+ commonly provides LegacyRuntime.ttf; older projects may have Arial.ttf.
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }
}
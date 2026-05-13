using UnityEngine;
using UnityEngine.UI;

public class GalleryManager : MonoBehaviour
{
    [Tooltip("Gallery 全局配置（留空则自动从 Resources 加载）")]
    [SerializeField] private GallerySettings settings;

    [Header("入场文字")]
    [Tooltip("入场黑屏时显示的文字")]
    [TextArea(2, 6)]
    [SerializeField] private string introText = "";
    [Tooltip("文字在屏幕上的位置 (0~1 归一化坐标)")]
    [SerializeField] private Vector2 introTextPosition = new Vector2(0.5f, 0.4f);
    [Tooltip("文字大小")]
    [SerializeField] private int introFontSize = 36;
    [Tooltip("文字颜色")]
    [SerializeField] private Color introTextColor = Color.white;

    private Canvas overlayCanvas;
    private Image blackOverlay;
    private Text introLabel;
    private float introTimer;
    private bool introFinished;

    public GallerySettings Settings
    {
        get
        {
            if (settings == null)
                settings = Resources.Load<GallerySettings>("GallerySettings");
            return settings;
        }
    }

    public static GalleryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        ApplyAmbientLight();
        CreateIntroOverlay();
    }

    private void Update()
    {
        if (!introFinished)
            UpdateIntro();
    }

    private void ApplyAmbientLight()
    {
        var s = Settings;
        if (s == null) return;
        RenderSettings.ambientLight = s.ambientColor * s.ambientBrightness;
    }

    private void CreateIntroOverlay()
    {
        var s = Settings;
        if (s == null || s.introDuration <= 0)
        {
            introFinished = true;
            return;
        }

        var canvasGO = new GameObject("IntroCanvas");
        canvasGO.transform.SetParent(transform);
        overlayCanvas = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 10000;
        canvasGO.AddComponent<CanvasScaler>();

        var bgGO = new GameObject("BlackBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        blackOverlay = bgGO.AddComponent<Image>();
        blackOverlay.color = Color.black;
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        if (!string.IsNullOrEmpty(introText))
        {
            var textGO = new GameObject("IntroText");
            textGO.transform.SetParent(canvasGO.transform, false);
            introLabel = textGO.AddComponent<Text>();
            introLabel.text = introText;
            introLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            introLabel.fontSize = introFontSize;
            introLabel.color = introTextColor;
            introLabel.alignment = TextAnchor.MiddleCenter;
            introLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            introLabel.verticalOverflow = VerticalWrapMode.Overflow;

            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = introTextPosition;
            textRT.anchorMax = introTextPosition;
            textRT.anchoredPosition = Vector2.zero;
            textRT.sizeDelta = new Vector2(800, 200);
        }

        introTimer = 0f;
        introFinished = false;
        GalleryPlayer.Freeze();
    }

    private void UpdateIntro()
    {
        var s = Settings;
        if (s == null) { introFinished = true; return; }

        introTimer += Time.deltaTime;
        float totalTime = s.introDuration + s.introFadeTime;

        if (introTimer >= s.introDuration && introTimer < totalTime)
        {
            float fadeProgress = (introTimer - s.introDuration) / s.introFadeTime;
            if (blackOverlay != null)
                blackOverlay.color = new Color(0, 0, 0, 1f - fadeProgress);
            if (introLabel != null)
            {
                var c = introTextColor;
                introLabel.color = new Color(c.r, c.g, c.b, 1f - fadeProgress);
            }
        }

        if (introTimer >= totalTime)
        {
            introFinished = true;
            if (overlayCanvas != null)
                Destroy(overlayCanvas.gameObject);

            GalleryPlayer.Unfreeze();
        }
    }
}

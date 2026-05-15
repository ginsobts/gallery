using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GalleryPlayer))]
public class GalleryVehicle : MonoBehaviour
{
    [System.Serializable]
    public struct Vehicle
    {
        [Tooltip("交通工具名称")]
        public string name;
        [Tooltip("对应的 Sprite")]
        public Sprite sprite;
        [Tooltip("帧动画（留空则使用单张 Sprite）")]
        public Sprite[] walkFrames;
        [Tooltip("移动速度")]
        public float speed;
    }

    [Header("交通工具列表")]
    [Tooltip("配置的交通工具（不包含步行，步行是默认状态）")]
    [SerializeField] private Vehicle[] vehicles;

    [Header("切换按键")]
    [Tooltip("按此键循环切换交通工具（输入按键名称，如 Tab, Q, F 等）")]
    [SerializeField] private string switchKeyName = "V";
    [Tooltip("是否显示 UI 按钮")]
    [SerializeField] private bool showUIButton = true;

    [Header("UI 样式")]
    [SerializeField] private Vector2 buttonSize = new Vector2(160, 36);
    [SerializeField] private Vector2 buttonOffset = new Vector2(20, 20);
    [SerializeField] private int fontSize = 16;

    private int currentIndex = -1;
    public int CurrentIndex => currentIndex;
    public event System.Action<int> OnVehicleChanged;
    private GalleryPlayer player;
    private SpriteRenderer sr;
    private FrameAnimator frameAnimator;
    private float defaultSpeed;
    private Sprite defaultSprite;

    private Canvas uiCanvas;
    private Text vehicleLabel;
    private KeyCode switchKey = KeyCode.V;
    private Image editorBtnImage;
    private static readonly Color EditorBtnOff = new Color(0.2f, 0.35f, 0.5f, 0.8f);
    private static readonly Color EditorBtnOn = new Color(0.3f, 0.7f, 0.4f, 0.9f);

    private void Awake()
    {
        player = GetComponent<GalleryPlayer>();
    }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        frameAnimator = GetComponent<FrameAnimator>();
        defaultSpeed = player.GetMoveSpeed();
        defaultSprite = sr.sprite;

        if (!string.IsNullOrEmpty(switchKeyName))
        {
            try { switchKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), switchKeyName, true); }
            catch { switchKey = KeyCode.V; }
        }

        currentIndex = -1;

        if (showUIButton)
            CreateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
            CycleVehicle();

        if (editorBtnImage != null)
        {
            bool editing = RuntimeEditor.Instance != null && RuntimeEditor.Instance.IsEditing;
            editorBtnImage.color = editing ? EditorBtnOn : EditorBtnOff;
        }
    }

    public void CycleVehicle()
    {
        if (vehicles == null || vehicles.Length == 0) return;

        currentIndex++;
        if (currentIndex >= vehicles.Length)
        {
            currentIndex = -1;
            ApplyWalk();
        }
        else
        {
            ApplyVehicle(currentIndex);
        }
        OnVehicleChanged?.Invoke(currentIndex);
    }

    private void ApplyWalk()
    {
        player.SetMoveSpeed(defaultSpeed);
        if (frameAnimator != null)
            frameAnimator.Stop();
        if (defaultSprite != null)
            sr.sprite = defaultSprite;
        if (vehicleLabel != null)
            vehicleLabel.text = "步行";
    }

    private void ApplyVehicle(int index)
    {
        var v = vehicles[index];
        if (v.speed > 0)
            player.SetMoveSpeed(v.speed);

        if (v.walkFrames != null && v.walkFrames.Length > 0)
        {
            if (frameAnimator == null)
                frameAnimator = gameObject.AddComponent<FrameAnimator>();
            frameAnimator.SetFramesAndPlay(v.walkFrames);
        }
        else if (v.sprite != null)
        {
            if (frameAnimator != null)
                frameAnimator.Stop();
            sr.sprite = v.sprite;
            sr.color = Color.white;
        }

        if (vehicleLabel != null)
            vehicleLabel.text = v.name;
    }

    private void CreateUI()
    {
        var canvasGO = new GameObject("VehicleUI");
        canvasGO.transform.SetParent(transform);
        uiCanvas = canvasGO.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 600;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        CreateVehicleButton(canvasGO.transform);
        CreateEditorButton(canvasGO.transform);
    }

    private void CreateVehicleButton(Transform parent)
    {
        var btnGO = new GameObject("SwitchButton");
        btnGO.transform.SetParent(parent, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 1);
        btnRT.anchorMax = new Vector2(0, 1);
        btnRT.pivot = new Vector2(0, 1);
        btnRT.anchoredPosition = new Vector2(buttonOffset.x, -buttonOffset.y);
        btnRT.sizeDelta = buttonSize;

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(CycleVehicle);
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f, 0.9f);
        btn.colors = colors;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        vehicleLabel = textGO.AddComponent<Text>();
        vehicleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        vehicleLabel.fontSize = fontSize;
        vehicleLabel.alignment = TextAnchor.MiddleCenter;
        vehicleLabel.color = Color.white;
        vehicleLabel.text = "步行";
    }

    private void CreateEditorButton(Transform parent)
    {
        var btnGO = new GameObject("EditorButton");
        btnGO.transform.SetParent(parent, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 1);
        btnRT.anchorMax = new Vector2(0, 1);
        btnRT.pivot = new Vector2(0, 1);
        btnRT.anchoredPosition = new Vector2(buttonOffset.x, -(buttonOffset.y + buttonSize.y + 8));
        btnRT.sizeDelta = buttonSize;

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = EditorBtnOff;
        editorBtnImage = btnImg;

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(OnEditorButtonClick);
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.45f, 0.6f, 0.9f);
        btn.colors = colors;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var label = textGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "编辑模式";
    }

    private void OnEditorButtonClick()
    {
        var editor = RuntimeEditor.Instance;
        if (editor != null)
            editor.ToggleEditor();
    }
}

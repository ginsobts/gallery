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
    [Tooltip("第 0 个视为默认（步行）")]
    [SerializeField] private Vehicle[] vehicles;

    [Header("切换按键")]
    [Tooltip("按此键循环切换交通工具")]
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;
    [Tooltip("是否显示 UI 切换按钮")]
    [SerializeField] private bool showUIButton = true;

    [Header("UI 样式")]
    [SerializeField] private Vector2 buttonSize = new Vector2(180, 45);
    [SerializeField] private Vector2 buttonOffset = new Vector2(20, 60);
    [SerializeField] private int fontSize = 20;

    private int currentIndex;
    public int CurrentIndex => currentIndex;
    public event System.Action<int> OnVehicleChanged;
    private GalleryPlayer player;
    private SpriteRenderer sr;
    private FrameAnimator frameAnimator;

    private Canvas uiCanvas;
    private Text vehicleLabel;

    private void Awake()
    {
        player = GetComponent<GalleryPlayer>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        frameAnimator = GetComponent<FrameAnimator>();

        if (vehicles == null || vehicles.Length == 0)
        {
            vehicles = new Vehicle[]
            {
                new Vehicle { name = "步行", sprite = null, walkFrames = null, speed = 5f }
            };
        }

        currentIndex = 0;
        ApplyVehicle(currentIndex);

        if (showUIButton)
            CreateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
            CycleVehicle();
    }

    public void CycleVehicle()
    {
        if (vehicles.Length <= 1) return;
        currentIndex = (currentIndex + 1) % vehicles.Length;
        ApplyVehicle(currentIndex);
    }

    private void ApplyVehicle(int index)
    {
        var v = vehicles[index];
        player.SetMoveSpeed(v.speed);
        OnVehicleChanged?.Invoke(index);

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

        var btnGO = new GameObject("SwitchButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        if (btnRT == null) btnRT = btnGO.AddComponent<RectTransform>();
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
        vehicleLabel.text = vehicles[currentIndex].name;
    }
}

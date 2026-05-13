using UnityEngine;

[System.Serializable]
public class FrameEffectSet
{
    [Tooltip("放大查看图片")]
    public bool zoom = false;

    [Tooltip("显示文字")]
    public bool showText = false;
    [TextArea(2, 5)]
    public string text = "";
    public float textDuration = 4f;

    [Tooltip("播放音效")]
    public bool playSound = false;
    public AudioClip soundClip;
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    [Tooltip("切换BGM")]
    public bool changeBGM = false;
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 0.6f;

    [Tooltip("改变天气")]
    public bool changeWeather = false;
    public GalleryWeather.WeatherType weatherType = GalleryWeather.WeatherType.Snow;
    public int weatherParticles = 60;
    public Color weatherColor = Color.white;

    [Tooltip("改变背景颜色")]
    public bool changeBackground = false;
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.1f);
    public float backgroundFade = 1.5f;

    [Tooltip("改变灯光亮度")]
    public bool changeBrightness = false;
    [Range(0f, 2f)]
    public float brightness = 0.5f;

    [Tooltip("跳转场景")]
    public bool loadScene = false;
    public string sceneName = "";

    [Tooltip("显示/隐藏物体")]
    public bool toggleObject = false;
    public GameObject targetObject;
    public bool objectShow = true;
}

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class GalleryFrame : MonoBehaviour
{
    [Header("图片")]
    [SerializeField] private Sprite image;

    [Header("显示")]
    [SerializeField] private int sortingOrder = 0;

    [Header("淡入触发")]
    [SerializeField] private bool fadeInOnApproach = false;
    [SerializeField] private float fadeDistance = 5f;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("说明文字")]
    [TextArea(1, 3)]
    [SerializeField] private string caption = "";
    [SerializeField] private Color captionColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);
    [SerializeField] private float captionSize = 0.15f;

    [Header("── 按键交互 ──")]
    [SerializeField] private bool enableKeyInteract = false;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private FrameEffectSet keyEffects = new FrameEffectSet();

    [Header("── 靠近触发 ──")]
    [SerializeField] private bool enableApproachTrigger = false;
    [SerializeField] private float approachDistance = 4f;
    [SerializeField] private bool approachOnlyOnce = true;
    [SerializeField] private FrameEffectSet approachEffects = new FrameEffectSet();

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Transform playerTransform;
    private float currentAlpha;
    private bool revealed;
    private TextMesh captionTM;
    private MeshRenderer captionMR;
    private bool keyInteractReady;
    private GameObject promptGO;
    private AudioSource audioSrc;
    private bool approachTriggered;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;

        if (image != null) { sr.sprite = image; sr.color = Color.white; }
        if (sr.sprite != null) sr.sortingOrder = sortingOrder;

        if (fadeInOnApproach)
        {
            currentAlpha = 0f;
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
        }
        else
        {
            currentAlpha = 1f;
            revealed = true;
        }
    }

    private void Start()
    {
        var player = GalleryPlayer.Instance;
        if (player != null) playerTransform = player.transform;
        if (!string.IsNullOrEmpty(caption)) CreateCaption();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (fadeInOnApproach && !revealed)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= fadeDistance)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, fadeSpeed * Time.deltaTime);
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, currentAlpha);
                if (captionTM != null)
                    captionTM.color = new Color(captionColor.r, captionColor.g, captionColor.b, currentAlpha * captionColor.a);
                if (currentAlpha >= 1f) revealed = true;
            }
        }

        float playerDist = Vector2.Distance(transform.position, playerTransform.position);

        if (enableApproachTrigger && !approachTriggered)
        {
            if (playerDist <= approachDistance)
            {
                ExecuteEffects(approachEffects);
                if (approachOnlyOnce) approachTriggered = true;
            }
        }
        if (enableApproachTrigger && !approachOnlyOnce && approachTriggered)
        {
            if (playerDist > approachDistance) approachTriggered = false;
        }

        if (!enableKeyInteract) return;

        bool inRange = playerDist <= interactDistance;
        if (inRange && !keyInteractReady)
        {
            keyInteractReady = true;
            ShowPrompt(true);
        }
        else if (!inRange && keyInteractReady)
        {
            keyInteractReady = false;
            ShowPrompt(false);
        }

        if (keyInteractReady && Input.GetKeyDown(interactKey))
            ExecuteEffects(keyEffects);
    }

    private void ExecuteEffects(FrameEffectSet fx)
    {
        if (fx.zoom) ZoomImage();
        if (fx.showText && !string.IsNullOrEmpty(fx.text)) ShowTextPopup(fx.text, fx.textDuration);
        if (fx.playSound && fx.soundClip != null) PlaySound(fx.soundClip, fx.soundVolume);
        if (fx.changeBGM && fx.bgmClip != null) ChangeBGM(fx.bgmClip, fx.bgmVolume);
        if (fx.changeWeather) ChangeWeather(fx.weatherType, fx.weatherParticles, fx.weatherColor);
        if (fx.changeBackground) ChangeBackground(fx.backgroundColor);
        if (fx.changeBrightness) ChangeBrightness(fx.brightness);
        if (fx.loadScene && !string.IsNullOrEmpty(fx.sceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(fx.sceneName);
        if (fx.toggleObject && fx.targetObject != null) fx.targetObject.SetActive(fx.objectShow);
    }

    private void ZoomImage()
    {
        if (sr.sprite == null) return;
        GalleryPlayer.Freeze();

        var overlay = new GameObject("ZoomOverlay");
        var cam = Camera.main;
        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;

        var bgGO = new GameObject("ZoomBG");
        bgGO.transform.SetParent(overlay.transform);
        bgGO.transform.position = cam.transform.position + Vector3.forward * 5f;
        var bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite = RuntimeSprite.Get();
        bgSR.color = new Color(0, 0, 0, 0.85f);
        bgSR.sortingOrder = 900;
        bgGO.transform.localScale = new Vector3(camW + 2, camH + 2, 1);

        var imgGO = new GameObject("ZoomImage");
        imgGO.transform.SetParent(overlay.transform);
        imgGO.transform.position = cam.transform.position + Vector3.forward * 4.9f;
        var imgSR = imgGO.AddComponent<SpriteRenderer>();
        imgSR.sprite = sr.sprite;
        imgSR.sortingOrder = 901;

        float sprW = sr.sprite.bounds.size.x;
        float sprH = sr.sprite.bounds.size.y;
        float fitScale = Mathf.Min((camW * 0.85f) / sprW, (camH * 0.85f) / sprH);
        imgGO.transform.localScale = Vector3.one * fitScale;

        overlay.AddComponent<ZoomOverlayClose>();
    }

    private void ShowTextPopup(string text, float duration)
    {
        var cam = Camera.main;
        var go = new GameObject("TextPopup");
        go.transform.position = cam.transform.position + Vector3.forward * 4.8f + Vector3.down * (cam.orthographicSize * 0.6f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.characterSize = 0.08f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        go.GetComponent<MeshRenderer>().sortingOrder = 910;

        var tw = go.AddComponent<GalleryTypewriter>();
        tw.Play(text);

        Destroy(go, duration);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (audioSrc == null)
        {
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 0f;
        }
        audioSrc.PlayOneShot(clip, volume);
    }

    private void ChangeBGM(AudioClip clip, float volume)
    {
        var bgmObj = GameObject.Find("GalleryBGM");
        AudioSource bgmSrc;
        if (bgmObj == null)
        {
            bgmObj = new GameObject("GalleryBGM");
            DontDestroyOnLoad(bgmObj);
            bgmSrc = bgmObj.AddComponent<AudioSource>();
            bgmSrc.loop = true;
            bgmSrc.playOnAwake = false;
        }
        else
        {
            bgmSrc = bgmObj.GetComponent<AudioSource>();
        }
        bgmSrc.clip = clip;
        bgmSrc.volume = volume;
        bgmSrc.Play();
    }

    private void ChangeWeather(GalleryWeather.WeatherType type, int particles, Color color)
    {
        var weather = FindObjectOfType<GalleryWeather>();
        if (weather == null) return;
        weather.SetWeather(type, particles, color);
    }

    private void ChangeBackground(Color color)
    {
        var bg = FindObjectOfType<GalleryBackground>();
        if (bg == null) return;
        bg.SetDefaultColor(color);
    }

    private void ChangeBrightness(float value)
    {
        var mgr = GalleryManager.Instance;
        if (mgr != null && mgr.Settings != null)
            mgr.Settings.ambientBrightness = value;
        RenderSettings.ambientLight = Color.white * value;
    }

    private void ShowPrompt(bool show)
    {
        if (show && promptGO == null)
        {
            promptGO = new GameObject("InteractPrompt");
            promptGO.transform.SetParent(transform);
            promptGO.transform.localPosition = Vector3.up * 1.2f;
            Vector3 ps = transform.localScale;
            promptGO.transform.localScale = new Vector3(
                ps.x != 0 ? 1f / ps.x : 1f, ps.y != 0 ? 1f / ps.y : 1f, 1f);
            var tm = promptGO.AddComponent<TextMesh>();
            tm.text = $"[{interactKey}]";
            tm.characterSize = 0.06f;
            tm.fontSize = 80;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 1f, 0.7f, 0.9f);
            promptGO.GetComponent<MeshRenderer>().sortingOrder = sortingOrder + 2;
        }
        else if (!show && promptGO != null)
        {
            Destroy(promptGO);
            promptGO = null;
        }
    }

    private void CreateCaption()
    {
        var textGO = new GameObject("Caption");
        textGO.transform.SetParent(transform);
        float yOffset = -0.6f;
        if (sr.sprite != null)
            yOffset = -(sr.sprite.bounds.extents.y + 0.15f);
        textGO.transform.localPosition = new Vector3(0, yOffset, 0);
        Vector3 ps = transform.localScale;
        textGO.transform.localScale = new Vector3(
            ps.x != 0 ? 1f / ps.x : 1f, ps.y != 0 ? 1f / ps.y : 1f, 1f);

        captionTM = textGO.AddComponent<TextMesh>();
        captionTM.text = caption;
        captionTM.characterSize = captionSize;
        captionTM.fontSize = 60;
        captionTM.anchor = TextAnchor.UpperCenter;
        captionTM.alignment = TextAlignment.Center;
        captionTM.color = fadeInOnApproach
            ? new Color(captionColor.r, captionColor.g, captionColor.b, 0f)
            : captionColor;
        captionMR = textGO.GetComponent<MeshRenderer>();
        captionMR.sortingOrder = sortingOrder + 1;
    }

    public void SwapImage(Sprite newImage, string newCaption = null)
    {
        if (newImage != null) { image = newImage; sr.sprite = newImage; sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, currentAlpha); }
        if (!string.IsNullOrEmpty(newCaption)) { caption = newCaption; if (captionTM != null) captionTM.text = newCaption; }
    }

    private void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null && image != null) { sr.sprite = image; sr.color = Color.white; sr.sortingOrder = sortingOrder; }
    }

    private void OnDrawGizmosSelected()
    {
        if (fadeInOnApproach)
        {
            Gizmos.color = new Color(1f, 1f, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, fadeDistance);
        }
        if (enableKeyInteract)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, interactDistance);
        }
        if (enableApproachTrigger)
        {
            Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, approachDistance);
        }
    }
}

public class ZoomOverlayClose : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            GalleryPlayer.Unfreeze();
            Destroy(gameObject);
        }
    }
}

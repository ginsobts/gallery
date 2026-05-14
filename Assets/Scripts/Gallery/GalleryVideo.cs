using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class GalleryVideo : MonoBehaviour
{
    [Header("视频")]
    [Tooltip("视频文件（VideoClip）")]
    [SerializeField] private VideoClip videoClip;
    [Tooltip("视频文件路径（StreamingAssets 内，优先于 VideoClip）")]
    [SerializeField] private string videoPath = "";

    [Header("播放设置")]
    [Tooltip("出现后自动播放")]
    [SerializeField] private bool autoPlay = true;
    [Tooltip("循环播放")]
    [SerializeField] private bool loop = true;
    [Tooltip("交互键（非自动播放时使用）")]
    [SerializeField] private KeyCode playKey = KeyCode.E;
    [Tooltip("触发播放/暂停的距离")]
    [SerializeField] private float triggerRange = 3f;

    [Header("淡入触发")]
    [SerializeField] private bool fadeInOnApproach = false;
    [SerializeField] private float fadeDistance = 6f;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("声音")]
    [SerializeField] private bool enableAudio = true;
    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 0.5f;
    [SerializeField] private float audioRange = 5f;

    [Header("外观")]
    [SerializeField] private Sprite coverImage;

    [Header("── 按键交互效果 ──")]
    [SerializeField] private bool enableKeyEffects = false;
    [SerializeField] private KeyCode effectKey = KeyCode.F;
    [SerializeField] private float effectKeyDistance = 3f;
    [SerializeField] private FrameEffectSet keyEffects = new FrameEffectSet();

    [Header("── 靠近触发效果 ──")]
    [SerializeField] private bool enableApproachEffects = false;
    [SerializeField] private float approachEffectDistance = 4f;
    [SerializeField] private bool approachEffectOnlyOnce = true;
    [SerializeField] private FrameEffectSet approachEffects = new FrameEffectSet();

    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private SpriteRenderer sr;
    private RenderTexture renderTexture;
    private Sprite videoSprite;
    private Transform playerTransform;
    private bool isPlaying;
    private bool playerInPlayRange;
    private GameObject promptGO;
    private float currentAlpha;
    private bool hasAppeared;
    private bool autoPlayTriggered;
    private MaterialPropertyBlock mpb;

    private bool effectKeyReady;
    private GameObject effectPromptGO;
    private bool approachEffectTriggered;
    private AudioSource effectAudioSrc;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        if (coverImage != null)
            sr.sprite = coverImage;
        else if (sr.sprite == null)
        {
            sr.sprite = RuntimeSprite.Get();
            sr.color = new Color(0.15f, 0.15f, 0.2f);
        }

        if (fadeInOnApproach)
        {
            currentAlpha = 0f;
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
            hasAppeared = false;
        }
        else
        {
            currentAlpha = 1f;
            hasAppeared = true;
        }
    }

    private void Start()
    {
        var player = GalleryPlayer.Instance;
        if (player != null)
            playerTransform = player.transform;

        SetupVideoPlayer();

        if (!autoPlay)
            CreatePrompt();
    }

    private void SetupVideoPlayer()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = loop;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        if (!enableAudio)
            audioSource.mute = true;

        if (!string.IsNullOrEmpty(videoPath))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, videoPath);
        }
        else if (videoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
        }

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            if (GalleryPlayer.Instance != null)
                playerTransform = GalleryPlayer.Instance.transform;
            else
                return;
        }

        Vector2 diff = (Vector2)transform.position - (Vector2)playerTransform.position;
        float sqrDist = diff.sqrMagnitude;
        playerInPlayRange = sqrDist <= triggerRange * triggerRange;

        float dist = -1f;
        if (fadeInOnApproach && !hasAppeared && sqrDist <= fadeDistance * fadeDistance)
        {
            dist = Mathf.Sqrt(sqrDist);
            UpdateFadeIn(dist);
        }
        if (enableAudio && audioSource != null)
        {
            if (dist < 0f) dist = Mathf.Sqrt(sqrDist);
            UpdateAudioVolume(dist);
        }

        if (!hasAppeared) return;

        if (autoPlay)
        {
            if (!autoPlayTriggered && hasAppeared)
            {
                autoPlayTriggered = true;
                Play();
            }
            if (playerInPlayRange && !isPlaying && autoPlayTriggered)
                Play();
            else if (!playerInPlayRange && isPlaying)
                Pause();
        }
        else
        {
            if (promptGO != null)
                promptGO.SetActive(playerInPlayRange && !isPlaying);

            if (playerInPlayRange && Input.GetKeyDown(playKey))
            {
                if (isPlaying) Pause();
                else Play();
            }
        }

        if (isPlaying && videoPlayer.isPlaying)
            UpdateVideoFrame();

        UpdateEffects(sqrDist);
    }

    private void UpdateEffects(float sqrDist)
    {
        if (enableApproachEffects && !approachEffectTriggered)
        {
            if (sqrDist <= approachEffectDistance * approachEffectDistance)
            {
                ExecuteEffects(approachEffects);
                if (approachEffectOnlyOnce) approachEffectTriggered = true;
            }
        }
        if (enableApproachEffects && !approachEffectOnlyOnce && approachEffectTriggered)
        {
            if (sqrDist > approachEffectDistance * approachEffectDistance) approachEffectTriggered = false;
        }

        if (!enableKeyEffects) return;

        bool inRange = sqrDist <= effectKeyDistance * effectKeyDistance;
        if (inRange && !effectKeyReady)
        {
            effectKeyReady = true;
            ShowEffectPrompt(true);
        }
        else if (!inRange && effectKeyReady)
        {
            effectKeyReady = false;
            ShowEffectPrompt(false);
        }

        if (effectKeyReady && Input.GetKeyDown(effectKey))
            ExecuteEffects(keyEffects);
    }

    private void ExecuteEffects(FrameEffectSet fx)
    {
        if (fx.zoom) ZoomImage();
        if (fx.showText && !string.IsNullOrEmpty(fx.text)) ShowTextPopup(fx.text, fx.textDuration, fx.textEffect);
        if (fx.playSound && fx.soundClip != null) PlayEffectSound(fx.soundClip, fx.soundVolume);
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

    private void ShowTextPopup(string text, float duration, int textEffect = 0)
    {
        var cam = Camera.main;
        var go = new GameObject("TextPopup");
        go.transform.position = cam.transform.position + Vector3.forward * 4.8f + Vector3.down * (cam.orthographicSize * 0.6f);

        var tm = go.AddComponent<TextMesh>();
        tm.characterSize = 0.08f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        go.GetComponent<MeshRenderer>().sortingOrder = 910;

        var fx = go.AddComponent<GalleryTextEffect>();
        fx.Play(text, (GalleryTextEffect.TextEffectType)textEffect, duration);
    }

    private void PlayEffectSound(AudioClip clip, float volume)
    {
        if (effectAudioSrc == null)
        {
            effectAudioSrc = gameObject.AddComponent<AudioSource>();
            effectAudioSrc.playOnAwake = false;
            effectAudioSrc.spatialBlend = 0f;
        }
        effectAudioSrc.PlayOneShot(clip, volume);
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
        if (weather == null)
        {
            var go = new GameObject("SceneWeather");
            var col2d = go.AddComponent<BoxCollider2D>();
            col2d.isTrigger = true;
            col2d.size = new Vector2(200f, 100f);
            weather = go.AddComponent<GalleryWeather>();
        }
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

    private void ShowEffectPrompt(bool show)
    {
        if (show && effectPromptGO == null)
        {
            effectPromptGO = new GameObject("EffectPrompt");
            effectPromptGO.transform.SetParent(transform);
            effectPromptGO.transform.localPosition = Vector3.up * 1.2f;
            Vector3 ps = transform.localScale;
            effectPromptGO.transform.localScale = new Vector3(
                ps.x != 0 ? 1f / ps.x : 1f, ps.y != 0 ? 1f / ps.y : 1f, 1f);
            var tm = effectPromptGO.AddComponent<TextMesh>();
            tm.text = $"[{effectKey}]";
            tm.characterSize = 0.06f;
            tm.fontSize = 80;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 1f, 0.7f, 0.9f);
            effectPromptGO.GetComponent<MeshRenderer>().sortingOrder = 22;
        }
        else if (!show && effectPromptGO != null)
        {
            Destroy(effectPromptGO);
            effectPromptGO = null;
        }
    }

    private void UpdateFadeIn(float dist)
    {
        if (!fadeInOnApproach || hasAppeared) return;
        if (dist <= fadeDistance)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, fadeSpeed * Time.deltaTime);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, currentAlpha);
            if (currentAlpha >= 1f) hasAppeared = true;
        }
    }

    private void UpdateAudioVolume(float dist)
    {
        if (!enableAudio || audioSource == null) return;
        if (dist >= audioRange) audioSource.volume = 0f;
        else audioSource.volume = Mathf.Lerp(maxVolume, 0f, dist / audioRange);
    }

    private void Play()
    {
        if (!videoPlayer.isPrepared) { videoPlayer.Prepare(); return; }
        videoPlayer.Play();
        isPlaying = true;
    }

    private void Pause()
    {
        videoPlayer.Pause();
        isPlaying = false;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        int width = (int)vp.width;
        int height = (int)vp.height;
        if (width == 0 || height == 0) return;

        renderTexture = new RenderTexture(width, height, 0);
        vp.targetTexture = renderTexture;

        Vector3 oldScale = transform.localScale;
        float oldBoundsW = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
        float oldBoundsH = sr.sprite != null ? sr.sprite.bounds.size.y : 1f;
        float oldVisualW = Mathf.Abs(oldScale.x) * oldBoundsW;
        float oldVisualH = Mathf.Abs(oldScale.y) * oldBoundsH;

        videoSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 4, 4),
            Vector2.one * 0.5f, 100f);
        sr.sprite = videoSprite;

        float newBoundsW = videoSprite.bounds.size.x;
        float newBoundsH = videoSprite.bounds.size.y;

        if (fitAspectOnPrepare)
        {
            float oldVisualArea = oldVisualW * oldVisualH;
            float videoAspect = (float)width / height;
            float targetW = Mathf.Sqrt(oldVisualArea * videoAspect);
            float targetH = targetW / videoAspect;
            transform.localScale = new Vector3(
                targetW / newBoundsW,
                targetH / newBoundsH, 1f);
        }
        else
        {
            transform.localScale = new Vector3(
                oldVisualW / newBoundsW,
                oldVisualH / newBoundsH, 1f);
        }

        mpb = new MaterialPropertyBlock();
        vp.Play();
        isPlaying = true;
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (!loop)
        {
            isPlaying = false;
            if (coverImage != null) sr.sprite = coverImage;
        }
    }

    private void UpdateVideoFrame()
    {
        if (renderTexture == null || mpb == null) return;
        sr.GetPropertyBlock(mpb);
        mpb.SetTexture("_MainTex", renderTexture);
        sr.SetPropertyBlock(mpb);
        float a = fadeInOnApproach ? currentAlpha : 1f;
        sr.color = new Color(1f, 1f, 1f, a);
    }

    private void CreatePrompt()
    {
        promptGO = new GameObject("VideoPrompt");
        promptGO.transform.SetParent(transform);
        promptGO.transform.localPosition = new Vector3(0, -0.8f, 0);

        Vector3 ps = transform.localScale;
        float ix = ps.x != 0 ? 1f / ps.x : 1f;
        float iy = ps.y != 0 ? 1f / ps.y : 1f;

        var promptSR = promptGO.AddComponent<SpriteRenderer>();
        promptSR.sprite = RuntimeSprite.Get();
        promptSR.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        promptSR.sortingOrder = 20;
        promptGO.transform.localScale = new Vector3(0.8f * ix, 0.3f * iy, 1f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(promptGO.transform);
        textGO.transform.localPosition = Vector3.zero;
        var tm = textGO.AddComponent<TextMesh>();
        tm.text = $"{playKey} - Play";
        tm.characterSize = 0.12f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        textGO.GetComponent<MeshRenderer>().sortingOrder = 21;
        promptGO.SetActive(false);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoEnd;
        }
        if (renderTexture != null) renderTexture.Release();
        if (videoSprite != null) Destroy(videoSprite);
    }

    // ── Runtime setters ──

    public string ElementId { get; set; }

    public void SetVideoUrl(string url)
    {
        if (videoPlayer == null) return;
        videoPlayer.source = UnityEngine.Video.VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.Stop();
        isPlaying = false;
        autoPlayTriggered = false;
    }

    public void SetCoverImage(Sprite cover) { coverImage = cover; if (!isPlaying && sr != null) sr.sprite = cover; }
    public void SetAutoPlay(bool val) { autoPlay = val; }
    public void SetLoop(bool val) { loop = val; if (videoPlayer != null) videoPlayer.isLooping = val; }
    public void SetAudio(bool enable, float volume, float range) { enableAudio = enable; maxVolume = volume; audioRange = range; }
    public void SetFadeIn(bool enable, float distance, float speed) { fadeInOnApproach = enable; fadeDistance = distance; fadeSpeed = speed; }
    public void SetTriggerRange(float range) { triggerRange = range; }

    private bool fitAspectOnPrepare = false;
    public void SetFitAspectOnPrepare(bool fit) { fitAspectOnPrepare = fit; }

    public void SetKeyEffects(bool enable, KeyCode key, float distance, FrameEffectSet effects)
    {
        enableKeyEffects = enable; effectKey = key; effectKeyDistance = distance; keyEffects = effects ?? new FrameEffectSet();
    }
    public void SetApproachEffects(bool enable, float distance, bool onlyOnce, FrameEffectSet effects)
    {
        enableApproachEffects = enable; approachEffectDistance = distance; approachEffectOnlyOnce = onlyOnce; approachEffects = effects ?? new FrameEffectSet();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, triggerRange);

        if (fadeInOnApproach)
        {
            Gizmos.color = new Color(1f, 1f, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, fadeDistance);
        }
        if (enableAudio)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, audioRange);
        }
        if (enableKeyEffects)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, effectKeyDistance);
        }
        if (enableApproachEffects)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.3f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, approachEffectDistance);
        }
    }
}

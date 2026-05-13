using UnityEngine;

public class GalleryNPCDialogue : MonoBehaviour
{
    [System.Serializable]
    public struct DialogueLine
    {
        [TextArea(1, 3)]
        public string text;
        [Tooltip("该句停留时间")]
        public float duration;
    }

    [Header("对话内容")]
    [SerializeField] private DialogueLine[] lines;
    [Tooltip("对话结束后是否循环")]
    [SerializeField] private bool loop = false;
    [Tooltip("触发方式：自动（靠近）/ 按键")]
    [SerializeField] private bool autoTrigger = true;
    [Tooltip("触发距离")]
    [SerializeField] private float triggerDistance = 2f;
    [Tooltip("按键触发键")]
    [SerializeField] private KeyCode dialogueKey = KeyCode.E;

    [Header("气泡样式")]
    [SerializeField] private Color bubbleColor = new Color(0.95f, 0.95f, 0.95f, 0.9f);
    [SerializeField] private Color textColor = new Color(0.1f, 0.1f, 0.1f);
    [SerializeField] private Vector2 bubbleOffset = new Vector2(0, 0.8f);
    [SerializeField] private float textSize = 0.06f;
    [SerializeField] private float typeSpeed = 15f;

    [Header("── 按键交互效果 ──")]
    [SerializeField] private bool enableKeyEffects = false;
    [SerializeField] private KeyCode effectKey = KeyCode.F;
    [SerializeField] private float effectKeyDistance = 2.5f;
    [SerializeField] private FrameEffectSet keyEffects = new FrameEffectSet();

    [Header("── 靠近触发效果 ──")]
    [SerializeField] private bool enableApproachEffects = false;
    [SerializeField] private float approachEffectDistance = 3f;
    [SerializeField] private bool approachEffectOnlyOnce = true;
    [SerializeField] private FrameEffectSet approachEffects = new FrameEffectSet();

    private Transform playerTransform;
    private GameObject bubbleGO;
    private SpriteRenderer bubbleSR;
    private TextMesh textMesh;
    private GalleryTypewriter typewriter;

    private int currentLine = -1;
    private float lineTimer;
    private bool dialogueActive;
    private bool hasFinished;
    private bool playerInRange;

    private bool effectKeyReady;
    private GameObject effectPromptGO;
    private bool approachEffectTriggered;
    private AudioSource effectAudioSrc;

    private void Start()
    {
        if (GalleryPlayer.Instance != null)
            playerTransform = GalleryPlayer.Instance.transform;

        CreateBubble();
        bubbleGO.SetActive(false);
    }

    private void CreateBubble()
    {
        bubbleGO = new GameObject("DialogueBubble");
        bubbleGO.transform.SetParent(transform);
        bubbleGO.transform.localPosition = new Vector3(bubbleOffset.x, bubbleOffset.y, 0);

        Vector3 ps = transform.localScale;
        float ix = ps.x != 0 ? 1f / ps.x : 1f;
        float iy = ps.y != 0 ? 1f / ps.y : 1f;
        bubbleGO.transform.localScale = new Vector3(ix * 1.8f, iy * 0.6f, 1f);

        bubbleSR = bubbleGO.AddComponent<SpriteRenderer>();
        bubbleSR.sprite = RuntimeSprite.Get();
        bubbleSR.color = bubbleColor;
        bubbleSR.sortingOrder = 50;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(bubbleGO.transform);
        textGO.transform.localPosition = Vector3.zero;
        textGO.transform.localScale = new Vector3(1f / 1.8f, 1f / 0.6f, 1f);

        textMesh = textGO.AddComponent<TextMesh>();
        textMesh.characterSize = textSize;
        textMesh.fontSize = 80;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = textColor;
        textGO.GetComponent<MeshRenderer>().sortingOrder = 51;

        if (typeSpeed > 0)
            typewriter = textGO.AddComponent<GalleryTypewriter>();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            if (GalleryPlayer.Instance != null)
                playerTransform = GalleryPlayer.Instance.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        playerInRange = dist <= triggerDistance;

        if (!dialogueActive)
        {
            if (hasFinished && !loop) { }
            else if (autoTrigger && playerInRange)
                StartDialogue();
            else if (!autoTrigger && playerInRange && Input.GetKeyDown(dialogueKey))
                StartDialogue();
        }
        else
        {
            UpdateDialogue();
        }

        if (dialogueActive && !playerInRange && autoTrigger)
            EndDialogue();

        UpdateEffects(dist);
    }

    private void UpdateEffects(float dist)
    {
        if (enableApproachEffects && !approachEffectTriggered)
        {
            if (dist <= approachEffectDistance)
            {
                ExecuteEffects(approachEffects);
                if (approachEffectOnlyOnce) approachEffectTriggered = true;
            }
        }
        if (enableApproachEffects && !approachEffectOnlyOnce && approachEffectTriggered)
        {
            if (dist > approachEffectDistance) approachEffectTriggered = false;
        }

        if (!enableKeyEffects) return;

        bool inRange = dist <= effectKeyDistance;
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
        if (fx.zoom) ZoomNPC();
        if (fx.showText && !string.IsNullOrEmpty(fx.text)) ShowTextPopup(fx.text, fx.textDuration);
        if (fx.playSound && fx.soundClip != null) PlayEffectSound(fx.soundClip, fx.soundVolume);
        if (fx.changeBGM && fx.bgmClip != null) ChangeBGM(fx.bgmClip, fx.bgmVolume);
        if (fx.changeWeather) ChangeWeather(fx.weatherType, fx.weatherParticles, fx.weatherColor);
        if (fx.changeBackground) ChangeBackground(fx.backgroundColor);
        if (fx.changeBrightness) ChangeBrightness(fx.brightness);
        if (fx.loadScene && !string.IsNullOrEmpty(fx.sceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(fx.sceneName);
        if (fx.toggleObject && fx.targetObject != null) fx.targetObject.SetActive(fx.objectShow);
    }

    private void ZoomNPC()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
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

    private void PlayEffectSound(AudioClip clip, float volume)
    {
        if (effectAudioSrc == null)
        {
            effectAudioSrc = GetComponent<AudioSource>();
            if (effectAudioSrc == null) effectAudioSrc = gameObject.AddComponent<AudioSource>();
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

    private void ShowEffectPrompt(bool show)
    {
        if (show && effectPromptGO == null)
        {
            effectPromptGO = new GameObject("EffectPrompt");
            effectPromptGO.transform.SetParent(transform);
            effectPromptGO.transform.localPosition = Vector3.up * 1.5f;
            Vector3 ps = transform.localScale;
            effectPromptGO.transform.localScale = new Vector3(
                ps.x != 0 ? 1f / ps.x : 1f, ps.y != 0 ? 1f / ps.y : 1f, 1f);
            var tm = effectPromptGO.AddComponent<TextMesh>();
            tm.text = $"[{effectKey}]";
            tm.characterSize = 0.06f;
            tm.fontSize = 80;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 1f, 0.7f, 0.9f);
            effectPromptGO.GetComponent<MeshRenderer>().sortingOrder = 52;
        }
        else if (!show && effectPromptGO != null)
        {
            Destroy(effectPromptGO);
            effectPromptGO = null;
        }
    }

    private void StartDialogue()
    {
        if (lines == null || lines.Length == 0) return;
        dialogueActive = true;
        currentLine = -1;
        NextLine();
        bubbleGO.SetActive(true);
    }

    private void NextLine()
    {
        currentLine++;
        if (currentLine >= lines.Length)
        {
            if (loop)
                currentLine = 0;
            else
            {
                EndDialogue();
                hasFinished = true;
                return;
            }
        }

        var line = lines[currentLine];
        lineTimer = 0f;

        if (typewriter != null && typeSpeed > 0)
            typewriter.Play(line.text);
        else
            textMesh.text = line.text;
    }

    private void UpdateDialogue()
    {
        if (currentLine < 0 || currentLine >= lines.Length) return;

        var line = lines[currentLine];
        bool textDone = typewriter == null || typewriter.IsDone;

        if (textDone)
        {
            lineTimer += Time.deltaTime;
            float dur = line.duration > 0 ? line.duration : 2f;
            if (lineTimer >= dur)
                NextLine();
        }

        if (Input.GetKeyDown(dialogueKey) || Input.GetMouseButtonDown(0))
        {
            if (typewriter != null && !typewriter.IsDone)
                typewriter.Skip();
            else
                NextLine();
        }
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        bubbleGO.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, triggerDistance);

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

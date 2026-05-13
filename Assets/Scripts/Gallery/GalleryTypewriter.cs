using UnityEngine;
using UnityEngine.UI;

public class GalleryTypewriter : MonoBehaviour
{
    [Header("打字机设置")]
    [Tooltip("每秒显示的字符数")]
    [SerializeField] private float charsPerSecond = 20f;
    [Tooltip("标点符号停顿（额外等待秒数）")]
    [SerializeField] private float punctuationPause = 0.15f;
    [Tooltip("打字音效")]
    [SerializeField] private AudioClip typeSound;
    [Tooltip("每几个字符播一次音效")]
    [SerializeField] private int soundInterval = 2;

    private string fullText;
    private int charIndex;
    private float charTimer;
    private bool isDone = true;

    private TextMesh textMesh;
    private Text uiText;
    private AudioSource audioSrc;

    private System.Action onComplete;

    private void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        uiText = GetComponent<Text>();

        if (typeSound != null)
        {
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null)
                audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.clip = typeSound;
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 0f;
            audioSrc.volume = 0.3f;
        }
    }

    public void Play(string text, System.Action callback = null)
    {
        fullText = text ?? "";
        charIndex = 0;
        charTimer = 0f;
        isDone = false;
        onComplete = callback;
        SetText("");
    }

    public void Skip()
    {
        if (isDone) return;
        charIndex = fullText.Length;
        SetText(fullText);
        isDone = true;
        onComplete?.Invoke();
    }

    public bool IsDone => isDone;

    private void Update()
    {
        if (isDone || fullText == null) return;

        charTimer += Time.unscaledDeltaTime;
        float interval = 1f / charsPerSecond;

        int prevIndex = charIndex;
        while (charTimer >= interval && charIndex < fullText.Length)
        {
            charTimer -= interval;
            charIndex++;

            if (charIndex < fullText.Length)
            {
                char c = fullText[charIndex - 1];
                if (c == '。' || c == '，' || c == '.' || c == ',' ||
                    c == '！' || c == '？' || c == '!' || c == '?' ||
                    c == '；' || c == ';' || c == '：' || c == ':')
                {
                    charTimer -= punctuationPause;
                }
            }

            if (audioSrc != null && charIndex % soundInterval == 0)
                audioSrc.PlayOneShot(typeSound, 0.3f);
        }

        if (charIndex != prevIndex)
            SetText(fullText.Substring(0, charIndex));

        if (charIndex >= fullText.Length && !isDone)
        {
            isDone = true;
            onComplete?.Invoke();
        }
    }

    private void SetText(string t)
    {
        if (textMesh != null) textMesh.text = t;
        if (uiText != null) uiText.text = t;
    }
}

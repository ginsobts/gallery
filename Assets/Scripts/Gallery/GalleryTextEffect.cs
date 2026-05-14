using UnityEngine;

public class GalleryTextEffect : MonoBehaviour
{
    public enum TextEffectType
    {
        Typewriter = 0,
        FadeIn = 1,
        SlideUp = 2,
        SlideDown = 3,
        ScaleIn = 4,
        Flicker = 5,
        WaveIn = 6,
        GlitchIn = 7,
    }

    private TextMesh textMesh;
    private MeshRenderer meshRenderer;
    private string fullText;
    private float duration;
    private float elapsed;
    private TextEffectType effectType;
    private bool isPlaying;

    private float charsPerSecond = 20f;
    private int charIndex;
    private float charTimer;

    private Vector3 startPos;
    private Vector3 targetPos;
    private Vector3 startScale;
    private float fadeOutStart;
    private bool autoDestroy = true;
    private char[] glitchBuffer;

    public bool IsDone => !isPlaying;

    public void Play(string text, TextEffectType effect, float totalDuration, bool destroyOnComplete = true)
    {
        textMesh = GetComponent<TextMesh>();
        meshRenderer = GetComponent<MeshRenderer>();
        fullText = text ?? "";
        duration = totalDuration;
        effectType = effect;
        elapsed = 0f;
        charIndex = 0;
        charTimer = 0f;
        isPlaying = true;
        autoDestroy = destroyOnComplete;
        fadeOutStart = Mathf.Max(0, duration - 1f);
        targetPos = transform.position;
        startScale = transform.localScale;

        switch (effectType)
        {
            case TextEffectType.Typewriter:
                textMesh.text = "";
                break;
            case TextEffectType.FadeIn:
                textMesh.text = fullText;
                SetAlpha(0f);
                break;
            case TextEffectType.SlideUp:
                textMesh.text = fullText;
                startPos = targetPos + (autoDestroy ? Vector3.down * 0.5f : Vector3.down * 0.1f);
                transform.position = startPos;
                SetAlpha(0f);
                break;
            case TextEffectType.SlideDown:
                textMesh.text = fullText;
                startPos = targetPos + (autoDestroy ? Vector3.up * 0.5f : Vector3.up * 0.1f);
                transform.position = startPos;
                SetAlpha(0f);
                break;
            case TextEffectType.ScaleIn:
                textMesh.text = fullText;
                startScale = transform.localScale;
                transform.localScale = Vector3.zero;
                break;
            case TextEffectType.Flicker:
                textMesh.text = fullText;
                SetAlpha(0f);
                break;
            case TextEffectType.WaveIn:
                textMesh.text = "";
                break;
            case TextEffectType.GlitchIn:
                textMesh.text = "";
                break;
        }
    }

    private void Update()
    {
        if (!isPlaying) return;
        elapsed += Time.unscaledDeltaTime;

        switch (effectType)
        {
            case TextEffectType.Typewriter:
                UpdateTypewriter();
                break;
            case TextEffectType.FadeIn:
                UpdateFadeIn();
                break;
            case TextEffectType.SlideUp:
            case TextEffectType.SlideDown:
                UpdateSlide();
                break;
            case TextEffectType.ScaleIn:
                UpdateScaleIn();
                break;
            case TextEffectType.Flicker:
                UpdateFlicker();
                break;
            case TextEffectType.WaveIn:
                UpdateWaveIn();
                break;
            case TextEffectType.GlitchIn:
                UpdateGlitchIn();
                break;
        }

        if (autoDestroy && elapsed >= fadeOutStart)
        {
            float fadeT = Mathf.Clamp01((elapsed - fadeOutStart) / 1f);
            SetAlpha(Mathf.Lerp(GetCurrentBaseAlpha(), 0f, fadeT));
        }

        if (elapsed >= duration)
        {
            isPlaying = false;
            if (autoDestroy)
                Destroy(gameObject);
        }
    }

    private float GetCurrentBaseAlpha()
    {
        switch (effectType)
        {
            case TextEffectType.Flicker:
                return textMesh.color.a;
            default:
                return 1f;
        }
    }

    private void UpdateTypewriter()
    {
        charTimer += Time.unscaledDeltaTime;
        float interval = 1f / charsPerSecond;
        while (charTimer >= interval && charIndex < fullText.Length)
        {
            charTimer -= interval;
            charIndex++;
            char c = fullText[charIndex - 1];
            if (c == '。' || c == '，' || c == '.' || c == ',' ||
                c == '！' || c == '？' || c == '!' || c == '?')
                charTimer -= 0.15f;
        }
        textMesh.text = fullText.Substring(0, charIndex);
    }

    private void UpdateFadeIn()
    {
        float t = Mathf.Clamp01(elapsed / 0.8f);
        if (elapsed < fadeOutStart)
            SetAlpha(t);
    }

    private void UpdateSlide()
    {
        float t = Mathf.Clamp01(elapsed / 0.6f);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        transform.position = Vector3.Lerp(startPos, targetPos, eased);
        if (elapsed < fadeOutStart)
            SetAlpha(eased);
    }

    private void UpdateScaleIn()
    {
        float t = Mathf.Clamp01(elapsed / 0.5f);
        float eased = 1f - Mathf.Pow(1f - t, 2f);
        float bounce = eased > 0.9f ? 1f : eased * 1.05f;
        transform.localScale = startScale * bounce;
    }

    private void UpdateFlicker()
    {
        if (elapsed < 0.8f)
        {
            float flickerRate = Mathf.Lerp(30f, 5f, elapsed / 0.8f);
            float a = Mathf.Sin(elapsed * flickerRate * Mathf.PI) > 0 ? 1f : 0f;
            SetAlpha(a);
        }
        else if (elapsed < fadeOutStart)
        {
            SetAlpha(1f);
        }
    }

    private void UpdateWaveIn()
    {
        float charsRevealed = elapsed * charsPerSecond * 1.5f;
        int showCount = Mathf.Min(Mathf.FloorToInt(charsRevealed), fullText.Length);
        if (showCount != charIndex)
        {
            charIndex = showCount;
            textMesh.text = fullText.Substring(0, charIndex);
        }
        float waveOffset = Mathf.Sin(elapsed * 4f) * (autoDestroy ? 0.05f : 0.02f);
        transform.position = targetPos + Vector3.up * waveOffset;
    }

    private void UpdateGlitchIn()
    {
        if (elapsed < 0.6f)
        {
            float progress = elapsed / 0.6f;
            int revealCount = Mathf.FloorToInt(progress * fullText.Length);
            if (glitchBuffer == null || glitchBuffer.Length != fullText.Length)
                glitchBuffer = new char[fullText.Length];
            for (int i = 0; i < fullText.Length; i++)
            {
                if (i < revealCount)
                    glitchBuffer[i] = fullText[i];
                else if (i < revealCount + 3)
                    glitchBuffer[i] = (char)Random.Range(0x4E00, 0x9FFF);
                else
                    glitchBuffer[i] = ' ';
            }
            textMesh.text = new string(glitchBuffer);
        }
        else
        {
            textMesh.text = fullText;
        }
    }

    private void SetAlpha(float a)
    {
        if (textMesh != null)
        {
            Color c = textMesh.color;
            c.a = a;
            textMesh.color = c;
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GalleryBGMZone : MonoBehaviour
{
    [Header("音乐")]
    [Tooltip("进入该区域后播放的 BGM")]
    [SerializeField] private AudioClip bgmClip;
    [Tooltip("BGM 音量")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.4f;
    [Tooltip("淡入淡出时间（秒）")]
    [SerializeField] private float fadeTime = 1.5f;

    [Header("区域")]
    [Tooltip("优先级（多个区域重叠时高优先级生效）")]
    [SerializeField] private int priority = 0;

    private static GalleryBGMZone activeZone;
    private static AudioSource bgmSourceA;
    private static AudioSource bgmSourceB;
    private static bool usingA = true;
    private static float fadeTimer;
    private static float fadeDuration;
    private static bool isFading;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<GalleryPlayer>() == null) return;
        if (activeZone != null && activeZone.priority > priority) return;

        if (activeZone == this) return;
        activeZone = this;
        CrossfadeTo(bgmClip, volume, fadeTime);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<GalleryPlayer>() == null) return;
        if (activeZone != this) return;

        activeZone = null;
        FadeOut(fadeTime);
    }

    private static void EnsureSources()
    {
        if (bgmSourceA != null) return;

        var go = new GameObject("GalleryBGM");
        DontDestroyOnLoad(go);
        bgmSourceA = go.AddComponent<AudioSource>();
        bgmSourceA.loop = true;
        bgmSourceA.playOnAwake = false;
        bgmSourceA.spatialBlend = 0f;

        bgmSourceB = go.AddComponent<AudioSource>();
        bgmSourceB.loop = true;
        bgmSourceB.playOnAwake = false;
        bgmSourceB.spatialBlend = 0f;
    }

    private static void CrossfadeTo(AudioClip clip, float targetVol, float duration)
    {
        EnsureSources();
        if (clip == null) return;

        var incoming = usingA ? bgmSourceB : bgmSourceA;
        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();

        fadeDuration = duration;
        fadeTimer = 0f;
        isFading = true;
        usingA = !usingA;

        var updater = bgmSourceA.GetComponent<BGMFadeUpdater>();
        if (updater == null)
            bgmSourceA.gameObject.AddComponent<BGMFadeUpdater>().Init(targetVol);
        else
            updater.Init(targetVol);
    }

    private static void FadeOut(float duration)
    {
        EnsureSources();
        fadeDuration = duration;
        fadeTimer = 0f;
        isFading = true;

        var updater = bgmSourceA.GetComponent<BGMFadeUpdater>();
        if (updater == null)
            bgmSourceA.gameObject.AddComponent<BGMFadeUpdater>().Init(0f);
        else
            updater.Init(0f);
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 0.5f, 0.15f);
        Gizmos.DrawCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));
        Gizmos.color = new Color(0.2f, 0.8f, 0.5f, 0.5f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));
    }

    private class BGMFadeUpdater : MonoBehaviour
    {
        private float targetVol;
        private float startVolA, startVolB;
        private float timer, duration;
        private bool active;

        public void Init(float target)
        {
            targetVol = target;
            startVolA = bgmSourceA.volume;
            startVolB = bgmSourceB.volume;
            timer = 0f;
            duration = fadeDuration;
            active = true;
        }

        private void Update()
        {
            if (!active) return;
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            var current = usingA ? bgmSourceA : bgmSourceB;
            var outgoing = usingA ? bgmSourceB : bgmSourceA;

            current.volume = Mathf.Lerp(0f, targetVol, t);
            outgoing.volume = Mathf.Lerp(usingA ? startVolB : startVolA, 0f, t);

            if (t >= 1f)
            {
                active = false;
                outgoing.Stop();
                outgoing.volume = 0f;
                isFading = false;
            }
        }
    }
}

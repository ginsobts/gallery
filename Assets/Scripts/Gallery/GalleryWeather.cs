using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GalleryWeather : MonoBehaviour
{
    public enum WeatherType
    {
        Rain,
        Snow,
        Fog,
        Sunbeam,
        Fireflies
    }

    [Header("天气类型")]
    [SerializeField] private WeatherType weatherType = WeatherType.Rain;

    [Header("粒子参数")]
    [SerializeField] private int particleCount = 60;
    [SerializeField] private Color particleColor = Color.white;
    [SerializeField] private float intensity = 1f;

    [Header("环境音")]
    [Tooltip("该天气对应的环境音（留空则无）")]
    [SerializeField] private AudioClip ambientClip;
    [Range(0f, 1f)]
    [SerializeField] private float ambientVolume = 0.3f;
    [Tooltip("声音淡入距离")]
    [SerializeField] private float audioFadeRange = 8f;

    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 scale;
        public float rotation;
        public float life;
        public float maxLife;
        public float size;
        public Color color;
    }

    private Particle[] particles;
    private BoxCollider2D areaCol;
    private Transform playerTransform;
    private AudioSource audioSource;
    private Vector3 areaCenter;
    private Vector3 areaHalf;

    private bool isVisible;
    private const float CULL_DISTANCE_SQR = 25f * 25f;
    private Camera mainCam;

    private SpriteRenderer[] particleSRs;
    private GameObject[] particleGOs;
    private Transform[] particleTransforms;

    private float audioUpdateTimer;
    private const float AUDIO_UPDATE_INTERVAL = 0.1f;

    private float cullMaxExtentSqr;

    private void Awake()
    {
        areaCol = GetComponent<BoxCollider2D>();
        areaCol.isTrigger = true;
    }

    private void Start()
    {
        playerTransform = GalleryPlayer.Instance != null ? GalleryPlayer.Instance.transform : null;
        mainCam = Camera.main;

        areaCenter = transform.position + (Vector3)areaCol.offset;
        areaHalf = Vector3.Scale(areaCol.size, transform.localScale) * 0.5f;

        float maxExtent = Mathf.Max(areaHalf.x, areaHalf.y);
        float cullDist = 25f + maxExtent;
        cullMaxExtentSqr = cullDist * cullDist;

        CreateParticles();

        if (ambientClip != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = ambientClip;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0f;
            audioSource.playOnAwake = false;
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (mainCam == null) { mainCam = Camera.main; if (mainCam == null) return; }

        float dx = mainCam.transform.position.x - areaCenter.x;
        float dy = mainCam.transform.position.y - areaCenter.y;
        float distSqr = dx * dx + dy * dy;
        isVisible = distSqr < cullMaxExtentSqr;

        audioUpdateTimer -= Time.deltaTime;
        if (audioUpdateTimer <= 0f)
        {
            audioUpdateTimer = AUDIO_UPDATE_INTERVAL;
            UpdateAudio();
        }

        if (isVisible) UpdateParticles();
    }

    private void UpdateAudio()
    {
        if (audioSource == null || playerTransform == null) return;

        float dist = DistToAreaSqr(playerTransform.position);
        float fadeRangeSqr = audioFadeRange * audioFadeRange;
        float targetVol = dist <= 0f ? ambientVolume : Mathf.Lerp(ambientVolume, 0f, dist / fadeRangeSqr);
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVol, AUDIO_UPDATE_INTERVAL * 2f);
    }

    private float DistToAreaSqr(Vector3 pos)
    {
        float dx = Mathf.Max(0, Mathf.Abs(pos.x - areaCenter.x) - areaHalf.x);
        float dy = Mathf.Max(0, Mathf.Abs(pos.y - areaCenter.y) - areaHalf.y);
        return dx * dx + dy * dy;
    }

    private void CreateParticles()
    {
        particles = new Particle[particleCount];

        var sprite = weatherType == WeatherType.Fog
            ? RuntimeSprite.GetCircle(16)
            : RuntimeSprite.GetCircle(8);

        particleGOs = new GameObject[particleCount];
        particleSRs = new SpriteRenderer[particleCount];
        particleTransforms = new Transform[particleCount];

        var containerGO = new GameObject("Particles");
        containerGO.transform.SetParent(transform, false);
        containerGO.transform.localPosition = Vector3.zero;

        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject("P");
            go.transform.SetParent(containerGO.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 3;

            particleGOs[i] = go;
            particleSRs[i] = sr;
            particleTransforms[i] = go.transform;

            particles[i] = new Particle
            {
                life = Random.Range(0f, 3f),
                maxLife = GetMaxLife(),
                size = GetParticleSize()
            };

            ResetParticle(ref particles[i], true);
            particleTransforms[i].position = particles[i].position;
            particleTransforms[i].localScale = particles[i].scale;
            if (particles[i].rotation != 0)
                particleTransforms[i].rotation = Quaternion.Euler(0, 0, particles[i].rotation);
        }
    }

    private void UpdateParticles()
    {
        float dt = Time.deltaTime * intensity;
        int count = particles.Length;
        int halfCount = count / 2;
        bool evenFrame = (Time.frameCount & 1) == 0;

        int start = evenFrame ? 0 : halfCount;
        int end = evenFrame ? halfCount : count;

        for (int i = start; i < end; i++)
        {
            var p = particles[i];

            p.life += dt * 2f;
            float t = p.life / p.maxLife;

            if (t >= 1f)
            {
                ResetParticle(ref p, false);
                particles[i] = p;
                if (particleTransforms[i] != null)
                {
                    particleTransforms[i].position = p.position;
                    particleTransforms[i].localScale = p.scale;
                    if (p.rotation != 0)
                        particleTransforms[i].rotation = Quaternion.Euler(0, 0, p.rotation);
                }
                continue;
            }

            p.position += p.velocity * dt * 2f;
            ApplyParticleColor(ref p, t);
            particles[i] = p;

            if (particleTransforms[i] != null)
            {
                particleTransforms[i].position = p.position;
                particleSRs[i].color = p.color;
            }
        }
    }

    private void ResetParticle(ref Particle p, bool randomPhase)
    {
        p.life = randomPhase ? Random.Range(0f, p.maxLife) : 0f;
        p.maxLife = GetMaxLife();
        p.size = GetParticleSize();

        Vector3 pos = new Vector3(
            Random.Range(areaCenter.x - areaHalf.x, areaCenter.x + areaHalf.x),
            0, 0);

        switch (weatherType)
        {
            case WeatherType.Rain:
                pos.y = areaCenter.y + areaHalf.y + Random.Range(0f, 2f);
                p.velocity = new Vector3(Random.Range(-0.3f, 0.3f), -Random.Range(6f, 10f), 0);
                p.scale = new Vector3(0.02f, p.size * 3f, 1f);
                break;

            case WeatherType.Snow:
                pos.y = areaCenter.y + areaHalf.y + Random.Range(0f, 2f);
                p.velocity = new Vector3(Random.Range(-0.5f, 0.5f), -Random.Range(0.5f, 1.5f), 0);
                p.scale = Vector3.one * p.size;
                break;

            case WeatherType.Fog:
                pos.y = Random.Range(areaCenter.y - areaHalf.y, areaCenter.y + areaHalf.y);
                p.velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.05f, 0.05f), 0);
                p.scale = Vector3.one * p.size;
                break;

            case WeatherType.Sunbeam:
                pos.y = areaCenter.y + areaHalf.y;
                float angle = Random.Range(-15f, 15f) * Mathf.Deg2Rad;
                p.velocity = new Vector3(Mathf.Sin(angle) * 0.1f, -Random.Range(0.3f, 0.8f), 0);
                p.scale = new Vector3(p.size * 0.3f, p.size * 8f, 1f);
                p.rotation = -angle * Mathf.Rad2Deg;
                break;

            case WeatherType.Fireflies:
                pos.y = Random.Range(areaCenter.y - areaHalf.y, areaCenter.y + areaHalf.y);
                p.velocity = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-0.2f, 0.2f), 0);
                p.scale = Vector3.one * p.size;
                break;
        }

        p.position = pos;
    }

    private void ApplyParticleColor(ref Particle p, float t)
    {
        float alpha;
        Color c = particleColor;

        switch (weatherType)
        {
            case WeatherType.Rain:
                alpha = 0.5f * intensity;
                p.color = new Color(c.r, c.g, c.b, alpha);
                break;

            case WeatherType.Snow:
                alpha = Mathf.Sin(t * Mathf.PI) * 0.8f;
                p.color = new Color(c.r, c.g, c.b, alpha);
                break;

            case WeatherType.Fog:
                alpha = Mathf.Sin(t * Mathf.PI) * 0.15f;
                p.color = new Color(c.r, c.g, c.b, alpha);
                break;

            case WeatherType.Sunbeam:
                alpha = Mathf.Sin(t * Mathf.PI) * 0.12f;
                p.color = new Color(c.r, c.g, c.b, alpha);
                break;

            case WeatherType.Fireflies:
                float flicker = (Mathf.Sin(Time.time * 5f + p.life * 10f) + 1f) * 0.5f;
                alpha = flicker * 0.8f;
                p.color = new Color(c.r, c.g, c.b, alpha);
                p.velocity += new Vector3(
                    Mathf.Sin(Time.time * 2f + p.life) * 0.01f,
                    Mathf.Cos(Time.time * 1.5f + p.life) * 0.01f, 0);
                break;
        }
    }

    private float GetMaxLife()
    {
        switch (weatherType)
        {
            case WeatherType.Rain: return Random.Range(0.5f, 1.2f);
            case WeatherType.Snow: return Random.Range(3f, 6f);
            case WeatherType.Fog: return Random.Range(5f, 10f);
            case WeatherType.Sunbeam: return Random.Range(3f, 6f);
            case WeatherType.Fireflies: return Random.Range(2f, 5f);
            default: return 3f;
        }
    }

    private float GetParticleSize()
    {
        switch (weatherType)
        {
            case WeatherType.Rain: return Random.Range(0.08f, 0.15f);
            case WeatherType.Snow: return Random.Range(0.04f, 0.1f);
            case WeatherType.Fog: return Random.Range(1f, 3f);
            case WeatherType.Sunbeam: return Random.Range(0.5f, 1.5f);
            case WeatherType.Fireflies: return Random.Range(0.05f, 0.12f);
            default: return 0.1f;
        }
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Color gc;
        switch (weatherType)
        {
            case WeatherType.Rain: gc = new Color(0.3f, 0.5f, 0.9f, 0.15f); break;
            case WeatherType.Snow: gc = new Color(0.8f, 0.9f, 1f, 0.15f); break;
            case WeatherType.Fog: gc = new Color(0.6f, 0.6f, 0.6f, 0.15f); break;
            case WeatherType.Sunbeam: gc = new Color(1f, 0.9f, 0.5f, 0.15f); break;
            default: gc = new Color(0.5f, 0.5f, 0.5f, 0.15f); break;
        }
        Gizmos.color = gc;
        Gizmos.DrawCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, weatherType.ToString());
#endif
    }

    public void SetWeather(WeatherType type, int count, Color color)
    {
        weatherType = type;
        particleCount = count;
        particleColor = color;
        if (particleGOs != null)
        {
            for (int i = 0; i < particleGOs.Length; i++)
                if (particleGOs[i] != null) Destroy(particleGOs[i]);
        }
        var container = transform.Find("Particles");
        if (container != null) Destroy(container.gameObject);
        CreateParticles();
    }
}

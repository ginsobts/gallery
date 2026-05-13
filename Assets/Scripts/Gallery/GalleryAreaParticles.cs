using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GalleryAreaParticles : MonoBehaviour
{
    public enum ParticleStyle
    {
        Sakura,
        Snow,
        Firefly,
        Bubble,
        Leaves,
        Dust
    }

    [Header("粒子样式")]
    [SerializeField] private ParticleStyle style = ParticleStyle.Firefly;
    [Tooltip("粒子数量")]
    [SerializeField] private int particleCount = 20;
    [Tooltip("粒子大小范围")]
    [SerializeField] private Vector2 sizeRange = new Vector2(0.05f, 0.15f);
    [Tooltip("粒子颜色")]
    [SerializeField] private Color particleColor = new Color(1f, 0.9f, 0.5f, 0.7f);
    [Tooltip("漂浮速度")]
    [SerializeField] private float driftSpeed = 0.5f;
    [Tooltip("仅在玩家进入区域时激活")]
    [SerializeField] private bool activateOnEnter = true;

    private struct Particle
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Vector3 basePos;
        public float phase;
        public float speed;
        public float size;
    }

    private Particle[] particles;
    private BoxCollider2D areaCol;
    private bool active;

    private void Awake()
    {
        areaCol = GetComponent<BoxCollider2D>();
        areaCol.isTrigger = true;
    }

    private void Start()
    {
        CreateParticles();
        if (activateOnEnter)
            SetParticlesActive(false);
        else
            active = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activateOnEnter) return;
        if (other.GetComponent<GalleryPlayer>() == null) return;
        active = true;
        SetParticlesActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!activateOnEnter) return;
        if (other.GetComponent<GalleryPlayer>() == null) return;
        active = false;
        SetParticlesActive(false);
    }

    private void Update()
    {
        if (!active || particles == null) return;

        for (int i = 0; i < particles.Length; i++)
        {
            var p = particles[i];
            if (p.go == null) continue;

            float t = Time.time * p.speed + p.phase;
            Vector3 offset = GetMotionOffset(t, i);
            p.go.transform.position = p.basePos + offset;

            float alpha = GetAlphaAnimation(t);
            p.sr.color = new Color(particleColor.r, particleColor.g, particleColor.b, particleColor.a * alpha);
        }
    }

    private Vector3 GetMotionOffset(float t, int index)
    {
        switch (style)
        {
            case ParticleStyle.Sakura:
            case ParticleStyle.Leaves:
                return new Vector3(
                    Mathf.Sin(t * 1.3f) * 0.5f,
                    -Mathf.Repeat(t * 0.3f, 2f) + 1f,
                    0);

            case ParticleStyle.Snow:
                return new Vector3(
                    Mathf.Sin(t * 0.8f + index) * 0.3f,
                    -Mathf.Repeat(t * 0.2f, 2f) + 1f,
                    0);

            case ParticleStyle.Firefly:
                return new Vector3(
                    Mathf.Sin(t * 0.7f) * 0.4f + Mathf.Cos(t * 1.1f) * 0.2f,
                    Mathf.Cos(t * 0.5f) * 0.3f + Mathf.Sin(t * 0.9f) * 0.2f,
                    0);

            case ParticleStyle.Bubble:
                return new Vector3(
                    Mathf.Sin(t * 0.6f) * 0.2f,
                    Mathf.Repeat(t * 0.25f, 2f) - 1f,
                    0);

            case ParticleStyle.Dust:
                return new Vector3(
                    Mathf.Sin(t * 0.4f + index * 0.5f) * 0.5f,
                    Mathf.Cos(t * 0.3f + index * 0.7f) * 0.3f,
                    0);

            default:
                return Vector3.zero;
        }
    }

    private float GetAlphaAnimation(float t)
    {
        switch (style)
        {
            case ParticleStyle.Firefly:
                return (Mathf.Sin(t * 2f) + 1f) * 0.5f;
            default:
                return 0.7f + Mathf.Sin(t * 1.5f) * 0.3f;
        }
    }

    private void CreateParticles()
    {
        particles = new Particle[particleCount];
        Vector3 center = transform.position + (Vector3)areaCol.offset;
        Vector3 halfSize = Vector3.Scale(areaCol.size, transform.localScale) * 0.5f;

        var sprite = RuntimeSprite.GetCircle(16);

        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject($"Particle_{i}");
            go.transform.SetParent(transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = particleColor;
            sr.sortingOrder = 4;

            float size = Random.Range(sizeRange.x, sizeRange.y);
            go.transform.localScale = Vector3.one * size;

            Vector3 pos = center + new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                0);

            go.transform.position = pos;

            particles[i] = new Particle
            {
                go = go,
                sr = sr,
                basePos = pos,
                phase = Random.Range(0f, Mathf.PI * 2f),
                speed = driftSpeed * Random.Range(0.7f, 1.3f),
                size = size
            };
        }
    }

    private void SetParticlesActive(bool state)
    {
        if (particles == null) return;
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i].go != null)
                particles[i].go.SetActive(state);
        }
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Color c;
        switch (style)
        {
            case ParticleStyle.Sakura: c = new Color(1f, 0.6f, 0.7f, 0.15f); break;
            case ParticleStyle.Snow: c = new Color(0.8f, 0.9f, 1f, 0.15f); break;
            case ParticleStyle.Firefly: c = new Color(1f, 0.9f, 0.3f, 0.15f); break;
            case ParticleStyle.Bubble: c = new Color(0.5f, 0.8f, 1f, 0.15f); break;
            case ParticleStyle.Leaves: c = new Color(0.4f, 0.7f, 0.2f, 0.15f); break;
            default: c = new Color(0.6f, 0.6f, 0.6f, 0.15f); break;
        }
        Gizmos.color = c;
        Gizmos.DrawCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));
    }
}

using UnityEngine;

[RequireComponent(typeof(GalleryVehicle))]
public class GalleryVehicleEffects : MonoBehaviour
{
    [Header("变身动画")]
    [Tooltip("变身时缩放动画时长")]
    [SerializeField] private float morphDuration = 0.3f;
    [Tooltip("变身中间缩放（挤压效果）")]
    [SerializeField] private Vector2 squashScale = new Vector2(1.3f, 0.5f);
    [Tooltip("变身时闪白")]
    [SerializeField] private bool flashOnMorph = true;
    [Tooltip("闪白颜色")]
    [SerializeField] private Color flashColor = new Color(1f, 1f, 0.9f, 0.8f);

    [Header("移动粒子")]
    [Tooltip("是否启用移动粒子")]
    [SerializeField] private bool enableMoveParticles = true;
    [Tooltip("粒子颜色")]
    [SerializeField] private Color particleColor = new Color(0.8f, 0.85f, 1f, 0.4f);
    [Tooltip("粒子生成间隔")]
    [SerializeField] private float particleInterval = 0.08f;
    [Tooltip("粒子大小")]
    [SerializeField] private float particleSize = 0.1f;
    [Tooltip("粒子生命周期")]
    [SerializeField] private float particleLife = 0.5f;

    private GalleryVehicle vehicle;
    private SpriteRenderer sr;
    private SpriteRenderer flashSR;
    private Vector3 baseScale;
    private float morphTimer = -1f;
    private float particleTimer;
    private Vector3 lastPos;

    private struct VFXParticle
    {
        public GameObject go;
        public SpriteRenderer sr;
        public float birth;
        public Vector2 velocity;
    }

    private VFXParticle[] particles;
    private int pHead;
    private const int MAX_PARTICLES = 30;

    private void Start()
    {
        vehicle = GetComponent<GalleryVehicle>();
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        lastPos = transform.position;
        particles = new VFXParticle[MAX_PARTICLES];

        if (vehicle != null)
            vehicle.OnVehicleChanged += OnVehicleSwitch;

        if (flashOnMorph)
        {
            var flashGO = new GameObject("VehicleFlash");
            flashGO.transform.SetParent(transform);
            flashGO.transform.localPosition = Vector3.zero;
            flashGO.transform.localScale = Vector3.one * 1.5f;
            flashSR = flashGO.AddComponent<SpriteRenderer>();
            flashSR.sprite = RuntimeSprite.GetCircle(16);
            flashSR.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            flashSR.sortingOrder = sr != null ? sr.sortingOrder + 1 : 11;
        }
    }

    private void OnDestroy()
    {
        if (vehicle != null)
            vehicle.OnVehicleChanged -= OnVehicleSwitch;
    }

    private void OnVehicleSwitch(int newIndex)
    {
        TriggerMorph();
    }

    private void Update()
    {
        UpdateMorphAnimation();
        UpdateMoveParticles();
        UpdateParticlePool();
    }

    private void TriggerMorph()
    {
        morphTimer = 0f;
        if (flashSR != null)
            flashSR.color = flashColor;
    }

    private void UpdateMorphAnimation()
    {
        if (morphTimer < 0f) return;

        morphTimer += Time.deltaTime;
        float t = morphTimer / morphDuration;

        if (t <= 0.5f)
        {
            float p = t * 2f;
            float sx = Mathf.Lerp(1f, squashScale.x, p);
            float sy = Mathf.Lerp(1f, squashScale.y, p);
            transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
        }
        else if (t <= 1f)
        {
            float p = (t - 0.5f) * 2f;
            float sx = Mathf.Lerp(squashScale.x, 1f, p);
            float sy = Mathf.Lerp(squashScale.y, 1f, p);
            transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
        }
        else
        {
            transform.localScale = baseScale;
            morphTimer = -1f;
        }

        if (flashSR != null && morphTimer >= 0f)
        {
            float alpha = flashColor.a * (1f - t);
            flashSR.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
        }
    }

    private void UpdateMoveParticles()
    {
        if (!enableMoveParticles) return;

        Vector3 pos = transform.position;
        float speed = (pos - lastPos).magnitude / Time.deltaTime;
        lastPos = pos;

        if (speed < 0.5f) return;

        particleTimer += Time.deltaTime;
        if (particleTimer >= particleInterval)
        {
            particleTimer -= particleInterval;
            SpawnParticle(pos);
        }
    }

    private void SpawnParticle(Vector3 pos)
    {
        int idx = pHead;
        pHead = (pHead + 1) % MAX_PARTICLES;

        if (particles[idx].go != null)
            Destroy(particles[idx].go);

        var go = new GameObject("VP");
        go.transform.position = pos + new Vector3(
            Random.Range(-0.2f, 0.2f), Random.Range(-0.15f, 0f), 0);

        var psr = go.AddComponent<SpriteRenderer>();
        psr.sprite = RuntimeSprite.GetCircle(8);
        psr.color = particleColor;
        psr.sortingOrder = 0;
        go.transform.localScale = Vector3.one * particleSize;

        particles[idx] = new VFXParticle
        {
            go = go, sr = psr,
            birth = Time.time,
            velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.2f, 0.5f))
        };
    }

    private void UpdateParticlePool()
    {
        float now = Time.time;
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            if (particles[i].go == null) continue;
            float age = now - particles[i].birth;
            if (age >= particleLife)
            {
                Destroy(particles[i].go);
                particles[i].go = null;
                continue;
            }

            float t = age / particleLife;
            particles[i].go.transform.position += (Vector3)(particles[i].velocity * Time.deltaTime);
            Color c = particles[i].sr.color;
            c.a = particleColor.a * (1f - t);
            particles[i].sr.color = c;
            particles[i].go.transform.localScale = Vector3.one * particleSize * (1f - t * 0.5f);
        }
    }
}

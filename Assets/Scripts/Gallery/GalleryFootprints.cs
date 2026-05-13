using UnityEngine;

public class GalleryFootprints : MonoBehaviour
{
    [Header("脚印参数")]
    [SerializeField] private float stepDistance = 0.35f;
    [SerializeField] private float footprintSize = 0.12f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int maxFootprints = 60;

    [Header("脚印样式")]
    [SerializeField] private Sprite defaultFootprintSprite;
    [SerializeField] private Color defaultColor = new Color(0.35f, 0.3f, 0.25f, 0.4f);

    [Header("地面类型对应的脚印样式")]
    [SerializeField] private GroundFootprintStyle[] groundStyles;

    [System.Serializable]
    public class GroundFootprintStyle
    {
        public GalleryGroundType.GroundMaterial groundMaterial;
        public Sprite sprite;
        public Color color = new Color(0.35f, 0.3f, 0.25f, 0.4f);
        public float sizeMultiplier = 1f;
    }

    private struct Footprint
    {
        public GameObject go;
        public SpriteRenderer sr;
        public float spawnTime;
        public float baseAlpha;
    }

    private Footprint[] pool;
    private int head;
    private int count;
    private Vector3 lastPos;
    private float distAccum;
    private bool leftFoot = true;
    private GalleryGroundType.GroundMaterial currentGround;

    private void Start()
    {
        pool = new Footprint[maxFootprints];
        head = 0;
        count = 0;
        lastPos = transform.position;
    }

    private void Update()
    {
        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos;
        float moved = delta.magnitude;

        if (moved > 0.001f)
        {
            distAccum += moved;
            if (distAccum >= stepDistance)
            {
                distAccum -= stepDistance;
                SpawnFootprint(pos, delta.normalized);
            }
        }

        lastPos = pos;
        FadeFootprints();
    }

    private void SpawnFootprint(Vector3 pos, Vector2 dir)
    {
        var style = GetStyleForGround(currentGround);

        int idx = (head + count) % maxFootprints;
        if (count >= maxFootprints)
        {
            if (pool[head].go != null) Destroy(pool[head].go);
            head = (head + 1) % maxFootprints;
        }
        else
        {
            count++;
        }

        var go = new GameObject("FP");
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float sideOffset = leftFoot ? -0.06f : 0.06f;
        Vector3 side = new Vector3(-dir.y, dir.x, 0) * sideOffset;
        go.transform.position = pos + side;
        go.transform.rotation = Quaternion.Euler(0, 0, angle);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = style.sprite != null ? style.sprite
            : (defaultFootprintSprite != null ? defaultFootprintSprite : RuntimeSprite.GetCircle(8));
        sr.color = style.color;
        sr.sortingOrder = -1;
        float s = footprintSize * style.sizeMultiplier;
        go.transform.localScale = new Vector3(s, s * 1.3f, 1f);

        pool[idx] = new Footprint
        {
            go = go, sr = sr,
            spawnTime = Time.time,
            baseAlpha = style.color.a
        };

        leftFoot = !leftFoot;
    }

    private void FadeFootprints()
    {
        float fadeStart = lifetime * 0.5f;
        float now = Time.time;

        for (int i = 0; i < count; i++)
        {
            int idx = (head + i) % maxFootprints;
            ref var fp = ref pool[idx];
            if (fp.go == null) continue;

            float age = now - fp.spawnTime;
            if (age >= lifetime)
            {
                Destroy(fp.go);
                fp.go = null;
                continue;
            }

            if (age > fadeStart)
            {
                float t = (age - fadeStart) / (lifetime - fadeStart);
                Color c = fp.sr.color;
                c.a = Mathf.Lerp(fp.baseAlpha, 0, t);
                fp.sr.color = c;
            }
        }

        while (count > 0 && pool[head].go == null)
        {
            head = (head + 1) % maxFootprints;
            count--;
        }
    }

    private GroundFootprintStyle GetStyleForGround(GalleryGroundType.GroundMaterial mat)
    {
        if (groundStyles != null)
            foreach (var gs in groundStyles)
                if (gs.groundMaterial == mat) return gs;

        return new GroundFootprintStyle
        {
            groundMaterial = GalleryGroundType.GroundMaterial.Default,
            sprite = null, color = defaultColor, sizeMultiplier = 1f
        };
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var gt = other.GetComponent<GalleryGroundType>();
        if (gt != null) currentGround = gt.Material;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var gt = other.GetComponent<GalleryGroundType>();
        if (gt != null && gt.Material == currentGround)
            currentGround = GalleryGroundType.GroundMaterial.Default;
    }
}

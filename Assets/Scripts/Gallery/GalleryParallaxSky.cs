using UnityEngine;

public class GalleryParallaxSky : MonoBehaviour
{
    [System.Serializable]
    public struct SkyLayer
    {
        [Tooltip("该层的 Sprite（留空自动生成云朵）")]
        public Sprite sprite;
        [Tooltip("该层的颜色")]
        public Color color;
        [Tooltip("视差系数 (0=完全锁定相机, 1=完全跟随世界)")]
        [Range(0f, 1f)]
        public float parallaxFactor;
        [Tooltip("自身水平漂移速度")]
        public float driftSpeed;
        [Tooltip("Y 偏移（相对于相机中心）")]
        public float yOffset;
        [Tooltip("缩放")]
        public Vector2 scale;
        [Tooltip("排序层级")]
        public int sortingOrder;
    }

    [Header("天空层")]
    [SerializeField] private SkyLayer[] layers = new SkyLayer[]
    {
        new SkyLayer
        {
            color = new Color(0.85f, 0.88f, 0.95f, 0.25f),
            parallaxFactor = 0.05f, driftSpeed = 0.15f,
            yOffset = 3f, scale = new Vector2(12f, 2f), sortingOrder = -99
        },
        new SkyLayer
        {
            color = new Color(0.9f, 0.92f, 1f, 0.18f),
            parallaxFactor = 0.02f, driftSpeed = 0.08f,
            yOffset = 4f, scale = new Vector2(18f, 2.5f), sortingOrder = -98
        }
    };

    [Header("全局")]
    [Tooltip("是否在 X 方向无限平铺")]
    [SerializeField] private bool tileX = true;

    private struct LayerState
    {
        public SpriteRenderer sr;
        public SpriteRenderer srClone;
        public float drift;
        public float tileWidth;
    }

    private LayerState[] states;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        states = new LayerState[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            var l = layers[i];
            var go = new GameObject($"SkyLayer_{i}");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = l.sprite != null ? l.sprite : RuntimeSprite.Get();
            sr.color = l.color;
            sr.sortingOrder = l.sortingOrder;
            go.transform.localScale = new Vector3(l.scale.x, l.scale.y, 1f);

            SpriteRenderer clone = null;
            float tw = l.scale.x;
            if (tileX)
            {
                var cloneGO = new GameObject($"SkyLayer_{i}_Clone");
                cloneGO.transform.SetParent(transform);
                clone = cloneGO.AddComponent<SpriteRenderer>();
                clone.sprite = sr.sprite;
                clone.color = sr.color;
                clone.sortingOrder = sr.sortingOrder;
                cloneGO.transform.localScale = go.transform.localScale;
            }

            states[i] = new LayerState { sr = sr, srClone = clone, drift = 0f, tileWidth = tw };
        }
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;

        for (int i = 0; i < layers.Length; i++)
        {
            var l = layers[i];
            ref var s = ref states[i];

            s.drift += l.driftSpeed * Time.deltaTime;

            float px = camPos.x * (1f - l.parallaxFactor) + s.drift;
            float py = camPos.y + l.yOffset;

            if (tileX && s.tileWidth > 0)
            {
                px = px % s.tileWidth;
            }

            s.sr.transform.position = new Vector3(px, py, 10f);

            if (s.srClone != null)
            {
                s.srClone.transform.position = new Vector3(px + s.tileWidth, py, 10f);
            }
        }
    }
}

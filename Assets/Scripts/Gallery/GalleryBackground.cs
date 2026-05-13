using UnityEngine;

public class GalleryBackground : MonoBehaviour
{
    [System.Serializable]
    public struct BackgroundZone
    {
        [Tooltip("区域中心位置（世界坐标）")]
        public Vector2 center;
        [Tooltip("该区域对应的背景图")]
        public Sprite background;
        [Tooltip("如果没有图片，使用纯色")]
        public Color fallbackColor;
        [Tooltip("区域生效半径")]
        public float radius;
    }

    [Header("背景区域")]
    [Tooltip("配置多个区域，玩家在不同位置看到不同背景")]
    [SerializeField] private BackgroundZone[] zones;

    [Header("默认背景")]
    [Tooltip("默认背景图（没有进入任何区域时显示）")]
    [SerializeField] private Sprite defaultBackground;
    [Tooltip("默认背景颜色")]
    [SerializeField] private Color defaultColor = new Color(0.08f, 0.08f, 0.12f);

    [Header("渐变设置")]
    [Tooltip("背景切换的渐变速度")]
    [SerializeField] private float transitionSpeed = 1.5f;
    [Tooltip("背景排序层级（应低于所有前景）")]
    [SerializeField] private int sortingOrder = -100;

    private SpriteRenderer bgSR_A;
    private SpriteRenderer bgSR_B;
    private bool usingA = true;
    private int currentZoneIndex = -1;
    private float fadeProgress = 1f;
    private Transform playerTransform;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
            playerTransform = player.transform;

        bgSR_A = CreateBGRenderer("BG_A");
        bgSR_B = CreateBGRenderer("BG_B");

        ApplyBackground(bgSR_A, defaultBackground, defaultColor);
        bgSR_A.color = SetAlpha(bgSR_A.color, 1f);
        bgSR_B.color = SetAlpha(bgSR_B.color, 0f);
    }

    private void LateUpdate()
    {
        if (playerTransform == null || mainCam == null) return;

        FollowCamera();

        int bestZone = FindBestZone();
        if (bestZone != currentZoneIndex)
        {
            currentZoneIndex = bestZone;
            StartTransition(bestZone);
        }

        if (fadeProgress < 1f)
            UpdateTransition();
    }

    private void FollowCamera()
    {
        Vector3 camPos = mainCam.transform.position;
        bgSR_A.transform.position = new Vector3(camPos.x, camPos.y, 10f);
        bgSR_B.transform.position = new Vector3(camPos.x, camPos.y, 10f);

        float camH = mainCam.orthographicSize * 2f;
        float camW = camH * mainCam.aspect;
        Vector3 scale = new Vector3(camW + 1f, camH + 1f, 1f);
        bgSR_A.transform.localScale = scale;
        bgSR_B.transform.localScale = scale;
    }

    private int FindBestZone()
    {
        if (zones == null || zones.Length == 0) return -1;

        int best = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < zones.Length; i++)
        {
            float dist = Vector2.Distance(playerTransform.position, zones[i].center);
            if (dist <= zones[i].radius && dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }
        return best;
    }

    private void StartTransition(int zoneIndex)
    {
        var incoming = usingA ? bgSR_B : bgSR_A;

        if (zoneIndex >= 0)
            ApplyBackground(incoming, zones[zoneIndex].background, zones[zoneIndex].fallbackColor);
        else
            ApplyBackground(incoming, defaultBackground, defaultColor);

        incoming.color = SetAlpha(incoming.color, 0f);
        fadeProgress = 0f;
        usingA = !usingA;
    }

    private void UpdateTransition()
    {
        fadeProgress += transitionSpeed * Time.deltaTime;
        fadeProgress = Mathf.Clamp01(fadeProgress);

        var current = usingA ? bgSR_A : bgSR_B;
        var outgoing = usingA ? bgSR_B : bgSR_A;

        current.color = SetAlpha(current.color, fadeProgress);
        outgoing.color = SetAlpha(outgoing.color, 1f - fadeProgress);

        current.sortingOrder = sortingOrder + 1;
        outgoing.sortingOrder = sortingOrder;
    }

    private SpriteRenderer CreateBGRenderer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    private void ApplyBackground(SpriteRenderer sr, Sprite sprite, Color color)
    {
        if (sprite != null)
        {
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Sliced;
        }
        else
        {
            sr.sprite = RuntimeSprite.Get();
            sr.color = color;
        }
    }

    public void SetDefaultColor(Color color)
    {
        defaultColor = color;
        if (currentZoneIndex < 0)
        {
            var active = usingA ? bgSR_A : bgSR_B;
            active.color = color;
        }
    }

    private Color SetAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }

    private void OnDrawGizmos()
    {
        if (zones == null) return;
        for (int i = 0; i < zones.Length; i++)
        {
            var z = zones[i];
            Color c = z.fallbackColor;
            c.a = 0.15f;
            Gizmos.color = c;
            Gizmos.DrawSphere((Vector3)z.center, z.radius);
            Gizmos.color = new Color(c.r, c.g, c.b, 0.5f);
            Gizmos.DrawWireSphere((Vector3)z.center, z.radius);

#if UNITY_EDITOR
            UnityEditor.Handles.Label((Vector3)z.center, $"BG Zone {i}");
#endif
        }
    }
}

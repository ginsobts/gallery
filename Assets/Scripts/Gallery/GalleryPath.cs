using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class GalleryPath : MonoBehaviour
{
    [Header("路径点（本地坐标）")]
    [Tooltip("路径的控制点，在 Scene 视图中可拖动编辑")]
    public List<Vector2> points = new List<Vector2>
    {
        new Vector2(-3, 0),
        new Vector2(0, 0),
        new Vector2(3, 0)
    };

    [Header("外观")]
    [Tooltip("小路颜色（仅在无自定义材质时生效）")]
    [SerializeField] private Color pathColor = new Color(0.92f, 0.9f, 0.86f);
    [Tooltip("自定义材质（拖入即用，留空则使用纯色）")]
    [SerializeField] private Material customMaterial;
    [Tooltip("纹理平铺模式")]
    [SerializeField] private LineTextureMode textureMode = LineTextureMode.Tile;
    [Tooltip("小路宽度（世界单位）")]
    [SerializeField] private float pathWidth = 0.3f;
    [Tooltip("曲线细分段数（每两个控制点之间）")]
    [SerializeField] private int smoothSegments = 8;
    [Tooltip("排序层级")]
    [SerializeField] private int sortingOrder = -5;

    [Header("边缘柔化（仅纯色模式）")]
    [Tooltip("边缘渐变宽度（0=硬边）")]
    [SerializeField] private float edgeSoftness = 0.06f;

    private LineRenderer lr;
    private static Material sharedDefaultMat;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        if (lr != null) Rebuild();
    }

    public void Rebuild()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.useWorldSpace = false;
        lr.loop = false;
        lr.sortingOrder = sortingOrder;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.alignment = LineAlignment.TransformZ;
        lr.textureMode = textureMode;

        if (customMaterial != null)
        {
            lr.sharedMaterial = customMaterial;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            );
            lr.colorGradient = g;
        }
        else
        {
            if (sharedDefaultMat == null)
            {
                sharedDefaultMat = new Material(Shader.Find("Sprites/Default"));
                sharedDefaultMat.name = "PathMaterial_Default";
            }
            lr.sharedMaterial = sharedDefaultMat;

            if (edgeSoftness > 0.001f)
            {
                float core = 1f - edgeSoftness / Mathf.Max(pathWidth * 0.5f, 0.01f);
                core = Mathf.Clamp01(core);
                Gradient colorGrad = new Gradient();
                colorGrad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(pathColor, 0f), new GradientColorKey(pathColor, 1f) },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(pathColor.a, (1f - core) * 0.5f),
                        new GradientAlphaKey(pathColor.a, 1f - (1f - core) * 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                lr.colorGradient = colorGrad;
            }
            else
            {
                lr.startColor = pathColor;
                lr.endColor = pathColor;
            }
        }

        lr.startWidth = pathWidth;
        lr.endWidth = pathWidth;
        lr.widthMultiplier = 1f;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;

        var smooth = GetSmoothPoints();
        lr.positionCount = smooth.Count;
        for (int i = 0; i < smooth.Count; i++)
            lr.SetPosition(i, new Vector3(smooth[i].x, smooth[i].y, 0));
    }

    private List<Vector2> GetSmoothPoints()
    {
        if (points == null || points.Count < 2)
            return new List<Vector2>(points ?? new List<Vector2>());

        if (smoothSegments <= 1 || points.Count < 3)
            return new List<Vector2>(points);

        var result = new List<Vector2>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p0 = points[Mathf.Max(i - 1, 0)];
            Vector2 p1 = points[i];
            Vector2 p2 = points[Mathf.Min(i + 1, points.Count - 1)];
            Vector2 p3 = points[Mathf.Min(i + 2, points.Count - 1)];

            for (int s = 0; s < smoothSegments; s++)
            {
                float t = (float)s / smoothSegments;
                result.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        result.Add(points[points.Count - 1]);
        return result;
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Count < 2) return;
        Gizmos.color = new Color(pathColor.r, pathColor.g, pathColor.b, 0.4f);
        var smooth = GetSmoothPoints();
        for (int i = 0; i < smooth.Count - 1; i++)
        {
            Vector3 a = transform.TransformPoint(smooth[i]);
            Vector3 b = transform.TransformPoint(smooth[i + 1]);
            Gizmos.DrawLine(a, b);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (points == null) return;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 wp = transform.TransformPoint(points[i]);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(wp, 0.12f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(wp + Vector3.up * 0.2f, i.ToString());
#endif
        }
    }
}

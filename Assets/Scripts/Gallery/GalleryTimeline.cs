using UnityEngine;

public class GalleryTimeline : MonoBehaviour
{
    [System.Serializable]
    public struct TimelinePoint
    {
        [Tooltip("路标位置（世界坐标）")]
        public Vector2 position;
        [Tooltip("日期文字")]
        public string dateText;
        [Tooltip("标注颜色")]
        public Color color;
    }

    [Header("时间线")]
    [Tooltip("时间线路标点")]
    [SerializeField] private TimelinePoint[] points;
    [Tooltip("连接线颜色")]
    [SerializeField] private Color lineColor = new Color(0.6f, 0.6f, 0.7f, 0.4f);
    [Tooltip("连接线宽度")]
    [SerializeField] private float lineWidth = 0.05f;
    [Tooltip("路标圆点大小")]
    [SerializeField] private float dotSize = 0.2f;
    [Tooltip("文字大小")]
    [SerializeField] private float textSize = 0.08f;
    [Tooltip("排序层级")]
    [SerializeField] private int sortingOrder = -2;

    private void Start()
    {
        if (points == null || points.Length == 0) return;

        CreateLines();
        CreateMarkers();
    }

    private static Material sharedLineMaterial;

    private void CreateLines()
    {
        if (sharedLineMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) sharedLineMaterial = new Material(shader);
        }

        for (int i = 0; i < points.Length - 1; i++)
        {
            var lineGO = new GameObject("Timeline_Line_" + i);
            lineGO.transform.SetParent(transform);

            var lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, (Vector3)points[i].position);
            lr.SetPosition(1, (Vector3)points[i + 1].position);
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.sortingOrder = sortingOrder;
            if (sharedLineMaterial != null) lr.sharedMaterial = sharedLineMaterial;
            lr.useWorldSpace = true;
        }
    }

    private void CreateMarkers()
    {
        var sprite = RuntimeSprite.GetCircle(16);

        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            Color c = point.color.a > 0 ? point.color : new Color(0.9f, 0.7f, 0.2f);

            var markerGO = new GameObject($"Timeline_Marker_{i}");
            markerGO.transform.SetParent(transform);
            markerGO.transform.position = (Vector3)point.position;

            var sr = markerGO.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = sortingOrder + 1;
            markerGO.transform.localScale = Vector3.one * dotSize;

            if (!string.IsNullOrEmpty(point.dateText))
            {
                var textGO = new GameObject("DateText");
                textGO.transform.SetParent(markerGO.transform);
                textGO.transform.localPosition = new Vector3(0, dotSize * 3f, 0);
                textGO.transform.localScale = Vector3.one * (1f / dotSize);

                var tm = textGO.AddComponent<TextMesh>();
                tm.text = point.dateText;
                tm.characterSize = textSize;
                tm.fontSize = 60;
                tm.anchor = TextAnchor.LowerCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = c;
                textGO.GetComponent<MeshRenderer>().sortingOrder = sortingOrder + 2;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Length == 0) return;

        Gizmos.color = lineColor;
        for (int i = 0; i < points.Length - 1; i++)
            Gizmos.DrawLine((Vector3)points[i].position, (Vector3)points[i + 1].position);

        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.color = points[i].color.a > 0 ? points[i].color : new Color(0.9f, 0.7f, 0.2f);
            Gizmos.DrawSphere((Vector3)points[i].position, dotSize * 0.5f);

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(points[i].dateText))
                UnityEditor.Handles.Label((Vector3)points[i].position + Vector3.up * 0.4f, points[i].dateText);
#endif
        }
    }
}

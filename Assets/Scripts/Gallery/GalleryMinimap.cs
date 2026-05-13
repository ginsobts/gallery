using UnityEngine;
using UnityEngine.UI;

public class GalleryMinimap : MonoBehaviour
{
    [Header("小地图设置")]
    [Tooltip("小地图显示的世界范围大小")]
    [SerializeField] private Vector2 worldSize = new Vector2(20f, 12f);
    [Tooltip("小地图 UI 大小（像素）")]
    [SerializeField] private Vector2 mapSize = new Vector2(200f, 140f);
    [Tooltip("小地图位置锚点（屏幕角落）")]
    [SerializeField] private Vector2 anchorPosition = new Vector2(-20f, -20f);
    [Tooltip("背景颜色")]
    [SerializeField] private Color bgColor = new Color(0.05f, 0.05f, 0.1f, 0.7f);
    [Tooltip("边框颜色")]
    [SerializeField] private Color borderColor = new Color(0.5f, 0.5f, 0.6f, 0.8f);
    [Tooltip("玩家标记颜色")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.9f, 0.4f);
    [Tooltip("区域标记颜色")]
    [SerializeField] private Color areaColor = new Color(0.9f, 0.7f, 0.2f, 0.6f);
    [Tooltip("NPC 标记颜色")]
    [SerializeField] private Color npcColor = new Color(0.4f, 0.7f, 1f, 0.8f);

    private Canvas mapCanvas;
    private RectTransform mapPanel;
    private Image playerDot;
    private Image[] areaDots;
    private Image[] npcDots;
    private Transform playerTransform;
    private GalleryFollower[] cachedFollowers;
    private RectTransform playerDotRT;
    private RectTransform[] npcDotRTs;

    private void Start()
    {
        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
            playerTransform = player.transform;

        CreateMinimapUI();
    }

    private void LateUpdate()
    {
        if (playerTransform == null || mapPanel == null) return;
        UpdatePlayerDot();
        UpdateNPCDots();
    }

    private void CreateMinimapUI()
    {
        var canvasGO = new GameObject("MinimapCanvas");
        canvasGO.transform.SetParent(transform);
        mapCanvas = canvasGO.AddComponent<Canvas>();
        mapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mapCanvas.sortingOrder = 500;
        canvasGO.AddComponent<CanvasScaler>();

        var panelGO = new GameObject("MapPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        mapPanel = panelGO.GetComponent<RectTransform>();
        mapPanel.anchorMin = new Vector2(1, 1);
        mapPanel.anchorMax = new Vector2(1, 1);
        mapPanel.pivot = new Vector2(1, 1);
        mapPanel.anchoredPosition = anchorPosition;
        mapPanel.sizeDelta = mapSize;

        var bgImg = panelGO.AddComponent<Image>();
        bgImg.color = bgColor;

        var borderGO = new GameObject("Border");
        borderGO.transform.SetParent(panelGO.transform, false);
        var borderImg = borderGO.AddComponent<Image>();
        borderImg.color = borderColor;
        var borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-2, -2);
        borderRT.offsetMax = new Vector2(2, 2);
        borderGO.transform.SetAsFirstSibling();

        CreateAreaMarkers(panelGO.transform);
        CreateNPCMarkers(panelGO.transform);
        CreatePlayerDot(panelGO.transform);
    }

    private void CreatePlayerDot(Transform parent)
    {
        var dotGO = new GameObject("PlayerDot");
        dotGO.transform.SetParent(parent, false);
        playerDot = dotGO.AddComponent<Image>();
        playerDot.color = playerColor;
        playerDotRT = dotGO.GetComponent<RectTransform>();
        playerDotRT.sizeDelta = new Vector2(8, 8);
    }

    private void CreateAreaMarkers(Transform parent)
    {
        var frames = FindObjectsOfType<GalleryFrame>();
        areaDots = new Image[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            var dotGO = new GameObject($"AreaDot_{i}");
            dotGO.transform.SetParent(parent, false);
            areaDots[i] = dotGO.AddComponent<Image>();
            areaDots[i].color = areaColor;
            var rt = dotGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(6, 6);

            Vector2 pos = WorldToMap(frames[i].transform.position);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }
    }

    private void CreateNPCMarkers(Transform parent)
    {
        cachedFollowers = FindObjectsOfType<GalleryFollower>();
        var followers = cachedFollowers;
        npcDots = new Image[followers.Length];
        npcDotRTs = new RectTransform[followers.Length];
        for (int i = 0; i < followers.Length; i++)
        {
            var dotGO = new GameObject($"NPCDot_{i}");
            dotGO.transform.SetParent(parent, false);
            npcDots[i] = dotGO.AddComponent<Image>();
            npcDots[i].color = npcColor;
            npcDotRTs[i] = dotGO.GetComponent<RectTransform>();
            npcDotRTs[i].sizeDelta = new Vector2(6, 6);
            npcDotRTs[i].anchorMin = new Vector2(0.5f, 0.5f);
            npcDotRTs[i].anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    private void UpdatePlayerDot()
    {
        if (playerDotRT == null) return;
        playerDotRT.anchoredPosition = WorldToMap(playerTransform.position);
    }

    private void UpdateNPCDots()
    {
        if (npcDotRTs == null || cachedFollowers == null) return;
        for (int i = 0; i < npcDotRTs.Length && i < cachedFollowers.Length; i++)
        {
            if (npcDotRTs[i] == null) continue;
            npcDotRTs[i].anchoredPosition = WorldToMap(cachedFollowers[i].transform.position);
        }
    }

    private Vector2 WorldToMap(Vector3 worldPos)
    {
        float x = (worldPos.x / worldSize.x) * mapSize.x;
        float y = (worldPos.y / worldSize.y) * mapSize.y;
        return new Vector2(x, y);
    }
}

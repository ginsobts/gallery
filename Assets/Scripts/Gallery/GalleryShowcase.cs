using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GalleryShowcase : MonoBehaviour
{
    [Header("展示墙设置")]
    [Tooltip("按此键打开/关闭展示墙")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [Tooltip("展示槽数量（一行最多几个）")]
    [SerializeField] private int slotsPerRow = 6;
    [Tooltip("展示槽大小")]
    [SerializeField] private float slotSize = 80f;

    private static GalleryShowcase instance;
    private Canvas showcaseCanvas;
    private GameObject panelGO;
    private bool isOpen;

    private static List<CollectedItem> collectedItems = new List<CollectedItem>();
    private int cachedTotalSlots = -1;

    private struct CollectedItem
    {
        public string id;
        public string name;
        public Sprite icon;
        public Color color;
    }

    private void Awake()
    {
        instance = this;
        cachedTotalSlots = -1;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen) Close();
            else Open();
        }
    }

    public static void NotifyCollected(string id, string name, Sprite icon, Color color)
    {
        foreach (var item in collectedItems)
            if (item.id == id) return;

        collectedItems.Add(new CollectedItem { id = id, name = name, icon = icon, color = color });

        if (instance != null && instance.isOpen)
            instance.RefreshUI();
    }

    private void Open()
    {
        isOpen = true;
        CreateUI();

        GalleryPlayer.Freeze();
    }

    private void Close()
    {
        isOpen = false;
        if (showcaseCanvas != null)
            Destroy(showcaseCanvas.gameObject);

        GalleryPlayer.Unfreeze();
    }

    private void OnDestroy()
    {
        if (isOpen) GalleryPlayer.Unfreeze();
        if (instance == this) instance = null;
    }

    private void CreateUI()
    {
        if (showcaseCanvas != null)
            Destroy(showcaseCanvas.gameObject);

        var canvasGO = new GameObject("ShowcaseCanvas");
        showcaseCanvas = canvasGO.AddComponent<Canvas>();
        showcaseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        showcaseCanvas.sortingOrder = 8800;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
        Stretch(bgGO);

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleText = titleGO.AddComponent<Text>();
        titleText.text = $"旅行纪念品  {collectedItems.Count} / ?";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 30;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.9f, 0.6f);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.2f, 0.88f);
        titleRT.anchorMax = new Vector2(0.8f, 0.95f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        // Slots container
        panelGO = new GameObject("Slots");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.GetComponent<RectTransform>();
        if (panelRT == null) panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.1f, 0.1f);
        panelRT.anchorMax = new Vector2(0.9f, 0.85f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        RefreshUI();

        // Hint
        var hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        var hintText = hintGO.AddComponent<Text>();
        hintText.text = "按 I 关闭";
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 18;
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = new Color(1, 1, 1, 0.4f);
        var hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.35f, 0.02f);
        hintRT.anchorMax = new Vector2(0.65f, 0.07f);
        hintRT.offsetMin = Vector2.zero;
        hintRT.offsetMax = Vector2.zero;
    }

    private void RefreshUI()
    {
        if (panelGO == null) return;

        foreach (Transform child in panelGO.transform)
            Destroy(child.gameObject);

        if (cachedTotalSlots < 0)
        {
            var allCollectibles = FindObjectsOfType<GalleryCollectible>(true);
            cachedTotalSlots = allCollectibles.Length;
        }
        int totalSlots = Mathf.Max(cachedTotalSlots, collectedItems.Count + 3);

        float spacing = slotSize + 10f;
        float panelW = slotsPerRow * spacing;

        for (int i = 0; i < totalSlots; i++)
        {
            int row = i / slotsPerRow;
            int col = i % slotsPerRow;

            var slotGO = new GameObject($"Slot_{i}");
            slotGO.transform.SetParent(panelGO.transform, false);
            var slotRT = slotGO.AddComponent<RectTransform>();
            slotRT.anchorMin = new Vector2(0.5f, 1f);
            slotRT.anchorMax = new Vector2(0.5f, 1f);
            slotRT.pivot = new Vector2(0.5f, 1f);
            slotRT.sizeDelta = new Vector2(slotSize, slotSize);
            slotRT.anchoredPosition = new Vector2(
                (col - slotsPerRow * 0.5f + 0.5f) * spacing,
                -row * spacing - 10f);

            var slotImg = slotGO.AddComponent<Image>();

            CollectedItem? found = null;
            if (i < collectedItems.Count)
                found = collectedItems[i];

            if (found.HasValue)
            {
                var item = found.Value;
                slotImg.color = Color.white;
                if (item.icon != null)
                    slotImg.sprite = item.icon;
                else
                    slotImg.color = item.color;

                var nameGO = new GameObject("Name");
                nameGO.transform.SetParent(slotGO.transform, false);
                var nameText = nameGO.AddComponent<Text>();
                nameText.text = item.name;
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 12;
                nameText.alignment = TextAnchor.UpperCenter;
                nameText.color = new Color(0.9f, 0.85f, 0.7f);
                var nameRT = nameGO.GetComponent<RectTransform>();
                nameRT.anchorMin = new Vector2(0, 0);
                nameRT.anchorMax = new Vector2(1, 0);
                nameRT.pivot = new Vector2(0.5f, 1f);
                nameRT.sizeDelta = new Vector2(0, 20);
                nameRT.anchoredPosition = new Vector2(0, -2);
            }
            else
            {
                slotImg.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);

                var qGO = new GameObject("Q");
                qGO.transform.SetParent(slotGO.transform, false);
                var qText = qGO.AddComponent<Text>();
                qText.text = "?";
                qText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                qText.fontSize = 28;
                qText.alignment = TextAnchor.MiddleCenter;
                qText.color = new Color(0.4f, 0.4f, 0.5f, 0.5f);
                Stretch(qGO);
            }
        }
    }

    private void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

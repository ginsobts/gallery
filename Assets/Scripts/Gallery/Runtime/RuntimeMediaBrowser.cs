using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeMediaBrowser : MonoBehaviour
{
    public static RuntimeMediaBrowser Instance { get; private set; }

    private Canvas browserCanvas;
    private GameObject contentPanel;
    private ScrollRect scrollRect;
    private Action<string> onFileSelected;
    private string currentSceneName;
    private bool isOpen;

    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };
    private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".avi", ".mov" };
    private static readonly string[] AudioExtensions = { ".mp3", ".ogg", ".wav" };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CreateUI();
        browserCanvas.gameObject.SetActive(false);
    }

    public void Open(Action<string> callback)
    {
        onFileSelected = callback;
        var builder = RuntimeSceneBuilder.Instance;
        currentSceneName = builder != null ? builder.CurrentSceneName : "default";

        EnsureMediaFolder();
        RefreshFileList();
        browserCanvas.gameObject.SetActive(true);
        isOpen = true;
    }

    public void Close()
    {
        browserCanvas.gameObject.SetActive(false);
        isOpen = false;
        onFileSelected = null;
    }

    private void EnsureMediaFolder()
    {
        if (string.IsNullOrEmpty(currentSceneName)) return;
        SceneDataHelper.GetMediaPath(currentSceneName);

        string importDir = GetImportFolder();
        if (!Directory.Exists(importDir)) Directory.CreateDirectory(importDir);
    }

    private string GetImportFolder()
    {
        return Path.Combine(Application.persistentDataPath, "Gallery", "import");
    }

    private void RefreshFileList()
    {
        ClearContent();

        CreateHeaderLabel("场景媒体文件夹:");
        string mediaDir = SceneDataHelper.GetMediaPath(currentSceneName);
        CreatePathLabel(mediaDir);
        ListFiles(mediaDir, "media/");

        CreateHeaderLabel("导入文件夹 (放入文件后刷新):");
        string importDir = GetImportFolder();
        CreatePathLabel(importDir);
        ListImportFiles(importDir);

        CreateActionButton("从电脑选择文件...", () => PickFileFromDisk());
        CreateActionButton("刷新列表", () => RefreshFileList());
        CreateActionButton("打开文件夹", () => OpenFolder(mediaDir));
        CreateActionButton("关闭", () => Close());
    }

    private void ListFiles(string dir, string prefix)
    {
        if (!Directory.Exists(dir)) return;
        string[] files = Directory.GetFiles(dir);
        foreach (var f in files)
        {
            string ext = Path.GetExtension(f).ToLower();
            if (!IsMediaFile(ext)) continue;
            string fileName = Path.GetFileName(f);
            string relativePath = prefix + fileName;
            string icon = GetFileIcon(ext);
            CreateFileButton($"{icon} {fileName}", () => SelectFile(relativePath));
        }
    }

    private void ListImportFiles(string dir)
    {
        if (!Directory.Exists(dir)) return;
        string[] files = Directory.GetFiles(dir);
        foreach (var f in files)
        {
            string ext = Path.GetExtension(f).ToLower();
            if (!IsMediaFile(ext)) continue;
            string fileName = Path.GetFileName(f);
            string fullPath = f;
            CreateFileButton($"[导入] {fileName}", () => ImportAndSelect(fullPath));
        }
    }

    private void ImportAndSelect(string sourcePath)
    {
        string relativePath = RuntimeAssetLoader.Instance.CopyMediaToScene(sourcePath, currentSceneName);
        if (!string.IsNullOrEmpty(relativePath))
        {
            SelectFile(relativePath);
        }
    }

    private void SelectFile(string relativePath)
    {
        onFileSelected?.Invoke(relativePath);
        Close();
    }

    private void PickFileFromDisk()
    {
        string path = NativeFilePicker.PickMediaFile("选择要导入的文件");
        if (string.IsNullOrEmpty(path)) return;

        string relativePath = RuntimeAssetLoader.Instance.CopyMediaToScene(path, currentSceneName);
        if (!string.IsNullOrEmpty(relativePath))
        {
            Debug.Log("[MediaBrowser] Imported: " + path + " -> " + relativePath);
            SelectFile(relativePath);
        }
    }

    private bool IsMediaFile(string ext)
    {
        foreach (var e in ImageExtensions) if (e == ext) return true;
        foreach (var e in VideoExtensions) if (e == ext) return true;
        foreach (var e in AudioExtensions) if (e == ext) return true;
        return false;
    }

    private string GetFileIcon(string ext)
    {
        foreach (var e in ImageExtensions) if (e == ext) return "[IMG]";
        foreach (var e in VideoExtensions) if (e == ext) return "[VID]";
        foreach (var e in AudioExtensions) if (e == ext) return "[SND]";
        return "[?]";
    }

    private void OpenFolder(string path)
    {
#if UNITY_STANDALONE_WIN
        path = path.Replace("/", "\\");
        System.Diagnostics.Process.Start("explorer.exe", path);
#elif UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", path);
#endif
    }

    // ── UI ──

    private static Font _cachedFont;
    private static Font GetFont()
    {
        if (_cachedFont != null) return _cachedFont;
        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cachedFont == null) _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_cachedFont == null) _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        return _cachedFont;
    }

    private void CreateUI()
    {
        var canvasGO = new GameObject("MediaBrowserCanvas");
        canvasGO.transform.SetParent(transform, false);
        browserCanvas = canvasGO.AddComponent<Canvas>();
        browserCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        browserCanvas.sortingOrder = 32100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.7f);
        bgImg.raycastTarget = true;

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.2f, 0.1f);
        panelRT.anchorMax = new Vector2(0.8f, 0.9f);
        panelRT.offsetMin = Vector2.zero; panelRT.offsetMax = Vector2.zero;
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);

        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(panelGO.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10); scrollRT.offsetMax = new Vector2(-10, -10);
        scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = Color.clear;
        scrollGO.AddComponent<Mask>();

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);
        contentPanel = contentGO;

        var layout = contentGO.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
    }

    private void ClearContent()
    {
        if (contentPanel == null) return;
        for (int i = contentPanel.transform.childCount - 1; i >= 0; i--)
            Destroy(contentPanel.transform.GetChild(i).gameObject);
    }

    private void CreateHeaderLabel(string text)
    {
        var go = new GameObject("Header");
        go.transform.SetParent(contentPanel.transform, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = GetFont();
        t.fontSize = 16;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(0.8f, 0.9f, 1f);
        t.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 28;
        le.preferredHeight = 28;
    }

    private void CreatePathLabel(string path)
    {
        var go = new GameObject("Path");
        go.transform.SetParent(contentPanel.transform, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
        var t = go.AddComponent<Text>();
        t.text = path;
        t.font = GetFont();
        t.fontSize = 11;
        t.color = new Color(0.6f, 0.6f, 0.6f);
        t.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 20;
        le.preferredHeight = 20;
    }

    private void CreateFileButton(string label, Action onClick)
    {
        var go = new GameObject("FileBtn");
        go.transform.SetParent(contentPanel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 28);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.26f);
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
        btn.colors = new ColorBlock
        {
            normalColor = new Color(0.22f, 0.22f, 0.26f),
            highlightedColor = new Color(0.3f, 0.5f, 0.65f),
            pressedColor = new Color(0.2f, 0.35f, 0.5f),
            selectedColor = new Color(0.25f, 0.4f, 0.55f),
            disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
            colorMultiplier = 1f, fadeDuration = 0.1f
        };

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8, 0); textRT.offsetMax = new Vector2(-8, 0);
        var t = textGO.AddComponent<Text>();
        t.text = label;
        t.font = GetFont();
        t.fontSize = 13;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleLeft;
        t.raycastTarget = false;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 28;
        le.preferredHeight = 28;
    }

    private void CreateActionButton(string label, Action onClick)
    {
        var go = new GameObject("ActionBtn");
        go.transform.SetParent(contentPanel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 34);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.35f, 0.5f);
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
        var t = textGO.AddComponent<Text>();
        t.text = label;
        t.font = GetFont();
        t.fontSize = 15;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 34;
        le.preferredHeight = 34;
    }
}

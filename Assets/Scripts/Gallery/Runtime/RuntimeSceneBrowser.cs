using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeSceneBrowser : MonoBehaviour
{
    public static RuntimeSceneBrowser Instance { get; private set; }

    private Canvas browserCanvas;
    private GameObject contentPanel;
    private InputField newSceneInput;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CreateUI();
        browserCanvas.gameObject.SetActive(false);
    }

    public void Open()
    {
        RefreshList();
        browserCanvas.gameObject.SetActive(true);
        isOpen = true;
    }

    public void Close()
    {
        browserCanvas.gameObject.SetActive(false);
        isOpen = false;
    }

    private void Update()
    {
        if (!isOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void RefreshList()
    {
        ClearContent();

        CreateHeader("场景管理");
        CreateSubLabel($"存储位置: {SceneDataHelper.GetScenesRootPath()}");

        string[] scenes = SceneDataHelper.ListScenes();
        if (scenes.Length == 0)
        {
            CreateSubLabel("暂无保存的场景");
        }
        else
        {
            foreach (var sceneName in scenes)
            {
                CreateSceneRow(sceneName);
            }
        }

        CreateSeparator();
        CreateHeader("创建新场景");
        CreateNewSceneRow();
        CreateSeparator();
        CreateActionButton("打开存储文件夹", () => OpenFolder(SceneDataHelper.GetScenesRootPath()));
        CreateActionButton("关闭", () => Close());
    }

    private void CreateSceneRow(string sceneName)
    {
        var go = new GameObject("SceneRow");
        go.transform.SetParent(contentPanel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 36);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.padding = new RectOffset(4, 4, 2, 2);
        layout.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().minHeight = 36;

        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(go.transform, false);
        nameGO.AddComponent<RectTransform>();
        var nameText = nameGO.AddComponent<Text>();
        nameText.text = sceneName;
        nameText.font = GetFont();
        nameText.fontSize = 15;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.raycastTarget = false;
        nameGO.AddComponent<LayoutElement>().flexibleWidth = 1;

        CreateRowButton(go.transform, "加载", new Color(0.2f, 0.45f, 0.3f), () => LoadScene(sceneName));
        CreateRowButton(go.transform, "删除", new Color(0.5f, 0.2f, 0.2f), () => DeleteScene(sceneName));
    }

    private void CreateRowButton(Transform parent, string label, Color color, Action onClick)
    {
        var go = new GameObject("Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
        go.AddComponent<LayoutElement>().preferredWidth = 60;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
        var t = textGO.AddComponent<Text>();
        t.text = label;
        t.font = GetFont();
        t.fontSize = 13;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
    }

    private void CreateNewSceneRow()
    {
        var go = new GameObject("NewSceneRow");
        go.transform.SetParent(contentPanel.transform, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.padding = new RectOffset(4, 4, 2, 2);
        layout.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().minHeight = 36;

        var inputGO = new GameObject("Input");
        inputGO.transform.SetParent(go.transform, false);
        inputGO.AddComponent<RectTransform>();
        var inputImg = inputGO.AddComponent<Image>();
        inputImg.color = new Color(0.2f, 0.2f, 0.24f);
        newSceneInput = inputGO.AddComponent<InputField>();
        newSceneInput.targetGraphic = inputImg;
        inputGO.AddComponent<LayoutElement>().flexibleWidth = 1;

        var inputTextGO = new GameObject("Text");
        inputTextGO.transform.SetParent(inputGO.transform, false);
        var inputTextRT = inputTextGO.AddComponent<RectTransform>();
        inputTextRT.anchorMin = Vector2.zero; inputTextRT.anchorMax = Vector2.one;
        inputTextRT.offsetMin = new Vector2(5, 0); inputTextRT.offsetMax = new Vector2(-5, 0);
        var inputText = inputTextGO.AddComponent<Text>();
        inputText.font = GetFont();
        inputText.fontSize = 14;
        inputText.color = Color.white;
        inputText.supportRichText = false;
        newSceneInput.textComponent = inputText;

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(inputGO.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(5, 0); phRT.offsetMax = new Vector2(-5, 0);
        var phText = phGO.AddComponent<Text>();
        phText.text = "输入场景名称...";
        phText.font = GetFont();
        phText.fontSize = 14;
        phText.fontStyle = FontStyle.Italic;
        phText.color = new Color(0.5f, 0.5f, 0.5f);
        newSceneInput.placeholder = phText;

        CreateRowButton(go.transform, "创建", new Color(0.2f, 0.4f, 0.6f), () => CreateNewScene());
    }

    private void CreateNewScene()
    {
        if (newSceneInput == null) return;
        string name = newSceneInput.text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        name = SanitizeName(name);
        string scenePath = SceneDataHelper.GetScenePath(name);
        if (Directory.Exists(scenePath))
        {
            Debug.LogWarning($"Scene already exists: {name}");
            return;
        }

        Directory.CreateDirectory(scenePath);
        SceneDataHelper.GetMediaPath(name);
        var newScene = new SceneData { sceneName = name };
        newScene.Save(SceneDataHelper.GetSceneJsonPath(name));

        LoadScene(name);
    }

    private void LoadScene(string sceneName)
    {
        Close();

        var builder = RuntimeSceneBuilder.Instance;
        if (builder != null)
        {
            builder.LoadScene(sceneName);
            var editor = RuntimeEditor.Instance;
            if (editor != null)
                editor.LoadSceneForEditing(sceneName);
        }
    }

    private void DeleteScene(string sceneName)
    {
        string path = SceneDataHelper.GetScenePath(sceneName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            RefreshList();
        }
    }

    private string SanitizeName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            name = name.Replace(c, '_');
        return name;
    }

    private void OpenFolder(string path)
    {
#if UNITY_STANDALONE_WIN
        path = path.Replace("/", "\\");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        System.Diagnostics.Process.Start("explorer.exe", path);
#elif UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", path);
#endif
    }

    // ── UI Helpers ──

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
        var canvasGO = new GameObject("SceneBrowserCanvas");
        canvasGO.transform.SetParent(transform, false);
        browserCanvas = canvasGO.AddComponent<Canvas>();
        browserCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        browserCanvas.sortingOrder = 32200;
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
        bgImg.color = new Color(0, 0, 0, 0.75f);
        bgImg.raycastTarget = true;
        var bgBtn = bgGO.AddComponent<Button>();
        bgBtn.targetGraphic = bgImg;
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(() => Close());

        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.25f, 0.15f);
        panelRT.anchorMax = new Vector2(0.75f, 0.85f);
        panelRT.offsetMin = Vector2.zero; panelRT.offsetMax = Vector2.zero;
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.13f, 0.13f, 0.16f, 0.96f);

        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(panelGO.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(8, 8); scrollRT.offsetMax = new Vector2(-8, -8);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollGO.AddComponent<Image>().color = Color.clear;
        scrollGO.AddComponent<Mask>();

        contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(scrollGO.transform, false);
        var contentRT = contentPanel.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        var layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentPanel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
    }

    private void ClearContent()
    {
        for (int i = contentPanel.transform.childCount - 1; i >= 0; i--)
            Destroy(contentPanel.transform.GetChild(i).gameObject);
    }

    private void CreateHeader(string text)
    {
        var go = new GameObject("Header");
        go.transform.SetParent(contentPanel.transform, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = GetFont();
        t.fontSize = 18;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 30;
        le.preferredHeight = 30;
    }

    private void CreateSubLabel(string text)
    {
        var go = new GameObject("Sub");
        go.transform.SetParent(contentPanel.transform, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = GetFont();
        t.fontSize = 12;
        t.color = new Color(0.6f, 0.6f, 0.6f);
        t.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 20;
        le.preferredHeight = 20;
    }

    private void CreateSeparator()
    {
        var go = new GameObject("Sep");
        go.transform.SetParent(contentPanel.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f);
        img.raycastTarget = false;
        go.AddComponent<LayoutElement>().minHeight = 2;
    }

    private void CreateActionButton(string label, Action onClick)
    {
        var go = new GameObject("ActionBtn");
        go.transform.SetParent(contentPanel.transform, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 34);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.3f, 0.42f);
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 34;
        le.preferredHeight = 34;

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
    }
}

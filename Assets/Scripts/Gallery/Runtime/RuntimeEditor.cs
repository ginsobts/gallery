using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RuntimeEditor : MonoBehaviour
{
    [Header("编辑器设置")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private KeyCode deleteKey = KeyCode.Delete;

    public static RuntimeEditor Instance { get; private set; }
    public bool IsEditing { get; private set; }
    public bool IsMouseMode { get; private set; }

    private Canvas editorCanvas;
    private GameObject toolbarPanel;
    private GameObject editorOverlay;
    private GameObject topBar;
    private GameObject bottomBar;
    private Text statusText;
    private Image mouseBtnImage;

    private GameObject selectedObject;
    private ElementData selectedElementData;
    private bool selectedIsBackground;
    private Camera cam;

    private SceneData currentScene;
    private string currentSceneName;
    private Dictionary<GameObject, ElementData> goToData = new Dictionary<GameObject, ElementData>();

    private RuntimeElementHandle elementHandle;
    private RuntimeSettingsPanel settingsPanel;
    private RuntimeSceneSettingsPanel sceneSettingsPanel;
    private RuntimeBlockSettingsPanel blockSettingsPanel;
    private List<GameObject> idLabels = new List<GameObject>();

    public SceneData CurrentScene => currentScene;
    public string CurrentSceneName => currentSceneName;
    public Canvas EditorCanvas => editorCanvas;
    public GameObject SelectedObject => selectedObject;
    public ElementData SelectedElementData => selectedElementData;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        cam = Camera.main;
    }

    private void Start()
    {
        CreateEditorUI();
        elementHandle = gameObject.AddComponent<RuntimeElementHandle>();
        settingsPanel = gameObject.AddComponent<RuntimeSettingsPanel>();
        sceneSettingsPanel = gameObject.AddComponent<RuntimeSceneSettingsPanel>();
        blockSettingsPanel = gameObject.AddComponent<RuntimeBlockSettingsPanel>();
        SetEditorActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleEditor();

        if (!IsEditing) return;

        UpdateEditorBars();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleEditor();
            return;
        }

        if (settingsPanel != null && settingsPanel.IsOpen) return;
        if (sceneSettingsPanel != null && (sceneSettingsPanel.IsOpen || sceneSettingsPanel.IsInPickMode)) return;
        if (blockSettingsPanel != null && blockSettingsPanel.IsOpen) return;

        if (IsMouseMode)
            HandleSelection();

        if (selectedObject != null && Input.GetKeyDown(deleteKey))
            DeleteSelected();
    }

    public void ToggleEditor()
    {
        IsEditing = !IsEditing;
        SetEditorActive(IsEditing);
        if (IsEditing)
        {
            unfreezeGuardFrames = 0;
            GalleryPlayer.Freeze();
            LoadCurrentSceneData();
            CreateIdLabels();
        }
        else
        {
            try
            {
                SaveScene();
                Deselect();
                DestroyIdLabels();
                if (settingsPanel != null) settingsPanel.Close();
                if (sceneSettingsPanel != null) sceneSettingsPanel.Close();
                if (blockSettingsPanel != null) blockSettingsPanel.Close();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[RuntimeEditor] Error closing editor: " + e);
            }
            finally
            {
                GalleryPlayer.ForceUnfreeze();
                unfreezeGuardFrames = 5;
                Debug.Log("[RuntimeEditor] Editor closed, freezeCount forced to 0");
            }
        }
    }

    private int unfreezeGuardFrames;

    private void LateUpdate()
    {
        if (unfreezeGuardFrames > 0)
        {
            GalleryPlayer.ForceUnfreeze();
            unfreezeGuardFrames--;
        }
    }

    // ── Scene Data ──

    public void LoadSceneForEditing(string sceneName)
    {
        currentSceneName = sceneName;
        string path = SceneDataHelper.GetSceneJsonPath(sceneName);
        currentScene = SceneData.Load(path);
        if (currentScene == null)
        {
            currentScene = new SceneData { sceneName = sceneName };
            currentScene.Save(path);
        }
        RebuildGoMapping();
    }

    private void LoadCurrentSceneData()
    {
        var builder = RuntimeSceneBuilder.Instance;
        if (builder != null && !string.IsNullOrEmpty(builder.CurrentSceneName))
        {
            currentSceneName = builder.CurrentSceneName;
            currentScene = SceneData.Load(SceneDataHelper.GetSceneJsonPath(currentSceneName));
            RebuildGoMapping();
        }

        if (currentScene == null)
        {
            currentSceneName = "default";
            string path = SceneDataHelper.GetSceneJsonPath(currentSceneName);
            currentScene = SceneData.Load(path);
            if (currentScene == null)
            {
                currentScene = new SceneData { sceneName = currentSceneName };
                currentScene.Save(path);
            }
            if (builder != null) builder.LoadScene(currentSceneName);
            RebuildGoMapping();
        }
    }

    public void RebuildGoMapping()
    {
        goToData.Clear();
        if (currentScene == null) return;
        var builder = RuntimeSceneBuilder.Instance;
        if (builder == null) return;
        foreach (var elem in currentScene.elements)
        {
            if (builder.ElementMap.TryGetValue(elem.id, out var go))
                goToData[go] = elem;
        }
    }

    public ElementData GetElementData(GameObject go)
    {
        return goToData.TryGetValue(go, out var d) ? d : null;
    }

    // ── Element Operations ──

    public void RenameElementId(ElementData elem, string newId)
    {
        if (elem == null || string.IsNullOrEmpty(newId)) return;
        string oldId = elem.id;

        var builder = RuntimeSceneBuilder.Instance;
        if (builder != null && builder.ElementMap.ContainsKey(oldId))
        {
            var go = builder.ElementMap[oldId];
            builder.ElementMap.Remove(oldId);
            builder.ElementMap[newId] = go;
        }

        elem.id = newId;
        SaveScene();
        DestroyIdLabels();
        CreateIdLabels();
        SetStatus($"ID 已修改: {oldId} -> {newId}");
    }

    public void AddElement(string type)
    {
        if (currentScene == null) { SetStatus("请先创建场景"); return; }

        Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
        pos.z = 0;

        ElementData elem = ElementData.CreateNew(type);
        elem.x = pos.x;
        elem.y = pos.y;
        currentScene.elements.Add(elem);

        var builder = RuntimeSceneBuilder.Instance;
        if (builder != null)
        {
            GameObject go = builder.SpawnAndRegister(elem);
            if (go != null) goToData[go] = elem;
        }
        SaveScene();
        SetStatus("已添加 " + type);

        var newGo = builder?.ElementMap.ContainsKey(elem.id) == true ? builder.ElementMap[elem.id] : null;
        if (newGo != null) Select(newGo);
    }

    public void DuplicateSelected()
    {
        if (selectedObject == null || currentScene == null) return;
        if (!goToData.TryGetValue(selectedObject, out var srcElem)) return;

        string json = JsonUtility.ToJson(srcElem);
        ElementData clone = JsonUtility.FromJson<ElementData>(json);
        clone.id = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        clone.x += 1.5f;
        clone.y += -1f;

        currentScene.elements.Add(clone);

        var builder = RuntimeSceneBuilder.Instance;
        if (builder != null)
        {
            GameObject go = builder.SpawnAndRegister(clone);
            if (go != null) goToData[go] = clone;
        }
        SaveScene();
        CreateIdLabels();
        SetStatus("已复制: " + clone.id);

        var newGo = builder?.ElementMap.ContainsKey(clone.id) == true ? builder.ElementMap[clone.id] : null;
        if (newGo != null) Select(newGo);
    }

    public void DeleteSelected()
    {
        if (selectedObject == null) return;
        if (selectedIsBackground)
        {
            SetStatus("背景图片不能通过Delete删除，请在场景布局中修改");
            return;
        }
        if (goToData.TryGetValue(selectedObject, out var elem))
        {
            currentScene.elements.Remove(elem);
            SaveScene();
        }
        goToData.Remove(selectedObject);
        var builder = RuntimeSceneBuilder.Instance;
        if (builder != null && elem != null)
            builder.RemoveElement(elem.id);
        else
            Destroy(selectedObject);
        Deselect();
        SetStatus("已删除");
    }

    public void SaveScene()
    {
        if (currentScene == null || string.IsNullOrEmpty(currentSceneName)) return;
        SyncAllPositions();
        currentScene.Save(SceneDataHelper.GetSceneJsonPath(currentSceneName));
        PlayerPrefs.SetString("Gallery_LastScene", currentSceneName);
        PlayerPrefs.Save();
    }

    public void SyncAllPositions()
    {
        foreach (var kv in goToData)
        {
            if (kv.Key == null) continue;
            kv.Value.x = kv.Key.transform.position.x;
            kv.Value.y = kv.Key.transform.position.y;
            kv.Value.scaleX = kv.Key.transform.localScale.x;
            kv.Value.scaleY = kv.Key.transform.localScale.y;
        }
        SyncBackgroundTransform();
    }

    private void SyncBackgroundTransform()
    {
        if (currentScene == null || currentScene.settings == null) return;
        var builder = RuntimeSceneBuilder.Instance;
        if (builder == null || builder.BackgroundGO == null) return;
        var bg = builder.BackgroundGO;
        currentScene.settings.backgroundX = bg.transform.position.x;
        currentScene.settings.backgroundY = bg.transform.position.y;
        currentScene.settings.backgroundScaleX = bg.transform.localScale.x;
        currentScene.settings.backgroundScaleY = bg.transform.localScale.y;
    }

    public void RebuildElement(ElementData elem)
    {
        if (elem == null) return;
        var builder = RuntimeSceneBuilder.Instance;
        if (builder == null) return;

        builder.CurrentSceneName = currentSceneName;

        if (builder.ElementMap.TryGetValue(elem.id, out var oldGo))
        {
            goToData.Remove(oldGo);
            builder.RemoveElement(elem.id);
        }

        GameObject newGo = builder.SpawnAndRegister(elem);
        if (newGo != null)
        {
            goToData[newGo] = elem;
            if (selectedElementData == elem) Select(newGo);
        }
        SaveScene();
    }

    public void MoveSelected(Vector3 worldDelta)
    {
        if (selectedObject == null) return;
        selectedObject.transform.position += worldDelta;
    }

    public void ScaleSelected(float dx, float dy)
    {
        if (selectedObject == null) return;
        Vector3 s = selectedObject.transform.localScale;
        s.x = Mathf.Max(0.1f, s.x + dx);
        s.y = Mathf.Max(0.1f, s.y + dy);
        selectedObject.transform.localScale = s;
    }

    public void AssignMediaToSelected(string absolutePath)
    {
        if (selectedElementData == null || string.IsNullOrEmpty(absolutePath)) return;
        string rel = RuntimeAssetLoader.Instance.CopyMediaToScene(absolutePath, currentSceneName);
        if (string.IsNullOrEmpty(rel)) { SetStatus("文件复制失败"); return; }
        Debug.Log($"[RuntimeEditor] AssignMedia: src={absolutePath} rel={rel} scene={currentSceneName}");
        selectedElementData.mediaFile = rel;
        selectedElementData.mediaFitted = false;

        string newFullPath = System.IO.Path.Combine(
            SceneDataHelper.GetScenePath(currentSceneName), rel);
        RuntimeAssetLoader.Instance.InvalidateCache(newFullPath);

        RebuildElement(selectedElementData);
        SetStatus("已更换文件: " + rel);
    }

    // ── Selection ──

    private void HandleSelection()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (IsPointerOverUI()) return;
        if (cam == null) return;

        Vector2 wp = cam.ScreenToWorldPoint(Input.mousePosition);

        GameObject closest = null;
        float closestSqrDist = float.MaxValue;

        foreach (var kv in goToData)
        {
            if (kv.Key == null) continue;
            Bounds bounds = GetElementBounds(kv.Key);
            if (bounds.Contains(new Vector3(wp.x, wp.y, bounds.center.z)))
            {
                float sqrDist = ((Vector2)bounds.center - wp).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closest = kv.Key;
                }
            }
        }

        if (closest == null)
        {
            var builder = RuntimeSceneBuilder.Instance;
            if (builder != null && builder.BackgroundGO != null)
            {
                Bounds bgBounds = GetElementBounds(builder.BackgroundGO);
                if (bgBounds.Contains(new Vector3(wp.x, wp.y, bgBounds.center.z)))
                    closest = builder.BackgroundGO;
            }
        }

        if (closest != null)
        {
            if (closest != selectedObject)
                Select(closest);
            if (elementHandle != null)
                elementHandle.StartMoveFromWorld(new Vector3(wp.x, wp.y, 0));
        }
        else
        {
            Deselect();
        }
    }

    private Bounds GetElementBounds(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) return sr.bounds;
        var col = go.GetComponent<BoxCollider2D>();
        if (col != null)
            return new Bounds(go.transform.position, new Vector3(col.size.x, col.size.y, 0.1f));
        Vector3 pos = go.transform.position;
        Vector3 s = go.transform.localScale;
        float minSize = 1f;
        return new Bounds(pos, new Vector3(
            Mathf.Max(Mathf.Abs(s.x), minSize),
            Mathf.Max(Mathf.Abs(s.y), minSize), 0.1f));
    }

    public void Select(GameObject go)
    {
        Deselect();
        selectedObject = go;

        var builder = RuntimeSceneBuilder.Instance;
        selectedIsBackground = builder != null && go == builder.BackgroundGO;

        if (!selectedIsBackground)
            goToData.TryGetValue(go, out selectedElementData);

        if (elementHandle != null) elementHandle.Attach(go, selectedElementData);

        if (selectedIsBackground)
            SetStatus("已选中背景图片 (拖拽移动/缩放)");
    }

    public void Deselect()
    {
        if (selectedIsBackground)
            SyncBackgroundTransform();
        selectedObject = null;
        selectedElementData = null;
        selectedIsBackground = false;
        if (elementHandle != null) elementHandle.Detach();
    }

    // ── UI ──

    private void CreateEditorUI()
    {
        editorCanvas = RuntimeUIHelper.CreateCanvas("EditorCanvas", transform, 32000);

        editorOverlay = new GameObject("Overlay");
        editorOverlay.transform.SetParent(editorCanvas.transform, false);
        var ort = editorOverlay.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        var oimg = editorOverlay.AddComponent<Image>();
        oimg.color = new Color(0, 0, 0, 0.12f);
        oimg.raycastTarget = false;

        CreateEditorBars();
        CreateToolbar();
        CreateStatusBar();
        CreateTutorialButton();
    }

    private Image topBarImage;
    private Image bottomBarImage;
    private Text topBarText;
    private Text bottomBarText;

    private void CreateEditorBars()
    {
        float barHeight = 30f;

        topBar = new GameObject("TopBar");
        topBar.transform.SetParent(editorCanvas.transform, false);
        var trt = topBar.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(0, barHeight);
        topBarImage = topBar.AddComponent<Image>();
        topBarImage.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);

        var topStripe = CreateStripeOverlay(topBar.transform, barHeight);

        var topTextGO = new GameObject("Text");
        topTextGO.transform.SetParent(topBar.transform, false);
        var topTextRT = topTextGO.AddComponent<RectTransform>();
        topTextRT.anchorMin = Vector2.zero; topTextRT.anchorMax = Vector2.one;
        topTextRT.offsetMin = Vector2.zero; topTextRT.offsetMax = Vector2.zero;
        topBarText = topTextGO.AddComponent<Text>();
        topBarText.text = "\u25c6  编辑模式  \u25c6    Tab / Esc 退出";
        topBarText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        topBarText.fontSize = 13;
        topBarText.alignment = TextAnchor.MiddleCenter;
        topBarText.color = new Color(1f, 0.95f, 0.7f);

        bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(editorCanvas.transform, false);
        var brt = bottomBar.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
        brt.pivot = new Vector2(0.5f, 0);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(0, barHeight);
        bottomBarImage = bottomBar.AddComponent<Image>();
        bottomBarImage.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);

        CreateStripeOverlay(bottomBar.transform, barHeight);

        var botTextGO = new GameObject("Text");
        botTextGO.transform.SetParent(bottomBar.transform, false);
        var botTextRT = botTextGO.AddComponent<RectTransform>();
        botTextRT.anchorMin = Vector2.zero; botTextRT.anchorMax = Vector2.one;
        botTextRT.offsetMin = Vector2.zero; botTextRT.offsetMax = Vector2.zero;
        bottomBarText = botTextGO.AddComponent<Text>();
        bottomBarText.text = "\u25c6  编辑模式  \u25c6    点击选中元素 | Del 删除";
        bottomBarText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bottomBarText.fontSize = 13;
        bottomBarText.alignment = TextAnchor.MiddleCenter;
        bottomBarText.color = new Color(1f, 0.95f, 0.7f);
    }

    private GameObject CreateStripeOverlay(Transform parent, float height)
    {
        var go = new GameObject("Stripes");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var rawImg = go.AddComponent<RawImage>();
        rawImg.color = new Color(1f, 1f, 1f, 0.08f);
        rawImg.raycastTarget = false;

        int texW = 64, texH = (int)height;
        var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                float diag = (x + y) % 16;
                float a = diag < 8 ? 0.6f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        rawImg.texture = tex;
        rawImg.uvRect = new Rect(0, 0, 12f, 1f);

        go.AddComponent<EditorBarStripeScroller>();
        return go;
    }

    private void UpdateEditorBars()
    {
        if (topBarText == null) return;
        float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 2.5f) * 0.15f;
        Color textCol = new Color(1f, 0.95f, 0.7f, pulse);
        topBarText.color = textCol;
        bottomBarText.color = textCol;
    }

    private void CreateToolbar()
    {
        toolbarPanel = new GameObject("Toolbar");
        toolbarPanel.transform.SetParent(editorCanvas.transform, false);
        var rt = toolbarPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -10);
        rt.sizeDelta = new Vector2(170, 0);
        toolbarPanel.AddComponent<Image>().color = RuntimeUIHelper.PanelBG;
        var csf = toolbarPanel.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = toolbarPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4; vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        RuntimeUIHelper.Label(toolbarPanel.transform, "编辑器", 14, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;

        var mouseBtn = RuntimeUIHelper.Btn(toolbarPanel.transform, "鼠标 (选择/拖拽)", () => ToggleMouseMode());
        mouseBtnImage = mouseBtn.gameObject.GetComponent<Image>();
        IsMouseMode = true;
        UpdateMouseBtnColor();

        RuntimeUIHelper.Spacer(toolbarPanel.transform, 4);
        RuntimeUIHelper.Btn(toolbarPanel.transform, "+ 照片", () => AddElement("photo"));
        RuntimeUIHelper.Btn(toolbarPanel.transform, "+ 视频", () => AddElement("video"));
        RuntimeUIHelper.Btn(toolbarPanel.transform, "+ NPC", () => AddElement("npc_dialogue"));
        RuntimeUIHelper.Btn(toolbarPanel.transform, "+ 天气", () => AddElement("weather"));
        RuntimeUIHelper.Spacer(toolbarPanel.transform, 4);
        RuntimeUIHelper.Btn(toolbarPanel.transform, "场景布局", () => OpenSceneSettings());
        RuntimeUIHelper.Btn(toolbarPanel.transform, "当前区块设置", () => OpenBlockSettings());
        RuntimeUIHelper.Spacer(toolbarPanel.transform, 4);
        RuntimeUIHelper.Btn(toolbarPanel.transform, "保存", () => { SaveScene(); SetStatus("已保存"); }, RuntimeUIHelper.AccentGreen);
        RuntimeUIHelper.Btn(toolbarPanel.transform, "场景列表", () =>
        {
            if (RuntimeSceneBrowser.Instance != null) RuntimeSceneBrowser.Instance.Open();
        });
    }

    private void CreateStatusBar()
    {
        var go = new GameObject("StatusBar");
        go.transform.SetParent(editorCanvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.25f, 0); rt.anchorMax = new Vector2(0.75f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 8);
        rt.sizeDelta = new Vector2(0, 30);
        go.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        statusText = RuntimeUIHelper.Label(go.transform, "", 12, TextAnchor.MiddleCenter);
        var trt = statusText.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        Object.Destroy(statusText.GetComponent<LayoutElement>());
    }

    private void SetEditorActive(bool active)
    {
        if (editorCanvas != null) editorCanvas.gameObject.SetActive(active);
        if (!active && elementHandle != null) elementHandle.Detach();
    }

    public void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    // ── Tutorial ──

    private Canvas tutorialCanvas;
    private GameObject tutorialPanel;

    private void CreateTutorialButton()
    {
        tutorialCanvas = RuntimeUIHelper.CreateCanvas("TutorialCanvas", transform, 31000);

        var btnGO = new GameObject("TutorialBtn");
        btnGO.transform.SetParent(tutorialCanvas.transform, false);
        var brt = btnGO.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 0);
        brt.anchorMax = new Vector2(0, 0);
        brt.pivot = new Vector2(0, 0);
        brt.anchoredPosition = new Vector2(12, 12);
        brt.sizeDelta = new Vector2(80, 32);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.4f, 0.6f, 0.9f);
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(ShowTutorial);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        var trt = txtGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var t = txtGO.AddComponent<Text>();
        t.text = "教学";
        t.font = RuntimeUIHelper.GetFont();
        t.fontSize = 15;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
    }

    private void ShowTutorial()
    {
        if (tutorialPanel != null) { Destroy(tutorialPanel); return; }

        tutorialPanel = new GameObject("TutorialPanel");
        tutorialPanel.transform.SetParent(tutorialCanvas.transform, false);
        var rt = tutorialPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bgBtn = tutorialPanel.AddComponent<Button>();
        var bgImg = tutorialPanel.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);
        bgBtn.targetGraphic = bgImg;
        bgBtn.navigation = new Navigation { mode = Navigation.Mode.None };
        bgBtn.onClick.AddListener(CloseTutorial);

        var box = new GameObject("Box");
        box.transform.SetParent(tutorialPanel.transform, false);
        var boxRT = box.AddComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.08f, 0.08f);
        boxRT.anchorMax = new Vector2(0.92f, 0.92f);
        boxRT.offsetMin = Vector2.zero; boxRT.offsetMax = Vector2.zero;
        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.1f, 0.1f, 0.14f, 1f);
        boxImg.raycastTarget = true;

        Transform contentParent;
        RuntimeUIHelper.ScrollPanel(box.transform, out contentParent);

        string[] lines = GetTutorialLines();
        foreach (var line in lines)
        {
            if (line.StartsWith("##"))
            {
                var lbl = RuntimeUIHelper.Label(contentParent, line.Substring(2).Trim(), 15, TextAnchor.MiddleLeft);
                lbl.fontStyle = FontStyle.Bold;
                lbl.color = new Color(0.4f, 0.8f, 1f);
            }
            else if (string.IsNullOrEmpty(line))
            {
                RuntimeUIHelper.Spacer(contentParent, 8);
            }
            else
            {
                RuntimeUIHelper.Label(contentParent, line, 13);
            }
        }
        RuntimeUIHelper.Spacer(contentParent, 20);
    }

    private void CloseTutorial()
    {
        if (tutorialPanel != null) { Destroy(tutorialPanel); tutorialPanel = null; }
    }

    private string[] GetTutorialLines()
    {
        return new[]
        {
            "## 基本操作",
            "按 Tab 或 Esc 键打开/关闭编辑器",
            "编辑器打开时玩家不会移动",
            "",
            "## 鼠标模式",
            "点击左侧「鼠标 (选择/拖拽)」按钮开启",
            "开启后点击场景中的元素即可选中",
            "选中后元素上方出现操作栏，下方出现拖拽栏",
            "",
            "## 操作栏按钮",
            "选择文件 — 为元素选择图片/视频文件",
            "设置交互 — 打开右侧属性面板",
            "▲ 顶层 — 将元素移到所有元素上方",
            "▼ 底层 — 将元素移到所有元素下方",
            "删除 — 删除该元素",
            "",
            "## 拖拽栏",
            "移动 — 按住拖动可移动元素位置",
            "横向缩放 — 左右拖动改变宽度",
            "纵向缩放 — 上下拖动改变高度",
            "等比缩放 — 拖动等比例缩放",
            "",
            "## 属性面板 (设置交互)",
            "ID — 可自定义元素ID（用于交互引用）",
            "变换 — 精确设置位置、缩放、排序层",
            "有碰撞体 — 控制该元素是否阻挡玩家",
            "启用按键交互 — 玩家靠近按键触发效果",
            "启用靠近触发 — 玩家走近自动触发效果",
            "",
            "## 交互效果类型",
            "缩放查看 / 显示文字 / 播放音效",
            "切换BGM / 改变天气 / 改变背景",
            "改变亮度 / 加载场景 / 开关物体",
            "",
            "## 工具栏其他功能",
            "+ 照片/视频/NPC — 添加新元素到场景中心",
            "设置背景图片 — 选择一张背景图",
            "保存 — 保存当前编辑（退出时也会自动保存）",
            "场景列表 — 切换/创建/删除场景",
            "",
            "## 快捷键",
            "Tab / Esc — 开启/关闭编辑器",
            "Delete — 删除选中元素",
            "",
            "## 其他说明",
            "每个元素下方的黄色文字是其ID",
            "可在「开关物体」交互中填入目标ID来控制其他元素",
            "所有数据保存在本地，关闭游戏后再打开仍保留",
            "",
            "点击灰色区域关闭本教学",
        };
    }

    // ── ID Labels ──

    private void CreateIdLabels()
    {
        DestroyIdLabels();
        foreach (var kv in goToData)
        {
            if (kv.Key == null) continue;
            var labelGO = new GameObject("IDLabel_" + kv.Value.id);
            labelGO.transform.SetParent(kv.Key.transform, false);

            var sr = kv.Key.GetComponent<SpriteRenderer>();
            float offsetY = -1f;
            if (sr != null && sr.sprite != null)
                offsetY = -(sr.sprite.bounds.extents.y * kv.Key.transform.localScale.y + 0.3f) / kv.Key.transform.localScale.y;

            labelGO.transform.localPosition = new Vector3(0, offsetY, 0);

            var textMesh = labelGO.AddComponent<TextMesh>();
            string typePrefix;
            switch (kv.Value.type)
            {
                case "photo": typePrefix = "图片"; break;
                case "video": typePrefix = "视频"; break;
                case "npc_dialogue": typePrefix = "NPC"; break;
                case "weather": typePrefix = "天气"; break;
                default: typePrefix = kv.Value.type; break;
            }
            textMesh.text = typePrefix + " id: " + kv.Value.id;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.08f;
            textMesh.anchor = TextAnchor.UpperCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(1f, 0.9f, 0.3f, 0.9f);

            var meshRenderer = labelGO.GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.sortingOrder = 30000;

            idLabels.Add(labelGO);
        }
    }

    private void DestroyIdLabels()
    {
        foreach (var lbl in idLabels)
            if (lbl != null) Destroy(lbl);
        idLabels.Clear();
    }

    private void ToggleMouseMode()
    {
        IsMouseMode = !IsMouseMode;
        UpdateMouseBtnColor();
        if (IsMouseMode)
            SetStatus("鼠标模式: 点击选中元素，拖拽移动/缩放");
        else
        {
            Deselect();
            SetStatus("鼠标模式已关闭");
        }
    }

    private void UpdateMouseBtnColor()
    {
        if (mouseBtnImage == null) return;
        mouseBtnImage.color = IsMouseMode
            ? new Color(0.3f, 0.75f, 0.4f)
            : RuntimeUIHelper.BtnNormal;
    }

    private void OpenSceneSettings()
    {
        if (currentScene == null) { SetStatus("请先创建场景"); return; }
        if (blockSettingsPanel != null && blockSettingsPanel.IsOpen) blockSettingsPanel.Close();
        if (sceneSettingsPanel != null)
        {
            if (sceneSettingsPanel.IsOpen) { sceneSettingsPanel.Close(); return; }
            sceneSettingsPanel.Open(currentScene.settings);
        }
    }

    private void OpenBlockSettings()
    {
        if (currentScene == null) { SetStatus("请先创建场景"); return; }
        if (sceneSettingsPanel != null && sceneSettingsPanel.IsOpen) sceneSettingsPanel.Close();
        if (blockSettingsPanel != null)
        {
            if (blockSettingsPanel.IsOpen) { blockSettingsPanel.Close(); return; }
            blockSettingsPanel.Open(currentScene.settings);
        }
    }

    private void PickBackgroundImage()
    {
        if (currentScene == null) { SetStatus("请先创建场景"); return; }
        string path = NativeFilePicker.PickImageFile("选择背景图片");
        if (string.IsNullOrEmpty(path)) return;

        string rel = RuntimeAssetLoader.Instance.CopyMediaToScene(path, currentSceneName);
        if (string.IsNullOrEmpty(rel)) { SetStatus("文件复制失败"); return; }

        string fullPath = System.IO.Path.Combine(SceneDataHelper.GetScenePath(currentSceneName), rel);
        RuntimeAssetLoader.Instance.InvalidateCache(fullPath);

        currentScene.settings.backgroundMediaFile = rel;
        FitBackgroundToSprite(currentScene.settings);
        var builder = RuntimeSceneBuilder.Instance;
        if (builder != null) builder.ApplyBackgroundImage(currentScene.settings);
        SaveScene();
        SetStatus("背景已设置: " + rel);
    }

    private void FitBackgroundToSprite(SceneSettingsData s)
    {
        if (string.IsNullOrEmpty(s.backgroundMediaFile)) return;
        Sprite sprite = RuntimeAssetLoader.Instance.LoadSpriteFromScene(currentSceneName, s.backgroundMediaFile);
        if (sprite == null) return;

        float spriteW = sprite.bounds.size.x;
        float spriteH = sprite.bounds.size.y;
        if (spriteW <= 0 || spriteH <= 0) return;

        float oldArea = Mathf.Abs(s.backgroundScaleX * s.backgroundScaleY);
        if (oldArea <= 0) oldArea = 20f * 12f;
        float uniformScale = Mathf.Sqrt(oldArea / (spriteW * spriteH));
        s.backgroundScaleX = uniformScale;
        s.backgroundScaleY = uniformScale;
    }

    public void MoveElementToTop(ElementData elem)
    {
        if (currentScene == null || elem == null) return;
        int maxOrder = 0;
        foreach (var e in currentScene.elements)
            if (e.sortingOrder > maxOrder) maxOrder = e.sortingOrder;
        elem.sortingOrder = maxOrder + 1;
        RebuildElement(elem);
        SetStatus("已移到最上层 (层级 " + elem.sortingOrder + ")");
    }

    public void MoveElementToBottom(ElementData elem)
    {
        if (currentScene == null || elem == null) return;
        int minOrder = 0;
        foreach (var e in currentScene.elements)
            if (e.sortingOrder < minOrder) minOrder = e.sortingOrder;
        elem.sortingOrder = minOrder - 1;
        RebuildElement(elem);
        SetStatus("已移到最下层 (层级 " + elem.sortingOrder + ")");
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}

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
    private Text statusText;
    private Image mouseBtnImage;

    private GameObject selectedObject;
    private ElementData selectedElementData;
    private Camera cam;

    private SceneData currentScene;
    private string currentSceneName;
    private Dictionary<GameObject, ElementData> goToData = new Dictionary<GameObject, ElementData>();

    private RuntimeElementHandle elementHandle;
    private RuntimeSettingsPanel settingsPanel;
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
        SetEditorActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleEditor();

        if (!IsEditing) return;
        if (settingsPanel != null && settingsPanel.IsOpen) return;

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
            GalleryPlayer.Freeze();
            LoadCurrentSceneData();
            CreateIdLabels();
        }
        else
        {
            SaveScene();
            Deselect();
            DestroyIdLabels();
            if (settingsPanel != null) settingsPanel.Close();
            GalleryPlayer.ForceUnfreeze();
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

    public void DeleteSelected()
    {
        if (selectedObject == null) return;
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
        float closestDist = float.MaxValue;

        foreach (var kv in goToData)
        {
            if (kv.Key == null) continue;
            Bounds bounds = GetElementBounds(kv.Key);
            if (bounds.Contains(new Vector3(wp.x, wp.y, bounds.center.z)))
            {
                float dist = Vector2.Distance(wp, bounds.center);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = kv.Key;
                }
            }
        }

        if (closest != null)
            Select(closest);
        else
            Deselect();
    }

    private Bounds GetElementBounds(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) return sr.bounds;
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
        goToData.TryGetValue(go, out selectedElementData);
        if (elementHandle != null) elementHandle.Attach(go, selectedElementData);
    }

    public void Deselect()
    {
        selectedObject = null;
        selectedElementData = null;
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

        CreateToolbar();
        CreateStatusBar();
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
        RuntimeUIHelper.Btn(toolbarPanel.transform, "+ NPC对话", () => AddElement("npc_dialogue"));
        RuntimeUIHelper.Btn(toolbarPanel.transform, "+ NPC跟随", () => AddElement("npc_follower"));
        RuntimeUIHelper.Btn(toolbarPanel.transform, "+ 天气", () => AddElement("weather"));
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
        statusText = RuntimeUIHelper.Label(go.transform, "Tab 退出 | 点击选中 | Del 删除", 12, TextAnchor.MiddleCenter);
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
            textMesh.text = kv.Value.id;
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

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RuntimeSceneSettingsPanel : MonoBehaviour
{
    private Canvas panelCanvas;
    private GameObject panelRoot;
    private RectTransform panelRT;
    private Transform content;

    private SceneSettingsData data;

    private static readonly string[] TransitionNames = { "Cut", "Fade", "Lerp" };

    private enum PickMode { None, CameraOrigin, TimelineNode, TimelineNew, BoundaryPlace, BoundaryEdit }
    private PickMode pickMode = PickMode.None;
    private int pickTargetIndex;
    private bool showAdvancedCamera = false;

    private GameObject boundaryPreviewLine;
    private SpriteRenderer boundaryLineSR;

    private List<BoundaryHandle> editHandles = new List<BoundaryHandle>();
    private int draggingIndex = -1;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public bool IsInPickMode => pickMode != PickMode.None;

    public void Open(SceneSettingsData settings)
    {
        data = settings;
        if (panelRoot == null) CreatePanel();
        panelRoot.SetActive(true);
        pickMode = PickMode.None;
        DestroyBoundaryPreview();
        ClearEditHandles();
        BuildContent();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        pickMode = PickMode.None;
        DestroyBoundaryPreview();
        HideBoundaryGizmos();
        ClearEditHandles();
        ApplyAndSave();
    }

    private void Update()
    {
        if (pickMode == PickMode.None) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (pickMode == PickMode.BoundaryPlace)
                FinishBoundaryPlacement();
            else if (pickMode == PickMode.BoundaryEdit)
                FinishBoundaryEdit();
            else
                CancelPick();
            return;
        }

        if (pickMode == PickMode.BoundaryEdit)
        {
            if (Input.GetMouseButtonDown(1))
            {
                FinishBoundaryEdit();
                return;
            }
            UpdateBoundaryEditDrag();
            return;
        }

        if (pickMode == PickMode.BoundaryPlace)
        {
            UpdateBoundaryPreview();
            if (Input.GetMouseButtonDown(0))
            {
                PlaceBoundary();
            }
            if (Input.GetMouseButtonDown(1))
            {
                FinishBoundaryPlacement();
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var cam = Camera.main;
            if (cam == null) { CancelPick(); return; }
            Vector2 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            OnPickPosition(wp);
        }
    }

    private void OnPickPosition(Vector2 worldPos)
    {
        switch (pickMode)
        {
            case PickMode.CameraOrigin:
                data.cameraFirstBlockX = worldPos.x;
                data.cameraY = worldPos.y;
                break;

            case PickMode.TimelineNode:
                if (data.timelinePoints != null && pickTargetIndex < data.timelinePoints.Length)
                {
                    data.timelinePoints[pickTargetIndex].x = worldPos.x;
                    data.timelinePoints[pickTargetIndex].y = worldPos.y;
                }
                break;

            case PickMode.TimelineNew:
                var pts = data.timelinePoints != null ? new List<TimelinePointData>(data.timelinePoints) : new List<TimelinePointData>();
                pts.Add(new TimelinePointData { x = worldPos.x, y = worldPos.y, dateText = "" });
                data.timelinePoints = pts.ToArray();
                break;
        }

        pickMode = PickMode.None;
        panelRoot.SetActive(true);
        BuildContent();

        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.SetStatus("位置已设定");
    }

    private void CancelPick()
    {
        pickMode = PickMode.None;
        DestroyBoundaryPreview();
        HideBoundaryGizmos();
        panelRoot.SetActive(true);
        BuildContent();
    }

    private void EnterPickMode(PickMode mode, int targetIndex = 0)
    {
        pickMode = mode;
        pickTargetIndex = targetIndex;
        panelRoot.SetActive(false);
        var editor = RuntimeEditor.Instance;
        if (editor != null)
        {
            if (mode == PickMode.BoundaryPlace)
                editor.SetStatus("左键放置分界线 | 右键或Esc结束");
            else
                editor.SetStatus("点击场景中任意位置选取坐标 (Esc取消)");
        }
    }

    // ── Boundary Placement ──

    private void EnterBoundaryPlacementMode()
    {
        pickMode = PickMode.BoundaryPlace;
        panelRoot.SetActive(false);
        CreateBoundaryPreview();
        ShowBoundaryGizmos();
        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.SetStatus("左键放置分界线 | 右键或Esc结束");
    }

    private void CreateBoundaryPreview()
    {
        DestroyBoundaryPreview();
        boundaryPreviewLine = new GameObject("BoundaryPreview");
        boundaryLineSR = boundaryPreviewLine.AddComponent<SpriteRenderer>();
        boundaryLineSR.sprite = RuntimeSprite.Get();
        boundaryLineSR.color = new Color(1f, 1f, 1f, 0.7f);
        boundaryLineSR.sortingOrder = 9999;
        var cam = Camera.main;
        float height = cam != null ? cam.orthographicSize * 2.5f : 20f;
        boundaryPreviewLine.transform.localScale = new Vector3(0.05f, height, 1f);
    }

    private void UpdateBoundaryPreview()
    {
        if (boundaryPreviewLine == null) return;
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        boundaryPreviewLine.transform.position = new Vector3(worldPos.x, cam.transform.position.y, 0f);
    }

    private void PlaceBoundary()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        float bx = worldPos.x;

        var boundaries = data.cameraBoundaries != null
            ? new List<float>(data.cameraBoundaries)
            : new List<float>();
        boundaries.Add(bx);
        boundaries.Sort();
        data.cameraBoundaries = boundaries.ToArray();
        data.cameraBlockCount = boundaries.Count + 1;
        EnsureBlockSettings();

        ShowBoundaryGizmos();

        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.SetStatus("已放置分界线 (共" + boundaries.Count + "条) | 右键结束");
    }

    private void FinishBoundaryPlacement()
    {
        pickMode = PickMode.None;
        DestroyBoundaryPreview();
        HideBoundaryGizmos();
        panelRoot.SetActive(true);

        if (data.cameraBoundaries != null && data.cameraBoundaries.Length > 0)
        {
            data.cameraBlockCount = data.cameraBoundaries.Length + 1;
            EnsureBlockSettings();
        }

        BuildContent();

        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.SetStatus("分界线设置完成，共 " +
            (data.cameraBoundaries != null ? data.cameraBoundaries.Length : 0) + " 条");
    }

    private void DestroyBoundaryPreview()
    {
        if (boundaryPreviewLine != null)
        {
            Destroy(boundaryPreviewLine);
            boundaryPreviewLine = null;
            boundaryLineSR = null;
        }
    }

    // ── Boundary Edit Mode ──

    private class BoundaryHandle
    {
        public int index;
        public GameObject lineGO;
        public GameObject moveHandle;
        public GameObject deleteHandle;
        public SpriteRenderer lineSR;
    }

    private void EnterBoundaryEditMode()
    {
        if (data.cameraBoundaries == null || data.cameraBoundaries.Length == 0) return;
        pickMode = PickMode.BoundaryEdit;
        panelRoot.SetActive(false);
        draggingIndex = -1;
        CreateEditHandles();
        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.SetStatus("拖拽 ◆ 移动分界线 | 点击 ✕ 删除 | 右键/Esc 返回");
    }

    private void CreateEditHandles()
    {
        ClearEditHandles();
        if (data.cameraBoundaries == null) return;
        var cam = Camera.main;
        float height = cam != null ? cam.orthographicSize * 2.5f : 20f;
        float camY = cam != null ? cam.transform.position.y : 0f;
        float handleSize = cam != null ? cam.orthographicSize * 0.12f : 0.6f;

        for (int i = 0; i < data.cameraBoundaries.Length; i++)
        {
            var handle = new BoundaryHandle { index = i };
            float bx = data.cameraBoundaries[i];

            handle.lineGO = new GameObject("BoundaryEditLine_" + i);
            handle.lineSR = handle.lineGO.AddComponent<SpriteRenderer>();
            handle.lineSR.sprite = RuntimeSprite.Get();
            handle.lineSR.color = new Color(0.4f, 0.75f, 1f, 0.5f);
            handle.lineSR.sortingOrder = 9998;
            handle.lineGO.transform.position = new Vector3(bx, camY, 0);
            handle.lineGO.transform.localScale = new Vector3(0.06f, height, 1f);

            handle.moveHandle = CreateWorldButton(bx, camY, handleSize,
                new Color(0.3f, 0.7f, 1f, 0.9f), "\u25C6", i);

            handle.deleteHandle = CreateWorldButton(bx, camY + handleSize * 2f, handleSize * 0.7f,
                new Color(0.9f, 0.25f, 0.25f, 0.9f), "\u2715", i);

            editHandles.Add(handle);
        }
    }

    private GameObject CreateWorldButton(float x, float y, float size, Color color, string symbol, int index)
    {
        var go = new GameObject("BtnHandle_" + symbol + "_" + index);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.color = color;
        sr.sortingOrder = 9999;
        go.transform.position = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(size, size, 1f);

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        textGO.transform.localPosition = Vector3.zero;
        var tm = textGO.AddComponent<TextMesh>();
        tm.text = symbol;
        tm.characterSize = 0.3f;
        tm.fontSize = 40;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        var mr = textGO.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 10000;

        go.AddComponent<BoxCollider2D>().size = Vector2.one;

        return go;
    }

    private void UpdateBoundaryEditDrag()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null)
            {
                for (int i = 0; i < editHandles.Count; i++)
                {
                    if (hit.gameObject == editHandles[i].deleteHandle)
                    {
                        DeleteBoundaryAt(i);
                        return;
                    }
                    if (hit.gameObject == editHandles[i].moveHandle)
                    {
                        draggingIndex = i;
                        return;
                    }
                }
            }
        }

        if (Input.GetMouseButton(0) && draggingIndex >= 0 && draggingIndex < editHandles.Count)
        {
            float newX = mouseWorld.x;
            data.cameraBoundaries[editHandles[draggingIndex].index] = newX;

            var h = editHandles[draggingIndex];
            float camY = cam.transform.position.y;
            float handleSize = cam.orthographicSize * 0.12f;
            h.lineGO.transform.position = new Vector3(newX, camY, 0);
            h.moveHandle.transform.position = new Vector3(newX, camY, 0);
            h.deleteHandle.transform.position = new Vector3(newX, camY + handleSize * 2f, 0);
        }

        if (Input.GetMouseButtonUp(0) && draggingIndex >= 0)
        {
            SortBoundariesAndRefresh();
            draggingIndex = -1;
        }
    }

    private void DeleteBoundaryAt(int handleIndex)
    {
        if (data.cameraBoundaries == null) return;
        int dataIndex = editHandles[handleIndex].index;
        var list = new List<float>(data.cameraBoundaries);
        if (dataIndex >= 0 && dataIndex < list.Count)
            list.RemoveAt(dataIndex);
        data.cameraBoundaries = list.Count > 0 ? list.ToArray() : null;
        data.cameraBlockCount = list.Count + 1;
        EnsureBlockSettings();

        draggingIndex = -1;
        CreateEditHandles();

        var editor = RuntimeEditor.Instance;
        if (editor != null)
        {
            int remaining = data.cameraBoundaries != null ? data.cameraBoundaries.Length : 0;
            editor.SetStatus("已删除分界线 (剩余 " + remaining + " 条)");
        }

        if (data.cameraBoundaries == null || data.cameraBoundaries.Length == 0)
            FinishBoundaryEdit();
    }

    private void SortBoundariesAndRefresh()
    {
        if (data.cameraBoundaries == null || data.cameraBoundaries.Length <= 1) return;
        System.Array.Sort(data.cameraBoundaries);
        CreateEditHandles();
    }

    private void FinishBoundaryEdit()
    {
        pickMode = PickMode.None;
        ClearEditHandles();
        draggingIndex = -1;
        panelRoot.SetActive(true);

        if (data.cameraBoundaries != null && data.cameraBoundaries.Length > 0)
        {
            System.Array.Sort(data.cameraBoundaries);
            data.cameraBlockCount = data.cameraBoundaries.Length + 1;
            EnsureBlockSettings();
        }

        BuildContent();

        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.SetStatus("分界线编辑完成");
    }

    private void ClearEditHandles()
    {
        for (int i = 0; i < editHandles.Count; i++)
        {
            if (editHandles[i].lineGO != null) Destroy(editHandles[i].lineGO);
            if (editHandles[i].moveHandle != null) Destroy(editHandles[i].moveHandle);
            if (editHandles[i].deleteHandle != null) Destroy(editHandles[i].deleteHandle);
        }
        editHandles.Clear();
    }

    private List<GameObject> boundaryGizmoLines = new List<GameObject>();

    private void ShowBoundaryGizmos()
    {
        HideBoundaryGizmos();
        if (data.cameraBoundaries == null) return;
        var cam = Camera.main;
        float height = cam != null ? cam.orthographicSize * 2.5f : 20f;
        float camY = cam != null ? cam.transform.position.y : 0f;

        for (int i = 0; i < data.cameraBoundaries.Length; i++)
        {
            var go = new GameObject("BoundaryGizmo_" + i);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprite.Get();
            sr.color = new Color(0.5f, 0.8f, 1f, 0.4f);
            sr.sortingOrder = 9998;
            go.transform.position = new Vector3(data.cameraBoundaries[i], camY, 0);
            go.transform.localScale = new Vector3(0.04f, height, 1f);
            boundaryGizmoLines.Add(go);
        }
    }

    private void HideBoundaryGizmos()
    {
        for (int i = 0; i < boundaryGizmoLines.Count; i++)
        {
            if (boundaryGizmoLines[i] != null) Destroy(boundaryGizmoLines[i]);
        }
        boundaryGizmoLines.Clear();
    }

    // ── Panel ──

    private void CreatePanel()
    {
        var editor = RuntimeEditor.Instance;
        panelCanvas = editor != null ? editor.EditorCanvas : null;
        if (panelCanvas == null) return;

        panelRoot = new GameObject("SceneLayoutPanel");
        panelRoot.transform.SetParent(panelCanvas.transform, false);
        panelRT = panelRoot.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0);
        panelRT.anchorMax = new Vector2(0.5f, 1);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(420, 0);
        panelRT.offsetMin = new Vector2(panelRT.offsetMin.x, 60);
        panelRT.offsetMax = new Vector2(panelRT.offsetMax.x, -60);
        panelRoot.AddComponent<Image>().color = RuntimeUIHelper.PanelBG;

        RuntimeUIHelper.ScrollPanel(panelRoot.transform, out content);
    }

    private void BuildContent()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        RuntimeUIHelper.Label(content, "场景布局", 16, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
        RuntimeUIHelper.Spacer(content, 4);

        BuildCameraSection();
        RuntimeUIHelper.Spacer(content, 6);
        BuildTransitionSection();
        RuntimeUIHelper.Spacer(content, 6);
        BuildTimelineSection();
        RuntimeUIHelper.Spacer(content, 6);

        RuntimeUIHelper.Btn(content, "预览全部", () => PreviewAll(), RuntimeUIHelper.AccentBlue);
        RuntimeUIHelper.Spacer(content, 2);
        RuntimeUIHelper.Btn(content, "应用并关闭", () => Close(), RuntimeUIHelper.AccentGreen);
        RuntimeUIHelper.Spacer(content, 4);
    }

    // ── Camera ──

    private void BuildCameraSection()
    {
        RuntimeUIHelper.Section(content, "相机区块");

        RuntimeUIHelper.FloatField(content, "过渡速度", data.cameraTransitionSpeed, v => data.cameraTransitionSpeed = Mathf.Max(0.1f, v));
        RuntimeUIHelper.FloatField(content, "相机 Y", data.cameraY, v => data.cameraY = v);

        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.Btn(content, "创建区域分界线", () => EnterBoundaryPlacementMode(), RuntimeUIHelper.AccentBlue);
        RuntimeUIHelper.Spacer(content, 2);

        int boundaryCount = data.cameraBoundaries != null ? data.cameraBoundaries.Length : 0;
        int blockCount = boundaryCount + 1;
        RuntimeUIHelper.ReadOnlyField(content, "分界线数量", boundaryCount.ToString());
        RuntimeUIHelper.ReadOnlyField(content, "区块数量", blockCount.ToString());

        if (boundaryCount > 0)
        {
            RuntimeUIHelper.Spacer(content, 2);
            for (int i = 0; i < data.cameraBoundaries.Length; i++)
            {
                string label = "分界线 " + (i + 1);
                RuntimeUIHelper.ReadOnlyField(content, label, "X = " + data.cameraBoundaries[i].ToString("F1"));
            }
            RuntimeUIHelper.Spacer(content, 2);
            RuntimeUIHelper.Btn(content, "编辑区域分界线", () => EnterBoundaryEditMode(), RuntimeUIHelper.AccentBlue);
        }

        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.ToggleField(content, "显示高级参数", showAdvancedCamera, v =>
        {
            showAdvancedCamera = v;
            BuildContent();
        });

        if (showAdvancedCamera)
        {
            RuntimeUIHelper.IntField(content, "区块数量(手动)", data.cameraBlockCount, v =>
            {
                data.cameraBlockCount = Mathf.Max(1, v);
                EnsureBlockSettings();
                BuildContent();
            });
            RuntimeUIHelper.FloatField(content, "第一区块X", data.cameraFirstBlockX, v => data.cameraFirstBlockX = v);
            RuntimeUIHelper.FloatField(content, "区块宽度(0=自动)", data.cameraBlockWidth, v => data.cameraBlockWidth = Mathf.Max(0, v));
            RuntimeUIHelper.Btn(content, "点击场景设定相机起点", () => EnterPickMode(PickMode.CameraOrigin));
            RuntimeUIHelper.Btn(content, "自动计算区块范围", () => AutoCalculateBlocks());
        }
    }

    public void EnsureBlockSettings()
    {
        int count = Mathf.Max(1, data.cameraBlockCount);
        var list = data.blockSettings != null ? new List<BlockSettingsData>(data.blockSettings) : new List<BlockSettingsData>();
        while (list.Count < count)
        {
            var bs = new BlockSettingsData();
            bs.blockIndex = list.Count;
            bs.ambientBrightness = data.ambientBrightness;
            bs.ambientColor = data.ambientColor != null ? (float[])data.ambientColor.Clone() : new float[] { 0.5f, 0.5f, 0.6f, 1f };
            bs.bgColor = data.backgroundColor != null ? (float[])data.backgroundColor.Clone() : new float[] { 0.05f, 0.05f, 0.1f, 1f };
            list.Add(bs);
        }
        data.blockSettings = list.ToArray();
    }

    private void AutoCalculateBlocks()
    {
        var editor = RuntimeEditor.Instance;
        if (editor == null || editor.CurrentScene == null) return;

        var elements = editor.CurrentScene.elements;
        if (elements.Count == 0)
        {
            editor.SetStatus("场景无元素，无法自动计算");
            return;
        }

        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (var e in elements)
        {
            if (e.x < minX) minX = e.x;
            if (e.x > maxX) maxX = e.x;
        }

        float padding = 5f;
        float totalWidth = maxX - minX + padding * 2f;
        int count = Mathf.Max(1, data.cameraBlockCount);
        float blockW = totalWidth / count;
        float firstX = minX - padding + blockW * 0.5f;

        data.cameraBlockWidth = blockW;
        data.cameraFirstBlockX = firstX;

        editor.SetStatus("已自动计算: 宽度=" + blockW.ToString("F1") + " 起点X=" + firstX.ToString("F1"));
        BuildContent();
    }

    // ── Transitions ──

    private void BuildTransitionSection()
    {
        RuntimeUIHelper.Section(content, "过渡效果");
        RuntimeUIHelper.FloatField(content, "过渡时长(秒)", data.transitionDuration, v => data.transitionDuration = Mathf.Max(0.1f, v));
        BuildTransitionPicker("背景", data.backgroundTransition, v => data.backgroundTransition = v);
        BuildTransitionPicker("灯光", data.lightingTransition, v => data.lightingTransition = v);
        BuildTransitionPicker("天气", data.weatherTransition, v => data.weatherTransition = v);
        BuildTransitionPicker("滤镜", data.filterTransition, v => data.filterTransition = v);
        BuildTransitionPicker("BGM", data.bgmTransition, v => data.bgmTransition = v);
    }

    private void BuildTransitionPicker(string label, int current, System.Action<int> onChange)
    {
        var row = new GameObject("Trans_" + label);
        row.transform.SetParent(content, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4; hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandHeight = true;
        row.AddComponent<LayoutElement>().minHeight = 26;

        var lbl = RuntimeUIHelper.Label(row.transform, label, 12);
        lbl.GetComponent<LayoutElement>().preferredWidth = 50;

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            Color btnColor = (idx == current) ? RuntimeUIHelper.AccentBlue : RuntimeUIHelper.BtnNormal;
            RuntimeUIHelper.Btn(row.transform, TransitionNames[idx], () =>
            {
                onChange(idx);
                BuildContent();
            }, btnColor);
        }
    }

    // ── Timeline ──

    private void BuildTimelineSection()
    {
        RuntimeUIHelper.Section(content, "时间轴");

        RuntimeUIHelper.FloatField(content, "连线宽度", data.timelineLineWidth, v => data.timelineLineWidth = Mathf.Max(0.01f, v));
        RuntimeUIHelper.FloatField(content, "圆点大小", data.timelineDotSize, v => data.timelineDotSize = Mathf.Max(0.05f, v));
        RuntimeUIHelper.FloatField(content, "文字大小", data.timelineTextSize, v => data.timelineTextSize = Mathf.Max(0.01f, v));

        RuntimeUIHelper.Spacer(content, 4);

        var pts = data.timelinePoints != null ? new List<TimelinePointData>(data.timelinePoints) : new List<TimelinePointData>();

        for (int i = 0; i < pts.Count; i++)
        {
            int idx = i;
            var pt = pts[idx];

            string nodeTitle = "节点 " + idx;
            if (!string.IsNullOrEmpty(pt.dateText)) nodeTitle += " (" + pt.dateText + ")";
            RuntimeUIHelper.Section(content, nodeTitle);

            RuntimeUIHelper.TextField(content, "日期文本", pt.dateText, v => { pt.dateText = v; SyncPoints(pts); });
            RuntimeUIHelper.ReadOnlyField(content, "位置", "(" + pt.x.ToString("F1") + ", " + pt.y.ToString("F1") + ")");
            RuntimeUIHelper.Btn(content, "点击场景重新定位", () => EnterPickMode(PickMode.TimelineNode, idx));

            RuntimeUIHelper.Btn(content, "删除此节点", () =>
            {
                pts.RemoveAt(idx);
                SyncPoints(pts);
                BuildContent();
            }, RuntimeUIHelper.AccentRed);
        }

        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.Btn(content, "+ 点击场景添加节点", () => EnterPickMode(PickMode.TimelineNew), RuntimeUIHelper.AccentBlue);
    }

    private void SyncPoints(List<TimelinePointData> pts)
    {
        data.timelinePoints = pts.ToArray();
    }

    // ── Preview & Apply ──

    private void PreviewAll()
    {
        var builder = RuntimeSceneBuilder.Instance;
        if (builder == null) return;
        builder.ApplyCameraSettings(data);
        builder.ApplyTimeline(data);
        builder.ApplyBlockSettingsManager(data);
        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.SetStatus("预览已应用");
    }

    private void ApplyAndSave()
    {
        PreviewAll();
        var editor = RuntimeEditor.Instance;
        if (editor != null)
        {
            editor.SaveScene();
            editor.SetStatus("场景布局已保存");
        }
    }
}

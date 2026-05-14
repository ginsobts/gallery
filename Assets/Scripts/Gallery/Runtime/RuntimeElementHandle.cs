using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RuntimeElementHandle : MonoBehaviour
{
    private Canvas handleCanvas;
    private GameObject root;
    private RectTransform rootRT;
    private GameObject actionBar;
    private Text sizeLabel;

    private GameObject target;
    private ElementData data;
    private Camera cam;

    private CanvasScaler cachedScaler;
    private RectTransform cachedActionBarRT;
    private SpriteRenderer cachedTargetSR;

    private enum DragMode { None, Move, ScaleLeft, ScaleRight, ScaleTop, ScaleBottom, ScaleTL, ScaleTR, ScaleBL, ScaleBR }
    private DragMode dragMode;
    private Vector3 dragStartWorldMouse;
    private Vector3 dragStartScale;
    private Vector3 dragStartPos;

    private GameObject[] cornerHandles = new GameObject[4];
    private GameObject[] edgeHandles = new GameObject[4];
    private GameObject[] borderLines = new GameObject[4];
    private GameObject sizeLabelGO;

    private RectTransform[] cornerRTs = new RectTransform[4];
    private RectTransform[] edgeRTs = new RectTransform[4];
    private RectTransform[] borderRTs = new RectTransform[4];
    private RectTransform sizeLabelRT;

    private Vector3 lastTargetPos;
    private Vector3 lastTargetScale;
    private int lastScreenW, lastScreenH;

    private const float HANDLE_SIZE = 10f;
    private const float BORDER_THICKNESS = 2.5f;

    public void Attach(GameObject go, ElementData elemData)
    {
        target = go;
        data = elemData;
        cam = Camera.main;
        cachedTargetSR = go != null ? go.GetComponent<SpriteRenderer>() : null;
        if (root == null) CreateUI();
        root.SetActive(true);
        UpdatePosition();
        UpdateSizeLabel();
    }

    public void Detach()
    {
        target = null;
        data = null;
        dragMode = DragMode.None;
        if (root != null) root.SetActive(false);
    }

    public bool IsAttached => target != null && root != null && root.activeSelf;

    public bool IsDragging => dragMode != DragMode.None;

    public void StartMoveFromWorld(Vector3 worldPos)
    {
        if (target == null) return;
        dragMode = DragMode.Move;
        dragStartWorldMouse = worldPos;
        dragStartPos = target.transform.position;
    }

    private void LateUpdate()
    {
        if (target == null) { if (root != null) root.SetActive(false); return; }
        if (root == null || !root.activeSelf) return;

        HandleDragInput();

        bool dirty = dragMode != DragMode.None
            || target.transform.position != lastTargetPos
            || target.transform.localScale != lastTargetScale
            || Screen.width != lastScreenW
            || Screen.height != lastScreenH;

        if (dirty)
        {
            UpdatePosition();
            lastTargetPos = target.transform.position;
            lastTargetScale = target.transform.localScale;
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;
        }
    }

    private void HandleDragInput()
    {
        if (dragMode == DragMode.None) return;

        if (Input.GetMouseButton(0))
        {
            if (cam == null) return;
            Vector3 worldNow = cam.ScreenToWorldPoint(Input.mousePosition);
            worldNow.z = 0;

            switch (dragMode)
            {
                case DragMode.Move:
                    target.transform.position = dragStartPos + (worldNow - dragStartWorldMouse);
                    break;
                case DragMode.ScaleLeft:
                    ApplyEdgeScale(worldNow, -1, 0);
                    break;
                case DragMode.ScaleRight:
                    ApplyEdgeScale(worldNow, 1, 0);
                    break;
                case DragMode.ScaleTop:
                    ApplyEdgeScale(worldNow, 0, 1);
                    break;
                case DragMode.ScaleBottom:
                    ApplyEdgeScale(worldNow, 0, -1);
                    break;
                case DragMode.ScaleTL:
                case DragMode.ScaleTR:
                case DragMode.ScaleBL:
                case DragMode.ScaleBR:
                    ApplyCornerScale(worldNow);
                    break;
            }
            UpdateSizeLabel();
        }
        else
        {
            if (dragMode != DragMode.None && target != null && data != null)
            {
                data.x = target.transform.position.x;
                data.y = target.transform.position.y;
                data.scaleX = target.transform.localScale.x;
                data.scaleY = target.transform.localScale.y;
            }
            dragMode = DragMode.None;
        }
    }

    private void ApplyEdgeScale(Vector3 worldNow, int axisX, int axisY)
    {
        Vector3 delta = worldNow - dragStartWorldMouse;
        Vector3 newScale = dragStartScale;

        if (axisX != 0)
        {
            float dx = delta.x * axisX;
            float spriteW = GetSpriteBoundsWidth();
            newScale.x = Mathf.Max(0.1f, dragStartScale.x + dx / spriteW);
        }
        if (axisY != 0)
        {
            float dy = delta.y * axisY;
            float spriteH = GetSpriteBoundsHeight();
            newScale.y = Mathf.Max(0.1f, dragStartScale.y + dy / spriteH);
        }

        target.transform.localScale = newScale;
    }

    private void ApplyCornerScale(Vector3 worldNow)
    {
        Vector3 delta = worldNow - dragStartWorldMouse;
        float spriteW = GetSpriteBoundsWidth();
        float spriteH = GetSpriteBoundsHeight();

        float dx = Mathf.Abs(delta.x) / spriteW;
        float dy = Mathf.Abs(delta.y) / spriteH;
        float avgDelta = (dx + dy) * 0.5f;

        float sign = (delta.x + delta.y) > 0 ? 1f : -1f;
        if (dragMode == DragMode.ScaleTL || dragMode == DragMode.ScaleBR)
            sign = (-delta.x + delta.y) > 0 ? 1f : -1f;

        float factor = avgDelta * sign;
        float newUniform = Mathf.Max(0.1f, dragStartScale.x + factor);
        float ratio = dragStartScale.y / Mathf.Max(0.001f, dragStartScale.x);
        target.transform.localScale = new Vector3(newUniform, newUniform * ratio, 1f);
    }

    private float GetSpriteBoundsWidth()
    {
        if (cachedTargetSR != null && cachedTargetSR.sprite != null)
            return cachedTargetSR.sprite.bounds.size.x;
        return 1f;
    }

    private float GetSpriteBoundsHeight()
    {
        if (cachedTargetSR != null && cachedTargetSR.sprite != null)
            return cachedTargetSR.sprite.bounds.size.y;
        return 1f;
    }

    private void StartDrag(DragMode mode)
    {
        if (target == null || cam == null) return;
        dragMode = mode;
        Vector3 w = cam.ScreenToWorldPoint(Input.mousePosition);
        w.z = 0;
        dragStartWorldMouse = w;
        dragStartScale = target.transform.localScale;
        dragStartPos = target.transform.position;
    }

    private void UpdatePosition()
    {
        if (target == null || cam == null || rootRT == null) return;

        Bounds bounds;
        if (cachedTargetSR != null && cachedTargetSR.sprite != null)
            bounds = cachedTargetSR.bounds;
        else
        {
            Vector3 pos = target.transform.position;
            Vector3 s = target.transform.localScale;
            bounds = new Bounds(pos, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 0));
        }

        Vector2 min = cam.WorldToScreenPoint(bounds.min);
        Vector2 max = cam.WorldToScreenPoint(bounds.max);

        float scaleX = cachedScaler.referenceResolution.x / Screen.width;
        float scaleY = cachedScaler.referenceResolution.y / Screen.height;
        float scale = Mathf.Lerp(scaleX, scaleY, cachedScaler.matchWidthOrHeight);

        float pad = 4f;
        float x0 = min.x * scale - pad;
        float y0 = min.y * scale - pad;
        float x1 = max.x * scale + pad;
        float y1 = max.y * scale + pad;
        float w = x1 - x0;
        float h = y1 - y0;

        rootRT.anchoredPosition = new Vector2(x0, y0);
        rootRT.sizeDelta = new Vector2(w, h);

        cachedActionBarRT.anchoredPosition = new Vector2(w * 0.5f, h + 4);

        cornerRTs[0].anchoredPosition = new Vector2(0, 0);
        cornerRTs[1].anchoredPosition = new Vector2(w, 0);
        cornerRTs[2].anchoredPosition = new Vector2(0, h);
        cornerRTs[3].anchoredPosition = new Vector2(w, h);

        float hw = w * 0.5f;
        float hh = h * 0.5f;
        edgeRTs[0].anchoredPosition = new Vector2(hw, 0);
        edgeRTs[1].anchoredPosition = new Vector2(hw, h);
        edgeRTs[2].anchoredPosition = new Vector2(0, hh);
        edgeRTs[3].anchoredPosition = new Vector2(w, hh);

        borderRTs[0].anchoredPosition = new Vector2(hw, 0);
        borderRTs[0].sizeDelta = new Vector2(w, BORDER_THICKNESS);
        borderRTs[1].anchoredPosition = new Vector2(hw, h);
        borderRTs[1].sizeDelta = new Vector2(w, BORDER_THICKNESS);
        borderRTs[2].anchoredPosition = new Vector2(0, hh);
        borderRTs[2].sizeDelta = new Vector2(BORDER_THICKNESS, h);
        borderRTs[3].anchoredPosition = new Vector2(w, hh);
        borderRTs[3].sizeDelta = new Vector2(BORDER_THICKNESS, h);

        if (sizeLabelRT != null)
            sizeLabelRT.anchoredPosition = new Vector2(hw, -18);
    }

    private string lastSizeText;

    private void UpdateSizeLabel()
    {
        if (sizeLabel == null || target == null) return;
        Vector3 s = target.transform.localScale;
        string newText = s.x.ToString("F2") + " x " + s.y.ToString("F2");
        if (newText != lastSizeText)
        {
            lastSizeText = newText;
            sizeLabel.text = newText;
        }
    }

    // ── UI Creation ──

    private void CreateUI()
    {
        var editor = RuntimeEditor.Instance;
        if (editor == null || editor.EditorCanvas == null) return;
        handleCanvas = editor.EditorCanvas;
        cachedScaler = handleCanvas.GetComponent<CanvasScaler>();

        root = new GameObject("ElementHandle");
        root.transform.SetParent(handleCanvas.transform, false);
        rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.zero;
        rootRT.pivot = Vector2.zero;

        CreateBorderLines();
        CreateCornerHandles();
        CreateEdgeHandles();
        CreateActionBar();
        CreateSizeLabel();

        cachedActionBarRT = actionBar.GetComponent<RectTransform>();
    }

    private void CreateBorderLines()
    {
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Border_" + i);
            go.transform.SetParent(root.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            img.raycastTarget = false;
            borderLines[i] = go;
            borderRTs[i] = rt;
        }
    }

    private void CreateCornerHandles()
    {
        cornerHandles[0] = CreateHandle("Corner_BL", DragMode.ScaleBL);
        cornerHandles[1] = CreateHandle("Corner_BR", DragMode.ScaleBR);
        cornerHandles[2] = CreateHandle("Corner_TL", DragMode.ScaleTL);
        cornerHandles[3] = CreateHandle("Corner_TR", DragMode.ScaleTR);
        for (int i = 0; i < 4; i++)
            cornerRTs[i] = cornerHandles[i].GetComponent<RectTransform>();
    }

    private void CreateEdgeHandles()
    {
        edgeHandles[0] = CreateHandle("Edge_Bottom", DragMode.ScaleBottom);
        edgeHandles[1] = CreateHandle("Edge_Top", DragMode.ScaleTop);
        edgeHandles[2] = CreateHandle("Edge_Left", DragMode.ScaleLeft);
        edgeHandles[3] = CreateHandle("Edge_Right", DragMode.ScaleRight);
        for (int i = 0; i < 4; i++)
            edgeRTs[i] = edgeHandles[i].GetComponent<RectTransform>();
    }

    private GameObject CreateHandle(string name, DragMode mode)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(HANDLE_SIZE, HANDLE_SIZE);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = true;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        outline.effectDistance = new Vector2(1, -1);

        var trigger = go.AddComponent<EventTrigger>();
        var pDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pDown.callback.AddListener(e => StartDrag(mode));
        trigger.triggers.Add(pDown);

        return go;
    }

    private void CreateActionBar()
    {
        actionBar = new GameObject("ActionBar");
        actionBar.transform.SetParent(root.transform, false);
        var rt = actionBar.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(0, 26);

        var hlg = actionBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3;
        hlg.padding = new RectOffset(3, 3, 2, 2);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        actionBar.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        actionBar.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.13f, 0.92f);

        ActionBtn("选择文件", RuntimeUIHelper.AccentBlue, OnPickFile);
        ActionBtn("设置交互", new Color(0.55f, 0.4f, 0.7f), OnOpenSettings);
        ActionBtn("复制", new Color(0.4f, 0.5f, 0.6f), OnDuplicate);
        ActionBtn("▲", new Color(0.3f, 0.5f, 0.3f), OnMoveToTop, 28);
        ActionBtn("▼", new Color(0.4f, 0.35f, 0.2f), OnMoveToBottom, 28);
        ActionBtn("删除", RuntimeUIHelper.AccentRed, OnDelete);
    }

    private void CreateSizeLabel()
    {
        sizeLabelGO = new GameObject("SizeLabel");
        sizeLabelGO.transform.SetParent(root.transform, false);
        var rt = sizeLabelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(100, 16);
        sizeLabelRT = rt;
        sizeLabel = sizeLabelGO.AddComponent<Text>();
        sizeLabel.font = RuntimeUIHelper.GetFont();
        sizeLabel.fontSize = 11;
        sizeLabel.color = new Color(0.2f, 0.2f, 0.2f);
        sizeLabel.alignment = TextAnchor.MiddleCenter;
        sizeLabel.raycastTarget = false;
    }

    private void ActionBtn(string label, Color color, System.Action onClick, float width = 60)
    {
        var go = new GameObject("AB_" + label);
        go.transform.SetParent(actionBar.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(() => onClick());
        btn.colors = new ColorBlock
        {
            normalColor = color,
            highlightedColor = color * 1.2f,
            pressedColor = color * 0.8f,
            selectedColor = color,
            disabledColor = color * 0.5f,
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        go.AddComponent<LayoutElement>().preferredWidth = width;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var t = textGO.AddComponent<Text>();
        t.text = label;
        t.font = RuntimeUIHelper.GetFont();
        t.fontSize = 11;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
    }

    private void OnPickFile()
    {
        string path = NativeFilePicker.PickMediaFile("选择文件");
        if (string.IsNullOrEmpty(path)) return;
        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.AssignMediaToSelected(path);
    }

    private void OnOpenSettings()
    {
        var editor = RuntimeEditor.Instance;
        if (editor == null) return;

        SyncTransformToData();

        var panel = editor.GetComponent<RuntimeSettingsPanel>();
        if (panel != null && data != null)
            panel.Open(data, target);
    }

    private void OnMoveToTop()
    {
        var editor = RuntimeEditor.Instance;
        if (editor != null && data != null)
        {
            SyncTransformToData();
            editor.MoveElementToTop(data);
        }
    }

    private void OnMoveToBottom()
    {
        var editor = RuntimeEditor.Instance;
        if (editor != null && data != null)
        {
            SyncTransformToData();
            editor.MoveElementToBottom(data);
        }
    }

    private void SyncTransformToData()
    {
        if (data != null && target != null)
        {
            data.x = target.transform.position.x;
            data.y = target.transform.position.y;
            data.scaleX = target.transform.localScale.x;
            data.scaleY = target.transform.localScale.y;
        }
    }

    private void OnDuplicate()
    {
        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.DuplicateSelected();
    }

    private void OnDelete()
    {
        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.DeleteSelected();
    }
}

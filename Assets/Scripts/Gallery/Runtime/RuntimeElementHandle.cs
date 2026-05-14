using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RuntimeElementHandle : MonoBehaviour
{
    private Canvas handleCanvas;
    private GameObject root;
    private RectTransform rootRT;
    private GameObject actionBar;
    private GameObject handleBar;
    private Text sizeLabel;

    private GameObject target;
    private ElementData data;
    private Camera cam;

    private enum DragMode { None, Move, ScaleH, ScaleV, ScaleUniform }
    private DragMode dragMode;
    private Vector3 dragStartMouse;
    private Vector3 dragStartScale;
    private Vector3 dragStartPos;

    public void Attach(GameObject go, ElementData elemData)
    {
        target = go;
        data = elemData;
        cam = Camera.main;
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

    private void LateUpdate()
    {
        if (target == null) { if (root != null) root.SetActive(false); return; }
        if (root == null || !root.activeSelf) return;

        HandleDragInput();
        UpdatePosition();
    }

    private void HandleDragInput()
    {
        if (dragMode == DragMode.None) return;

        if (Input.GetMouseButton(0))
        {
            Vector3 mouseDelta = Input.mousePosition - dragStartMouse;
            var editor = RuntimeEditor.Instance;
            if (editor == null) return;

            switch (dragMode)
            {
                case DragMode.Move:
                    if (cam != null)
                    {
                        Vector3 worldNow = cam.ScreenToWorldPoint(Input.mousePosition);
                        Vector3 worldStart = cam.ScreenToWorldPoint(dragStartMouse);
                        worldNow.z = 0; worldStart.z = 0;
                        target.transform.position = dragStartPos + (worldNow - worldStart);
                    }
                    break;
                case DragMode.ScaleH:
                    float dxH = mouseDelta.x * 0.005f;
                    editor.ScaleSelected(dxH, 0);
                    dragStartMouse = Input.mousePosition;
                    break;
                case DragMode.ScaleV:
                    float dyV = mouseDelta.y * 0.005f;
                    editor.ScaleSelected(0, dyV);
                    dragStartMouse = Input.mousePosition;
                    break;
                case DragMode.ScaleUniform:
                    float avg = (mouseDelta.x + mouseDelta.y) * 0.5f * 0.005f;
                    editor.ScaleSelected(avg, avg);
                    dragStartMouse = Input.mousePosition;
                    break;
            }
            UpdateSizeLabel();
        }
        else
        {
            dragMode = DragMode.None;
        }
    }

    private void StartDrag(DragMode mode)
    {
        if (target == null) return;
        dragMode = mode;
        dragStartMouse = Input.mousePosition;
        dragStartScale = target.transform.localScale;
        dragStartPos = target.transform.position;
    }

    private void UpdatePosition()
    {
        if (target == null || cam == null || rootRT == null) return;

        Bounds bounds = GetWorldBounds(target);
        Vector2 min = cam.WorldToScreenPoint(bounds.min);
        Vector2 max = cam.WorldToScreenPoint(bounds.max);

        var canvasScaler = handleCanvas.GetComponent<CanvasScaler>();
        float scaleX = canvasScaler.referenceResolution.x / Screen.width;
        float scaleY = canvasScaler.referenceResolution.y / Screen.height;
        float scale = Mathf.Lerp(scaleX, scaleY, canvasScaler.matchWidthOrHeight);

        float pad = 8f;
        float x0 = min.x * scale - pad;
        float y0 = min.y * scale - pad;
        float x1 = max.x * scale + pad;
        float y1 = max.y * scale + pad;
        float w = x1 - x0;
        float h = y1 - y0;

        rootRT.anchoredPosition = new Vector2(x0, y0);
        rootRT.sizeDelta = new Vector2(w, h);

        var abRT = actionBar.GetComponent<RectTransform>();
        abRT.anchoredPosition = new Vector2(w * 0.5f, h + 4);

        var hbRT = handleBar.GetComponent<RectTransform>();
        hbRT.anchoredPosition = new Vector2(w * 0.5f, -4);
    }

    private void UpdateSizeLabel()
    {
        if (sizeLabel == null || target == null) return;
        Vector3 s = target.transform.localScale;
        sizeLabel.text = $"{s.x:F1} x {s.y:F1}";
    }

    private Bounds GetWorldBounds(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) return sr.bounds;
        Vector3 pos = go.transform.position;
        Vector3 s = go.transform.localScale;
        return new Bounds(pos, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 0));
    }

    // ── UI Creation ──

    private void CreateUI()
    {
        var editor = RuntimeEditor.Instance;
        if (editor == null || editor.EditorCanvas == null) return;
        handleCanvas = editor.EditorCanvas;

        root = new GameObject("ElementHandle");
        root.transform.SetParent(handleCanvas.transform, false);
        rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.zero;
        rootRT.pivot = Vector2.zero;

        CreateBorder();
        CreateActionBar();
        CreateHandleBar();
    }

    private void CreateBorder()
    {
        var border = new GameObject("Border");
        border.transform.SetParent(root.transform, false);
        var rt = border.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        border.AddComponent<Outline>();
        var img = border.AddComponent<Image>();
        img.color = new Color(0.3f, 0.7f, 1f, 0.15f);
        img.raycastTarget = false;
    }

    private void CreateActionBar()
    {
        actionBar = new GameObject("ActionBar");
        actionBar.transform.SetParent(root.transform, false);
        var rt = actionBar.AddComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(0, 28);

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
        ActionBtn("删除", RuntimeUIHelper.AccentRed, OnDelete);
    }

    private void CreateHandleBar()
    {
        handleBar = new GameObject("HandleBar");
        handleBar.transform.SetParent(root.transform, false);
        var rt = handleBar.AddComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 28);

        var hlg = handleBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3;
        hlg.padding = new RectOffset(3, 3, 2, 2);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        handleBar.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        handleBar.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.13f, 0.92f);

        DragHandle("移动", new Color(0.3f, 0.6f, 0.3f), DragMode.Move, 60);
        DragHandle("横向缩放", new Color(0.6f, 0.45f, 0.2f), DragMode.ScaleH, 70);
        DragHandle("纵向缩放", new Color(0.2f, 0.45f, 0.6f), DragMode.ScaleV, 70);
        DragHandle("等比缩放", new Color(0.5f, 0.35f, 0.55f), DragMode.ScaleUniform, 70);

        var labelGO = new GameObject("SizeLabel");
        labelGO.transform.SetParent(handleBar.transform, false);
        labelGO.AddComponent<RectTransform>();
        sizeLabel = labelGO.AddComponent<Text>();
        sizeLabel.font = RuntimeUIHelper.GetFont();
        sizeLabel.fontSize = 11;
        sizeLabel.color = new Color(0.7f, 0.7f, 0.7f);
        sizeLabel.alignment = TextAnchor.MiddleCenter;
        sizeLabel.raycastTarget = false;
        labelGO.AddComponent<LayoutElement>().preferredWidth = 60;
    }

    private void DragHandle(string label, Color color, DragMode mode, float width)
    {
        var go = new GameObject("DH_" + label);
        go.transform.SetParent(handleBar.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        go.AddComponent<LayoutElement>().preferredWidth = width;

        var trigger = go.AddComponent<EventTrigger>();

        var pDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pDown.callback.AddListener(e => StartDrag(mode));
        trigger.triggers.Add(pDown);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(e => { });
        trigger.triggers.Add(drag);

        var pUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pUp.callback.AddListener(e => { dragMode = DragMode.None; });
        trigger.triggers.Add(pUp);

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

    private void ActionBtn(string label, Color color, System.Action onClick)
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
        go.AddComponent<LayoutElement>().preferredWidth = 65;

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

        if (data != null && target != null)
        {
            data.x = target.transform.position.x;
            data.y = target.transform.position.y;
            data.scaleX = target.transform.localScale.x;
            data.scaleY = target.transform.localScale.y;
        }

        var panel = editor.GetComponent<RuntimeSettingsPanel>();
        if (panel != null && data != null)
            panel.Open(data, target);
    }

    private void OnDelete()
    {
        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.DeleteSelected();
    }
}

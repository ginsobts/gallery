using UnityEngine;
using UnityEngine.UI;

public static class RuntimeUIHelper
{
    private static Font _font;

    public static Font GetFont()
    {
        if (_font != null) return _font;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        return _font;
    }

    public static readonly Color PanelBG = new Color(0.12f, 0.12f, 0.15f, 0.94f);
    public static readonly Color BtnNormal = new Color(0.25f, 0.25f, 0.3f);
    public static readonly Color BtnHover = new Color(0.35f, 0.55f, 0.7f);
    public static readonly Color BtnPress = new Color(0.2f, 0.4f, 0.6f);
    public static readonly Color InputBG = new Color(0.18f, 0.18f, 0.22f);
    public static readonly Color ToggleOn = new Color(0.2f, 0.65f, 0.35f);
    public static readonly Color ToggleOff = new Color(0.35f, 0.35f, 0.4f);
    public static readonly Color SectionColor = new Color(0.22f, 0.28f, 0.38f);
    public static readonly Color AccentBlue = new Color(0.3f, 0.55f, 0.85f);
    public static readonly Color AccentRed = new Color(0.7f, 0.25f, 0.25f);
    public static readonly Color AccentGreen = new Color(0.25f, 0.6f, 0.3f);

    public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static Text Label(Transform parent, string text, int fontSize = 14, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = GetFont();
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 20;
        le.preferredHeight = 20;
        return t;
    }

    public static Button Btn(Transform parent, string label, System.Action onClick, Color? bgColor = null)
    {
        Color bg = bgColor ?? BtnNormal;
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
        var img = go.AddComponent<Image>();
        img.color = bg;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        btn.colors = new ColorBlock
        {
            normalColor = bg,
            highlightedColor = BtnHover,
            pressedColor = BtnPress,
            selectedColor = bg,
            disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var t = textGO.AddComponent<Text>();
        t.text = label;
        t.font = GetFont();
        t.fontSize = 13;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        go.AddComponent<LayoutElement>().minHeight = 28;
        return btn;
    }

    public static void Section(Transform parent, string title)
    {
        var go = new GameObject("Section");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
        var img = go.AddComponent<Image>();
        img.color = SectionColor;
        img.raycastTarget = false;
        var t = Label(go.transform, "  " + title, 13, TextAnchor.MiddleLeft);
        t.fontStyle = FontStyle.Bold;
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        Object.Destroy(t.GetComponent<LayoutElement>());
        go.AddComponent<LayoutElement>().minHeight = 24;
    }

    public static Toggle ToggleField(Transform parent, string label, bool value, System.Action<bool> onChange)
    {
        var go = new GameObject("Toggle_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        go.AddComponent<LayoutElement>().minHeight = 26;

        var boxGO = new GameObject("Box");
        boxGO.transform.SetParent(go.transform, false);
        boxGO.AddComponent<RectTransform>();
        var boxImg = boxGO.AddComponent<Image>();
        boxImg.color = value ? ToggleOn : ToggleOff;
        boxImg.raycastTarget = true;
        boxGO.AddComponent<LayoutElement>().preferredWidth = 26;

        var checkGO = new GameObject("Check");
        checkGO.transform.SetParent(boxGO.transform, false);
        var checkRT = checkGO.AddComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.2f, 0.2f);
        checkRT.anchorMax = new Vector2(0.8f, 0.8f);
        checkRT.offsetMin = Vector2.zero; checkRT.offsetMax = Vector2.zero;
        var checkImg = checkGO.AddComponent<Image>();
        checkImg.color = Color.white;
        checkImg.enabled = value;

        var toggle = go.AddComponent<Toggle>();
        toggle.isOn = value;
        toggle.targetGraphic = boxImg;
        toggle.graphic = checkImg;
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        toggle.onValueChanged.AddListener(v =>
        {
            boxImg.color = v ? ToggleOn : ToggleOff;
            onChange?.Invoke(v);
        });

        Label(go.transform, label, 13);

        return toggle;
    }

    public static InputField TextField(Transform parent, string label, string value, System.Action<string> onChange)
    {
        var go = new GameObject("Field_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().minHeight = 26;

        var lbl = Label(go.transform, label, 12);
        lbl.GetComponent<LayoutElement>().preferredWidth = 80;

        var inputGO = new GameObject("Input");
        inputGO.transform.SetParent(go.transform, false);
        inputGO.AddComponent<RectTransform>();
        var inputImg = inputGO.AddComponent<Image>();
        inputImg.color = InputBG;
        var input = inputGO.AddComponent<InputField>();
        input.targetGraphic = inputImg;
        input.navigation = new Navigation { mode = Navigation.Mode.None };
        inputGO.AddComponent<LayoutElement>().flexibleWidth = 1;

        var inputTextGO = new GameObject("Text");
        inputTextGO.transform.SetParent(inputGO.transform, false);
        var itr = inputTextGO.AddComponent<RectTransform>();
        itr.anchorMin = Vector2.zero; itr.anchorMax = Vector2.one;
        itr.offsetMin = new Vector2(4, 0); itr.offsetMax = new Vector2(-4, 0);
        var it = inputTextGO.AddComponent<Text>();
        it.font = GetFont();
        it.fontSize = 12;
        it.color = Color.white;
        it.supportRichText = false;
        input.textComponent = it;
        input.text = value ?? "";
        input.onEndEdit.AddListener(v => onChange?.Invoke(v));

        return input;
    }

    public static InputField FloatField(Transform parent, string label, float value, System.Action<float> onChange)
    {
        return TextField(parent, label, value.ToString("F2"), s =>
        {
            if (float.TryParse(s, out float f)) onChange?.Invoke(f);
        });
    }

    public static InputField IntField(Transform parent, string label, int value, System.Action<int> onChange)
    {
        return TextField(parent, label, value.ToString(), s =>
        {
            if (int.TryParse(s, out int i)) onChange?.Invoke(i);
        });
    }

    public static void Spacer(Transform parent, float height = 6)
    {
        var go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
        go.AddComponent<LayoutElement>().minHeight = height;
    }

    public static GameObject ScrollPanel(Transform parent, out Transform content)
    {
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(parent, false);
        var srt = scrollGO.AddComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        var maskImg = scrollGO.AddComponent<Image>();
        maskImg.color = Color.white;
        var mask = scrollGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        var crt = contentGO.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = Vector2.zero;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = crt;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 40f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        content = contentGO.transform;
        return scrollGO;
    }
}

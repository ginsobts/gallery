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

    public static void ButtonGroup(Transform parent, string label, string[] options, int current, System.Action<int> onChange)
    {
        if (!string.IsNullOrEmpty(label))
            Label(parent, label, 12);

        int perRow = 4;
        for (int row = 0; row * perRow < options.Length; row++)
        {
            var rowGO = new GameObject("BtnRow_" + row);
            rowGO.transform.SetParent(parent, false);
            rowGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 2;
            hlg.padding = new RectOffset(2, 2, 0, 0);
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            rowGO.AddComponent<LayoutElement>().minHeight = 24;

            int start = row * perRow;
            int end = Mathf.Min(start + perRow, options.Length);
            for (int i = start; i < end; i++)
            {
                int idx = i;
                Color c = (idx == current) ? AccentBlue : BtnNormal;
                var btn = Btn(rowGO.transform, options[idx], () => onChange(idx), c);
                var le = btn.GetComponent<LayoutElement>();
                if (le != null) { le.minHeight = 22; le.flexibleWidth = 1; }
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null) txt.fontSize = 11;
            }
        }
    }

    public static Dropdown DropdownField(Transform parent, string label, string[] options, int current, System.Action<int> onChange)
    {
        var go = new GameObject("Drop_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().minHeight = 28;

        var lbl = Label(go.transform, label, 12);
        lbl.GetComponent<LayoutElement>().preferredWidth = 80;

        var dropGO = new GameObject("Dropdown");
        dropGO.transform.SetParent(go.transform, false);
        dropGO.AddComponent<RectTransform>();
        var dropImg = dropGO.AddComponent<Image>();
        dropImg.color = InputBG;
        dropGO.AddComponent<LayoutElement>().flexibleWidth = 1;

        var captionGO = new GameObject("Label");
        captionGO.transform.SetParent(dropGO.transform, false);
        var crt = captionGO.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(6, 0); crt.offsetMax = new Vector2(-20, 0);
        var captionText = captionGO.AddComponent<Text>();
        captionText.font = GetFont();
        captionText.fontSize = 12;
        captionText.color = Color.white;
        captionText.alignment = TextAnchor.MiddleLeft;

        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(dropGO.transform, false);
        var art = arrowGO.AddComponent<RectTransform>();
        art.anchorMin = new Vector2(1, 0); art.anchorMax = Vector2.one;
        art.sizeDelta = new Vector2(18, 0); art.anchoredPosition = new Vector2(-9, 0);
        var arrowText = arrowGO.AddComponent<Text>();
        arrowText.font = GetFont();
        arrowText.fontSize = 12;
        arrowText.text = "\u25BC";
        arrowText.color = new Color(0.7f, 0.7f, 0.7f);
        arrowText.alignment = TextAnchor.MiddleCenter;

        var templateGO = new GameObject("Template");
        templateGO.transform.SetParent(dropGO.transform, false);
        var trt = templateGO.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(1, 0);
        trt.pivot = new Vector2(0.5f, 1);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(0, 150);
        var tImg = templateGO.AddComponent<Image>();
        tImg.color = new Color(0.16f, 0.16f, 0.2f, 0.98f);
        var tScroll = templateGO.AddComponent<ScrollRect>();
        tScroll.horizontal = false;

        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(templateGO.transform, false);
        var vrt = viewportGO.AddComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
        viewportGO.AddComponent<Image>().color = Color.white;
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        tScroll.viewport = vrt;

        var contentGO2 = new GameObject("Content");
        contentGO2.transform.SetParent(viewportGO.transform, false);
        var ccrt = contentGO2.AddComponent<RectTransform>();
        ccrt.anchorMin = new Vector2(0, 1); ccrt.anchorMax = new Vector2(1, 1);
        ccrt.pivot = new Vector2(0.5f, 1);
        ccrt.sizeDelta = Vector2.zero;
        tScroll.content = ccrt;

        var itemGO = new GameObject("Item");
        itemGO.transform.SetParent(contentGO2.transform, false);
        var irt = itemGO.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0, 0.5f); irt.anchorMax = new Vector2(1, 0.5f);
        irt.sizeDelta = new Vector2(0, 24);
        var itemToggle = itemGO.AddComponent<Toggle>();
        itemToggle.navigation = new Navigation { mode = Navigation.Mode.None };

        var itemBG = new GameObject("Item Background");
        itemBG.transform.SetParent(itemGO.transform, false);
        var ibrt = itemBG.AddComponent<RectTransform>();
        ibrt.anchorMin = Vector2.zero; ibrt.anchorMax = Vector2.one;
        ibrt.offsetMin = Vector2.zero; ibrt.offsetMax = Vector2.zero;
        var ibImg = itemBG.AddComponent<Image>();
        ibImg.color = new Color(0.25f, 0.4f, 0.6f, 0.4f);
        itemToggle.targetGraphic = ibImg;

        var itemLabelGO = new GameObject("Item Label");
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        var ilrt = itemLabelGO.AddComponent<RectTransform>();
        ilrt.anchorMin = Vector2.zero; ilrt.anchorMax = Vector2.one;
        ilrt.offsetMin = new Vector2(6, 0); ilrt.offsetMax = new Vector2(-6, 0);
        var itemLabel = itemLabelGO.AddComponent<Text>();
        itemLabel.font = GetFont();
        itemLabel.fontSize = 12;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAnchor.MiddleLeft;

        templateGO.SetActive(false);

        var dropdown = dropGO.AddComponent<Dropdown>();
        dropdown.targetGraphic = dropImg;
        dropdown.template = trt;
        dropdown.captionText = captionText;
        dropdown.itemText = itemLabel;
        dropdown.navigation = new Navigation { mode = Navigation.Mode.None };

        dropdown.ClearOptions();
        var optList = new System.Collections.Generic.List<Dropdown.OptionData>();
        foreach (var opt in options)
            optList.Add(new Dropdown.OptionData(opt));
        dropdown.AddOptions(optList);
        dropdown.value = Mathf.Clamp(current, 0, options.Length - 1);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(v => onChange?.Invoke(v));

        return dropdown;
    }

    public static Slider SliderField(Transform parent, string label, float min, float max, float value, System.Action<float> onChange)
    {
        var go = new GameObject("Slider_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4; hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().minHeight = 26;

        var lbl = Label(go.transform, label, 12);
        lbl.GetComponent<LayoutElement>().preferredWidth = 80;

        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(go.transform, false);
        sliderGO.AddComponent<RectTransform>();
        sliderGO.AddComponent<LayoutElement>().flexibleWidth = 1;

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgrt = bgGO.AddComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0, 0.35f); bgrt.anchorMax = new Vector2(1, 0.65f);
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fart = fillArea.AddComponent<RectTransform>();
        fart.anchorMin = new Vector2(0, 0.35f); fart.anchorMax = new Vector2(1, 0.65f);
        fart.offsetMin = Vector2.zero; fart.offsetMax = Vector2.zero;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        var frt = fillGO.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        fillGO.AddComponent<Image>().color = AccentBlue;

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var hart = handleArea.AddComponent<RectTransform>();
        hart.anchorMin = Vector2.zero; hart.anchorMax = Vector2.one;
        hart.offsetMin = Vector2.zero; hart.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleArea.transform, false);
        var hrt = handleGO.AddComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(14, 0);
        handleGO.AddComponent<Image>().color = Color.white;

        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = frt;
        slider.handleRect = hrt;
        slider.targetGraphic = handleGO.GetComponent<Image>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        var valText = Label(go.transform, value.ToString("F1"), 11);
        valText.GetComponent<LayoutElement>().preferredWidth = 36;

        slider.onValueChanged.AddListener(v =>
        {
            valText.text = v.ToString("F1");
            onChange?.Invoke(v);
        });

        return slider;
    }

    public static void ReadOnlyField(Transform parent, string label, string value)
    {
        var go = new GameObject("RO_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4; hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().minHeight = 22;

        var lbl = Label(go.transform, label, 11);
        lbl.GetComponent<LayoutElement>().preferredWidth = 80;
        var val = Label(go.transform, value, 11);
        val.color = new Color(0.7f, 0.8f, 0.9f);
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

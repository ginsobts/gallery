using UnityEngine;
using UnityEngine.UI;

public class RuntimeBlockSettingsPanel : MonoBehaviour
{
    private Canvas panelCanvas;
    private GameObject panelRoot;
    private RectTransform panelRT;
    private Transform content;

    private SceneSettingsData sceneData;
    private BlockSettingsData blockData;
    private int currentBlockIndex;

    private static readonly string[] WeatherNames = { "雨", "雪", "雾", "阳光", "萤火虫" };
    private static readonly string[] ColorFilterNames = {
        "无", "复古", "怀旧", "冷色", "暖色", "黑白",
        "梦幻", "日落", "海洋", "森林", "霓虹", "粉彩",
        "高对比", "褪色", "蓝紫", "金色"
    };
    private static readonly string[] ArtisticNames = {
        "无", "铅笔", "油画", "水彩", "像素",
        "漫画", "印象派", "点彩", "木刻", "炭笔",
        "线描", "彩窗", "马赛克", "波普", "故障",
        "浮世绘", "低多边形", "浮雕", "热成像", "负片",
        "十字绣", "VHS"
    };

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    public void Open(SceneSettingsData settings)
    {
        sceneData = settings;
        currentBlockIndex = DetectCurrentBlock();
        EnsureBlockData();

        if (panelRoot == null) CreatePanel();
        panelRoot.SetActive(true);
        BuildContent();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ApplyAndSave();
    }

    private int DetectCurrentBlock()
    {
        var cam = Camera.main;
        if (cam == null) return 0;
        var gc = cam.GetComponent<GalleryCamera>();
        if (gc != null) return gc.CurrentBlock;
        return 0;
    }

    private void EnsureBlockData()
    {
        if (sceneData.blockSettings == null)
            sceneData.blockSettings = new BlockSettingsData[0];

        int count = Mathf.Max(1, sceneData.cameraBlockCount);
        var list = new System.Collections.Generic.List<BlockSettingsData>(sceneData.blockSettings);
        while (list.Count < count)
        {
            var bs = new BlockSettingsData();
            bs.blockIndex = list.Count;
            bs.ambientBrightness = sceneData.ambientBrightness;
            bs.ambientColor = sceneData.ambientColor != null ? (float[])sceneData.ambientColor.Clone() : new float[] { 0.5f, 0.5f, 0.6f, 1f };
            bs.bgColor = sceneData.backgroundColor != null ? (float[])sceneData.backgroundColor.Clone() : new float[] { 0.05f, 0.05f, 0.1f, 1f };
            list.Add(bs);
        }
        sceneData.blockSettings = list.ToArray();

        currentBlockIndex = Mathf.Clamp(currentBlockIndex, 0, sceneData.blockSettings.Length - 1);
        blockData = sceneData.blockSettings[currentBlockIndex];
    }

    private void CreatePanel()
    {
        var editor = RuntimeEditor.Instance;
        panelCanvas = editor != null ? editor.EditorCanvas : null;
        if (panelCanvas == null) return;

        panelRoot = new GameObject("BlockSettingsPanel");
        panelRoot.transform.SetParent(panelCanvas.transform, false);
        panelRT = panelRoot.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1, 0);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.pivot = new Vector2(1, 0.5f);
        panelRT.anchoredPosition = new Vector2(-10, 0);
        panelRT.sizeDelta = new Vector2(360, 0);
        panelRT.offsetMin = new Vector2(panelRT.offsetMin.x, 40);
        panelRT.offsetMax = new Vector2(panelRT.offsetMax.x, -40);
        panelRoot.AddComponent<Image>().color = RuntimeUIHelper.PanelBG;

        RuntimeUIHelper.ScrollPanel(panelRoot.transform, out content);
    }

    private void BuildContent()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        RuntimeUIHelper.Label(content, "区块 " + currentBlockIndex + " 设置", 16, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
        RuntimeUIHelper.Spacer(content, 2);

        if (sceneData.cameraBlockCount > 1)
            BuildBlockSwitcher();

        RuntimeUIHelper.Spacer(content, 4);
        BuildBackgroundUI();
        RuntimeUIHelper.Spacer(content, 4);
        BuildLightingUI();
        RuntimeUIHelper.Spacer(content, 4);
        BuildWeatherUI();
        RuntimeUIHelper.Spacer(content, 4);
        BuildFilterUI();
        RuntimeUIHelper.Spacer(content, 4);
        BuildBGMUI();

        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.Section(content, "NPC跟随");
        RuntimeUIHelper.ToggleField(content, "进入此区块时遣散跟随者", blockData.dismissFollowers, v => blockData.dismissFollowers = v);

        RuntimeUIHelper.Spacer(content, 6);
        if (sceneData.cameraBlockCount > 1)
            BuildCopySection();

        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.Btn(content, "预览", () => Preview(), RuntimeUIHelper.AccentBlue);
        RuntimeUIHelper.Spacer(content, 2);
        RuntimeUIHelper.Btn(content, "应用并关闭", () => Close(), RuntimeUIHelper.AccentGreen);
        RuntimeUIHelper.Spacer(content, 4);
    }

    private void BuildBlockSwitcher()
    {
        var row = new GameObject("BlockSwitch");
        row.transform.SetParent(content, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3; hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;
        row.AddComponent<LayoutElement>().minHeight = 28;

        int count = Mathf.Min(sceneData.blockSettings.Length, 10);
        for (int i = 0; i < count; i++)
        {
            int idx = i;
            Color c = (idx == currentBlockIndex) ? RuntimeUIHelper.AccentBlue : RuntimeUIHelper.BtnNormal;
            RuntimeUIHelper.Btn(row.transform, "" + idx, () =>
            {
                currentBlockIndex = idx;
                blockData = sceneData.blockSettings[idx];
                BuildContent();
            }, c);
        }
    }

    // ── Background ──

    private void BuildBackgroundUI()
    {
        RuntimeUIHelper.Section(content, "背景");
        RuntimeUIHelper.Btn(content,
            string.IsNullOrEmpty(blockData.backgroundMediaFile) ? "选择背景图片" : "背景: " + blockData.backgroundMediaFile,
            () =>
            {
                string path = NativeFilePicker.PickImageFile("选择背景图片");
                if (string.IsNullOrEmpty(path)) return;
                var editor = RuntimeEditor.Instance;
                if (editor == null) return;
                string rel = RuntimeAssetLoader.Instance.CopyMediaToScene(path, editor.CurrentSceneName);
                if (!string.IsNullOrEmpty(rel))
                {
                    string fullPath = System.IO.Path.Combine(SceneDataHelper.GetScenePath(editor.CurrentSceneName), rel);
                    RuntimeAssetLoader.Instance.InvalidateCache(fullPath);
                    blockData.backgroundMediaFile = rel;
                    BuildContent();
                }
            });
        RuntimeUIHelper.FloatField(content, "缩放X", blockData.backgroundScaleX, v => blockData.backgroundScaleX = v);
        RuntimeUIHelper.FloatField(content, "缩放Y", blockData.backgroundScaleY, v => blockData.backgroundScaleY = v);
    }

    // ── Lighting ──

    private void BuildLightingUI()
    {
        RuntimeUIHelper.Section(content, "灯光");
        RuntimeUIHelper.FloatField(content, "环境亮度", blockData.ambientBrightness, v => blockData.ambientBrightness = Mathf.Clamp01(v));

        Color ac = SceneDataHelper.ToColor(blockData.ambientColor);
        RuntimeUIHelper.FloatField(content, "环境R", ac.r, v => { ac.r = v; blockData.ambientColor = SceneDataHelper.FromColor(ac); });
        RuntimeUIHelper.FloatField(content, "环境G", ac.g, v => { ac.g = v; blockData.ambientColor = SceneDataHelper.FromColor(ac); });
        RuntimeUIHelper.FloatField(content, "环境B", ac.b, v => { ac.b = v; blockData.ambientColor = SceneDataHelper.FromColor(ac); });

        Color bgc = SceneDataHelper.ToColor(blockData.bgColor);
        RuntimeUIHelper.FloatField(content, "背景色R", bgc.r, v => { bgc.r = v; blockData.bgColor = SceneDataHelper.FromColor(bgc); });
        RuntimeUIHelper.FloatField(content, "背景色G", bgc.g, v => { bgc.g = v; blockData.bgColor = SceneDataHelper.FromColor(bgc); });
        RuntimeUIHelper.FloatField(content, "背景色B", bgc.b, v => { bgc.b = v; blockData.bgColor = SceneDataHelper.FromColor(bgc); });
    }

    // ── Weather ──

    private void BuildWeatherUI()
    {
        RuntimeUIHelper.Section(content, "天气");
        RuntimeUIHelper.ToggleField(content, "启用天气", blockData.weatherEnabled, v =>
        {
            blockData.weatherEnabled = v;
            BuildContent();
        });

        if (blockData.weatherEnabled)
        {
            RuntimeUIHelper.DropdownField(content, "天气类型", WeatherNames, blockData.weatherType, v =>
            {
                blockData.weatherType = v;
                BuildContent();
            });
            RuntimeUIHelper.IntField(content, "粒子数量", blockData.weatherParticles, v => blockData.weatherParticles = Mathf.Max(1, v));

            Color wc = SceneDataHelper.ToColor(blockData.weatherColor);
            RuntimeUIHelper.FloatField(content, "颜色R", wc.r, v => { wc.r = v; blockData.weatherColor = SceneDataHelper.FromColor(wc); });
            RuntimeUIHelper.FloatField(content, "颜色G", wc.g, v => { wc.g = v; blockData.weatherColor = SceneDataHelper.FromColor(wc); });
            RuntimeUIHelper.FloatField(content, "颜色B", wc.b, v => { wc.b = v; blockData.weatherColor = SceneDataHelper.FromColor(wc); });
        }
    }

    // ── Filter ──

    private void BuildFilterUI()
    {
        RuntimeUIHelper.Section(content, "滤镜");

        RuntimeUIHelper.DropdownField(content, "颜色滤镜", ColorFilterNames, blockData.colorFilter, v =>
        {
            blockData.colorFilter = v;
            BuildContent();
        });
        if (blockData.colorFilter > 0)
            RuntimeUIHelper.FloatField(content, "颜色强度", blockData.colorFilterIntensity, v => blockData.colorFilterIntensity = Mathf.Clamp01(v));

        RuntimeUIHelper.Spacer(content, 2);
        RuntimeUIHelper.DropdownField(content, "风格化", ArtisticNames, blockData.artisticStyle, v =>
        {
            blockData.artisticStyle = v;
            BuildContent();
        });
        if (blockData.artisticStyle > 0)
            RuntimeUIHelper.FloatField(content, "风格强度", blockData.artisticIntensity, v => blockData.artisticIntensity = Mathf.Clamp01(v));
    }

    // ── BGM ──

    private void BuildBGMUI()
    {
        RuntimeUIHelper.Section(content, "BGM");
        RuntimeUIHelper.Btn(content,
            string.IsNullOrEmpty(blockData.bgmFile) ? "选择BGM音频" : "BGM: " + blockData.bgmFile,
            () =>
            {
                string path = NativeFilePicker.PickMediaFile("选择BGM音频");
                if (string.IsNullOrEmpty(path)) return;
                var editor = RuntimeEditor.Instance;
                if (editor == null) return;
                string rel = RuntimeAssetLoader.Instance.CopyMediaToScene(path, editor.CurrentSceneName);
                if (!string.IsNullOrEmpty(rel))
                {
                    blockData.bgmFile = rel;
                    BuildContent();
                }
            });
        RuntimeUIHelper.FloatField(content, "音量", blockData.bgmVolume, v => blockData.bgmVolume = Mathf.Clamp01(v));
        RuntimeUIHelper.FloatField(content, "淡入时长", blockData.bgmFadeTime, v => blockData.bgmFadeTime = Mathf.Max(0.1f, v));
    }

    // ── Copy from another block ──

    private void BuildCopySection()
    {
        RuntimeUIHelper.Section(content, "复制");
        for (int i = 0; i < sceneData.blockSettings.Length; i++)
        {
            if (i == currentBlockIndex) continue;
            int srcIdx = i;
            RuntimeUIHelper.Btn(content, "从区块 " + srcIdx + " 复制全部设置", () =>
            {
                CopyBlockSettings(sceneData.blockSettings[srcIdx], blockData);
                BuildContent();
            });
        }
    }

    private void CopyBlockSettings(BlockSettingsData src, BlockSettingsData dst)
    {
        int savedIndex = dst.blockIndex;
        dst.backgroundMediaFile = src.backgroundMediaFile;
        dst.backgroundScaleX = src.backgroundScaleX;
        dst.backgroundScaleY = src.backgroundScaleY;
        dst.ambientBrightness = src.ambientBrightness;
        dst.ambientColor = src.ambientColor != null ? (float[])src.ambientColor.Clone() : null;
        dst.bgColor = src.bgColor != null ? (float[])src.bgColor.Clone() : null;
        dst.weatherEnabled = src.weatherEnabled;
        dst.weatherType = src.weatherType;
        dst.weatherParticles = src.weatherParticles;
        dst.weatherColor = src.weatherColor != null ? (float[])src.weatherColor.Clone() : null;
        dst.colorFilter = src.colorFilter;
        dst.colorFilterIntensity = src.colorFilterIntensity;
        dst.artisticStyle = src.artisticStyle;
        dst.artisticIntensity = src.artisticIntensity;
        dst.bgmFile = src.bgmFile;
        dst.bgmVolume = src.bgmVolume;
        dst.bgmFadeTime = src.bgmFadeTime;
        dst.dismissFollowers = src.dismissFollowers;
        dst.blockIndex = savedIndex;
    }

    // ── Preview & Apply ──

    private void Preview()
    {
        var builder = RuntimeSceneBuilder.Instance;
        if (builder != null) builder.ApplyBlockSettingsManager(sceneData);
        var editor = RuntimeEditor.Instance;
        if (editor != null) editor.SetStatus("区块 " + currentBlockIndex + " 预览已应用");
    }

    private void ApplyAndSave()
    {
        Preview();
        var editor = RuntimeEditor.Instance;
        if (editor != null)
        {
            editor.SaveScene();
            editor.SetStatus("区块 " + currentBlockIndex + " 设置已保存");
        }
    }
}

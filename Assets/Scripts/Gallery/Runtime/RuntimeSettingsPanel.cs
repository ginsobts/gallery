using UnityEngine;
using UnityEngine.UI;

public class RuntimeSettingsPanel : MonoBehaviour
{
    private Canvas panelCanvas;
    private GameObject panelRoot;
    private RectTransform panelRT;
    private Transform content;

    private ElementData currentData;
    private GameObject currentTarget;
    private bool needsRebuild;
    private bool pendingUIRebuild;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Update()
    {
        if (!pendingUIRebuild) return;
        pendingUIRebuild = false;
        if (IsOpen) BuildContent();
    }

    private void ScheduleRebuild()
    {
        pendingUIRebuild = true;
    }

    public void Open(ElementData data, GameObject target)
    {
        currentData = data;
        currentTarget = target;
        if (panelRoot == null) CreatePanel();
        panelRoot.SetActive(true);
        pendingUIRebuild = false;
        BuildContent();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (needsRebuild && currentData != null)
        {
            if (currentTarget != null)
            {
                currentData.x = currentTarget.transform.position.x;
                currentData.y = currentTarget.transform.position.y;
                currentData.scaleX = currentTarget.transform.localScale.x;
                currentData.scaleY = currentTarget.transform.localScale.y;
            }
            var editor = RuntimeEditor.Instance;
            if (editor != null)
            {
                editor.RebuildElement(currentData);
                needsRebuild = false;
            }
        }
        currentData = null;
        currentTarget = null;
    }

    private void CreatePanel()
    {
        var editor = RuntimeEditor.Instance;
        panelCanvas = editor != null ? editor.EditorCanvas : null;
        if (panelCanvas == null) return;

        panelRoot = new GameObject("SettingsPanel");
        panelRoot.transform.SetParent(panelCanvas.transform, false);
        panelRT = panelRoot.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1, 0);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.pivot = new Vector2(1, 0.5f);
        panelRT.offsetMin = new Vector2(-320, 10);
        panelRT.offsetMax = new Vector2(-10, -10);

        var bg = panelRoot.AddComponent<Image>();
        bg.color = RuntimeUIHelper.PanelBG;
        bg.raycastTarget = true;

        RuntimeUIHelper.ScrollPanel(panelRoot.transform, out content);
    }

    private void BuildContent()
    {
        ClearContent();
        if (currentData == null) return;

        BuildHeader();
        BuildTransformSection();

        switch (currentData.type)
        {
            case "photo": BuildPhotoSection(); break;
            case "video": BuildVideoSection(); break;
            case "npc_dialogue": BuildNPCDialogueSection(); break;
            case "weather": BuildWeatherSection(); break;
        }

        if (currentData.type == "photo" || currentData.type == "video" || currentData.type == "npc_dialogue")
            BuildInteractionSection();

        RuntimeUIHelper.Spacer(content, 10);
        RuntimeUIHelper.Btn(content, "应用并关闭", () => Close(), RuntimeUIHelper.AccentGreen);
        RuntimeUIHelper.Spacer(content, 6);
    }

    private void ClearContent()
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    // ── Header ──
    private void BuildHeader()
    {
        string typeLabel;
        switch (currentData.type)
        {
            case "photo": typeLabel = "照片"; break;
            case "video": typeLabel = "视频"; break;
            case "npc_dialogue": typeLabel = "NPC 对话"; break;
            case "npc_follower": typeLabel = "NPC 跟随"; break;
            case "weather": typeLabel = "天气"; break;
            default: typeLabel = currentData.type; break;
        }
        RuntimeUIHelper.Section(content, typeLabel + " 设置");
        RuntimeUIHelper.TextField(content, "ID", currentData.id, newId =>
        {
            if (string.IsNullOrEmpty(newId) || newId == currentData.id) return;
            var editor = RuntimeEditor.Instance;
            if (editor != null) editor.RenameElementId(currentData, newId);
        });

        RuntimeUIHelper.Btn(content, "更换文件", () =>
        {
            string path = NativeFilePicker.PickMediaFile("选择文件");
            if (string.IsNullOrEmpty(path)) return;
            var editor = RuntimeEditor.Instance;
            if (editor != null)
            {
                string rel = RuntimeAssetLoader.Instance.CopyMediaToScene(path, editor.CurrentSceneName);
                if (!string.IsNullOrEmpty(rel))
                {
                    currentData.mediaFile = rel;
                    currentData.mediaFitted = false;
                    string newFullPath = System.IO.Path.Combine(
                        SceneDataHelper.GetScenePath(editor.CurrentSceneName), rel);
                    RuntimeAssetLoader.Instance.InvalidateCache(newFullPath);
                    needsRebuild = true;
                    editor.SetStatus("已更换文件: " + rel);
                }
            }
        }, RuntimeUIHelper.AccentBlue);

        if (!string.IsNullOrEmpty(currentData.mediaFile))
            RuntimeUIHelper.Label(content, "文件: " + currentData.mediaFile, 10);

        RuntimeUIHelper.Spacer(content);
    }

    // ── Transform ──
    private void BuildTransformSection()
    {
        RuntimeUIHelper.Section(content, "变换");
        RuntimeUIHelper.FloatField(content, "位置 X", currentData.x, v => { currentData.x = v; ApplyTransform(); });
        RuntimeUIHelper.FloatField(content, "位置 Y", currentData.y, v => { currentData.y = v; ApplyTransform(); });

        if (currentData.type == "weather")
        {
            if (currentData.weather == null) currentData.weather = new WeatherData();
            RuntimeUIHelper.FloatField(content, "范围 X", currentData.weather.sizeX, v => { currentData.weather.sizeX = Mathf.Max(0.5f, v); needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "范围 Y", currentData.weather.sizeY, v => { currentData.weather.sizeY = Mathf.Max(0.5f, v); needsRebuild = true; });
        }
        else
        {
            RuntimeUIHelper.FloatField(content, "缩放 X", currentData.scaleX, v => { currentData.scaleX = v; ApplyTransform(); });
            RuntimeUIHelper.FloatField(content, "缩放 Y", currentData.scaleY, v => { currentData.scaleY = v; ApplyTransform(); });
            RuntimeUIHelper.IntField(content, "排序层", currentData.sortingOrder, v => { currentData.sortingOrder = v; needsRebuild = true; });
            RuntimeUIHelper.ToggleField(content, "有碰撞体", currentData.hasCollider, v => { currentData.hasCollider = v; needsRebuild = true; });
            RuntimeUIHelper.TextField(content, "说明文字", currentData.caption, v => { currentData.caption = v; needsRebuild = true; });

            if (currentData.type == "photo" || currentData.type == "video" || currentData.type == "npc_dialogue")
            {
                RuntimeUIHelper.Spacer(content, 4);
                RuntimeUIHelper.ToggleField(content, "可被玩家推动", currentData.pushable, v => { currentData.pushable = v; needsRebuild = true; });
                if (currentData.pushable)
                    RuntimeUIHelper.FloatField(content, "摩擦力", currentData.pushFriction, v => { currentData.pushFriction = Mathf.Max(0f, v); needsRebuild = true; });
            }
        }
        RuntimeUIHelper.Spacer(content);
    }

    private void ApplyTransform()
    {
        if (currentTarget == null) return;
        currentTarget.transform.position = new Vector3(currentData.x, currentData.y, 0);
        if (currentData.type != "weather")
            currentTarget.transform.localScale = new Vector3(currentData.scaleX, currentData.scaleY, 1);
    }

    // ── Photo ──
    private void BuildPhotoSection()
    {
        if (currentData.photo == null) currentData.photo = new PhotoData();
        var p = currentData.photo;

        RuntimeUIHelper.Section(content, "照片设置");
        RuntimeUIHelper.ToggleField(content, "靠近渐入", p.fadeInOnApproach, v => { p.fadeInOnApproach = v; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "渐入距离", p.fadeDistance, v => { p.fadeDistance = v; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "渐入速度", p.fadeSpeed, v => { p.fadeSpeed = v; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "文字大小", p.captionSize, v => { p.captionSize = v; needsRebuild = true; });
        BuildColorField("文字颜色", p.captionColor, arr => { p.captionColor = arr; needsRebuild = true; });

        RuntimeUIHelper.Spacer(content, 6);
        RuntimeUIHelper.ToggleField(content, "附带天气特效", p.hasWeather, v => { p.hasWeather = v; needsRebuild = true; ScheduleRebuild(); });
        if (p.hasWeather)
        {
            RuntimeUIHelper.IntField(content, "天气类型", p.weatherType, i => { p.weatherType = i; needsRebuild = true; });
            RuntimeUIHelper.Label(content, "(0=雨 1=雪 2=雾 3=阳光 4=萤火虫)", 10);
            RuntimeUIHelper.IntField(content, "粒子数量", p.weatherParticles, i => { p.weatherParticles = i; needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "范围 X", p.weatherSizeX, f => { p.weatherSizeX = f; needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "范围 Y", p.weatherSizeY, f => { p.weatherSizeY = f; needsRebuild = true; });
            BuildColorField("粒子颜色", p.weatherColor, arr => { p.weatherColor = arr; needsRebuild = true; });
        }

        RuntimeUIHelper.Spacer(content);
    }

    // ── Video ──
    private void BuildVideoSection()
    {
        if (currentData.video == null) currentData.video = new VideoData();
        var v = currentData.video;

        RuntimeUIHelper.Section(content, "视频设置");
        RuntimeUIHelper.ToggleField(content, "自动播放", v.autoPlay, b => { v.autoPlay = b; needsRebuild = true; });
        RuntimeUIHelper.ToggleField(content, "循环播放", v.loop, b => { v.loop = b; needsRebuild = true; });
        RuntimeUIHelper.TextField(content, "播放键", v.playKey, s => { v.playKey = s; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "触发范围", v.triggerRange, f => { v.triggerRange = f; needsRebuild = true; });
        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.ToggleField(content, "启用音频", v.enableAudio, b => { v.enableAudio = b; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "最大音量", v.maxVolume, f => { v.maxVolume = f; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "音频范围", v.audioRange, f => { v.audioRange = f; needsRebuild = true; });
        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.ToggleField(content, "靠近渐入", v.fadeInOnApproach, b => { v.fadeInOnApproach = b; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "渐入距离", v.fadeDistance, f => { v.fadeDistance = f; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "渐入速度", v.fadeSpeed, f => { v.fadeSpeed = f; needsRebuild = true; });

        RuntimeUIHelper.Btn(content, "选择封面图", () =>
        {
            string path = NativeFilePicker.PickImageFile("选择封面图片");
            if (string.IsNullOrEmpty(path)) return;
            var editor = RuntimeEditor.Instance;
            if (editor != null)
            {
                string rel = RuntimeAssetLoader.Instance.CopyMediaToScene(path, editor.CurrentSceneName);
                v.coverMediaFile = rel;
                needsRebuild = true;
            }
        });
        RuntimeUIHelper.Spacer(content);
    }

    // ── NPC Dialogue ──
    private void BuildNPCDialogueSection()
    {
        if (currentData.npcDialogue == null) currentData.npcDialogue = new NPCDialogueData();
        var nd = currentData.npcDialogue;

        RuntimeUIHelper.Section(content, "NPC 对话设置");
        RuntimeUIHelper.ToggleField(content, "自动触发", nd.autoTrigger, b => { nd.autoTrigger = b; needsRebuild = true; });
        RuntimeUIHelper.ToggleField(content, "循环对话", nd.loop, b => { nd.loop = b; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "触发距离", nd.triggerDistance, f => { nd.triggerDistance = f; needsRebuild = true; });
        RuntimeUIHelper.TextField(content, "对话键", nd.dialogueKey, s => { nd.dialogueKey = s; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "打字速度", nd.typeSpeed, f => { nd.typeSpeed = f; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "文字大小", nd.textSize, f => { nd.textSize = f; needsRebuild = true; });

        RuntimeUIHelper.Spacer(content, 4);
        BuildColorField("气泡颜色", nd.bubbleColor, arr => { nd.bubbleColor = arr; needsRebuild = true; });
        BuildColorField("文字颜色", nd.textColor, arr => { nd.textColor = arr; needsRebuild = true; });

        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.Section(content, "对话行");
        if (nd.lines != null)
        {
            for (int i = 0; i < nd.lines.Length; i++)
            {
                int idx = i;
                RuntimeUIHelper.TextField(content, $"行{i + 1} 文字", nd.lines[i].text, s => { nd.lines[idx].text = s; needsRebuild = true; });
                RuntimeUIHelper.FloatField(content, $"行{i + 1} 时长", nd.lines[i].duration, f => { nd.lines[idx].duration = f; needsRebuild = true; });
                RuntimeUIHelper.DropdownField(content, $"行{i + 1} 效果", TextEffectNames,
                    Mathf.Clamp(nd.lines[i].textEffect, 0, TextEffectNames.Length - 1), v => { nd.lines[idx].textEffect = v; needsRebuild = true; });
                RuntimeUIHelper.Btn(content, "删除行 " + (i + 1), () => { RemoveDialogueLine(nd, idx); ScheduleRebuild(); }, RuntimeUIHelper.AccentRed);
                RuntimeUIHelper.Spacer(content, 2);
            }
        }
        RuntimeUIHelper.Btn(content, "+ 添加对话行", () => { AddDialogueLine(nd); ScheduleRebuild(); });

        RuntimeUIHelper.Spacer(content, 6);
        RuntimeUIHelper.Section(content, "NPC 待机方向动画");
        RuntimeUIHelper.FloatField(content, "帧率 (FPS)", nd.idleAnimFps, f => { nd.idleAnimFps = Mathf.Max(0.1f, f); needsRebuild = true; });
        BuildNpcDirFrameRow("待机-上", nd.idleUpFiles, files => { nd.idleUpFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("待机-下", nd.idleDownFiles, files => { nd.idleDownFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("待机-左", nd.idleLeftFiles, files => { nd.idleLeftFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("待机-右", nd.idleRightFiles, files => { nd.idleRightFiles = files; needsRebuild = true; });

        RuntimeUIHelper.Spacer(content, 6);
        RuntimeUIHelper.Section(content, "NPC 跟随设置");
        RuntimeUIHelper.ToggleField(content, "可跟随玩家", nd.canFollow, b => { nd.canFollow = b; needsRebuild = true; ScheduleRebuild(); });
        if (nd.canFollow)
        {
            RuntimeUIHelper.FloatField(content, "跟随距离", nd.followDistance, f => { nd.followDistance = f; needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "跟随速度", nd.followSpeed, f => { nd.followSpeed = f; needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "记录间隔", nd.recordInterval, f => { nd.recordInterval = f; needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "行走帧率", nd.walkAnimFps, f => { nd.walkAnimFps = Mathf.Max(0.1f, f); needsRebuild = true; });

            RuntimeUIHelper.Spacer(content, 4);
            RuntimeUIHelper.Label(content, "NPC 行走方向动画", 12);
            BuildNpcDirFrameRow("行走-上", nd.walkUpFiles, files => { nd.walkUpFiles = files; needsRebuild = true; });
            BuildNpcDirFrameRow("行走-下", nd.walkDownFiles, files => { nd.walkDownFiles = files; needsRebuild = true; });
            BuildNpcDirFrameRow("行走-左", nd.walkLeftFiles, files => { nd.walkLeftFiles = files; needsRebuild = true; });
            BuildNpcDirFrameRow("行走-右", nd.walkRightFiles, files => { nd.walkRightFiles = files; needsRebuild = true; });
        }

        RuntimeUIHelper.Spacer(content);
    }

    private void AddDialogueLine(NPCDialogueData nd)
    {
        var list = nd.lines != null ? new System.Collections.Generic.List<DialogueLineData>(nd.lines) : new System.Collections.Generic.List<DialogueLineData>();
        list.Add(new DialogueLineData { text = "对话文字...", duration = 3f });
        nd.lines = list.ToArray();
        needsRebuild = true;
    }

    private void RemoveDialogueLine(NPCDialogueData nd, int index)
    {
        if (nd.lines == null || index < 0 || index >= nd.lines.Length) return;
        var list = new System.Collections.Generic.List<DialogueLineData>(nd.lines);
        list.RemoveAt(index);
        nd.lines = list.ToArray();
        needsRebuild = true;
    }

    // ── NPC Follower ──
    private void BuildNPCFollowerSection()
    {
        if (currentData.npcFollower == null) currentData.npcFollower = new NPCFollowerData();
        var nf = currentData.npcFollower;

        RuntimeUIHelper.Section(content, "跟随者设置");
        RuntimeUIHelper.FloatField(content, "跟随距离", nf.followDistance, f => { nf.followDistance = f; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "跟随速度", nf.followSpeed, f => { nf.followSpeed = f; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "记录间隔", nf.recordInterval, f => { nf.recordInterval = f; needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "动画帧率", nf.animFps, f => { nf.animFps = f; needsRebuild = true; });

        RuntimeUIHelper.Spacer(content, 6);
        RuntimeUIHelper.Section(content, "方向动画");
        BuildNpcDirFrameRow("行走-上", nf.walkUpFiles, files => { nf.walkUpFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("行走-下", nf.walkDownFiles, files => { nf.walkDownFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("行走-左", nf.walkLeftFiles, files => { nf.walkLeftFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("行走-右", nf.walkRightFiles, files => { nf.walkRightFiles = files; needsRebuild = true; });
        RuntimeUIHelper.Spacer(content, 2);
        BuildNpcDirFrameRow("待机-上", nf.idleUpFiles, files => { nf.idleUpFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("待机-下", nf.idleDownFiles, files => { nf.idleDownFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("待机-左", nf.idleLeftFiles, files => { nf.idleLeftFiles = files; needsRebuild = true; });
        BuildNpcDirFrameRow("待机-右", nf.idleRightFiles, files => { nf.idleRightFiles = files; needsRebuild = true; });

        RuntimeUIHelper.Spacer(content);
    }

    // ── Weather ──
    private static readonly string[] WeatherTypeNames = { "雨", "雪", "雾", "阳光", "萤火虫" };

    private void BuildWeatherSection()
    {
        if (currentData.weather == null) currentData.weather = new WeatherData();
        var w = currentData.weather;

        RuntimeUIHelper.Section(content, "粒子效果");
        RuntimeUIHelper.DropdownField(content, "天气类型", WeatherTypeNames,
            Mathf.Clamp(w.weatherType, 0, WeatherTypeNames.Length - 1),
            i => { w.weatherType = i; needsRebuild = true; });
        RuntimeUIHelper.IntField(content, "粒子数量", w.particleCount, i => { w.particleCount = Mathf.Max(1, i); needsRebuild = true; });
        RuntimeUIHelper.FloatField(content, "强度", w.intensity, f => { w.intensity = Mathf.Max(0.01f, f); needsRebuild = true; });
        BuildColorField("粒子颜色", w.particleColor, arr => { w.particleColor = arr; needsRebuild = true; });
        RuntimeUIHelper.Spacer(content);
    }

    // ── Interaction Effects ──

    private static readonly string[] WeatherNames = { "雨", "雪", "雾", "光束", "萤火虫" };
    private static readonly string[] TextEffectNames = { "打字机", "渐隐", "上滑", "下滑", "缩放", "闪烁", "波浪", "故障" };

    private void BuildInteractionSection()
    {
        RuntimeUIHelper.Spacer(content, 8);
        RuntimeUIHelper.Section(content, "按键交互");
        RuntimeUIHelper.ToggleField(content, "启用按键交互", currentData.enableKeyInteract, v =>
        {
            currentData.enableKeyInteract = v;
            if (v && currentData.keyEffects == null) currentData.keyEffects = new EffectData();
            needsRebuild = true;
            ScheduleRebuild();
        });
        if (currentData.enableKeyInteract)
        {
            RuntimeUIHelper.TextField(content, "交互按键", currentData.interactKey, s => { currentData.interactKey = s; needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "交互距离", currentData.interactDistance, f => { currentData.interactDistance = f; needsRebuild = true; });
            if (currentData.keyEffects == null) currentData.keyEffects = new EffectData();
            BuildEffectDataSection("按键效果", currentData.keyEffects);
        }

        RuntimeUIHelper.Spacer(content, 8);
        RuntimeUIHelper.Section(content, "靠近触发");
        RuntimeUIHelper.ToggleField(content, "启用靠近触发", currentData.enableApproachTrigger, v =>
        {
            currentData.enableApproachTrigger = v;
            if (v && currentData.approachEffects == null) currentData.approachEffects = new EffectData();
            needsRebuild = true;
            ScheduleRebuild();
        });
        if (currentData.enableApproachTrigger)
        {
            RuntimeUIHelper.FloatField(content, "触发距离", currentData.approachDistance, f => { currentData.approachDistance = f; needsRebuild = true; });
            RuntimeUIHelper.ToggleField(content, "仅触发一次", currentData.approachOnlyOnce, v => { currentData.approachOnlyOnce = v; needsRebuild = true; });
            if (currentData.approachEffects == null) currentData.approachEffects = new EffectData();
            BuildEffectDataSection("靠近效果", currentData.approachEffects);
        }
    }

    private void BuildEffectDataSection(string title, EffectData fx)
    {
        RuntimeUIHelper.Spacer(content, 4);
        RuntimeUIHelper.Label(content, "  ── " + title + " ──", 12).fontStyle = FontStyle.Italic;

        RuntimeUIHelper.ToggleField(content, "缩放查看", fx.zoom, v => { fx.zoom = v; needsRebuild = true; });

        RuntimeUIHelper.ToggleField(content, "显示文字", fx.showText, v => { fx.showText = v; needsRebuild = true; ScheduleRebuild(); });
        if (fx.showText)
        {
            RuntimeUIHelper.TextField(content, "文字内容", fx.text, s => { fx.text = s; needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "显示时长", fx.textDuration, f => { fx.textDuration = f; needsRebuild = true; });
            RuntimeUIHelper.DropdownField(content, "文字效果", TextEffectNames,
                Mathf.Clamp(fx.textEffect, 0, TextEffectNames.Length - 1), i => { fx.textEffect = i; needsRebuild = true; });
        }

        RuntimeUIHelper.ToggleField(content, "播放音效", fx.playSound, v => { fx.playSound = v; needsRebuild = true; ScheduleRebuild(); });
        if (fx.playSound)
        {
            RuntimeUIHelper.FloatField(content, "音效音量", fx.soundVolume, f => { fx.soundVolume = f; needsRebuild = true; });
            RuntimeUIHelper.Btn(content, "选择音效文件", () =>
            {
                string path = NativeFilePicker.PickMediaFile("选择音效");
                if (string.IsNullOrEmpty(path)) return;
                var editor = RuntimeEditor.Instance;
                if (editor != null)
                {
                    fx.soundFile = RuntimeAssetLoader.Instance.CopyMediaToScene(path, editor.CurrentSceneName);
                    needsRebuild = true;
                }
            });
            if (!string.IsNullOrEmpty(fx.soundFile))
                RuntimeUIHelper.Label(content, "  " + fx.soundFile, 10);
        }

        RuntimeUIHelper.ToggleField(content, "切换BGM", fx.changeBGM, v => { fx.changeBGM = v; needsRebuild = true; ScheduleRebuild(); });
        if (fx.changeBGM)
        {
            RuntimeUIHelper.FloatField(content, "BGM音量", fx.bgmVolume, f => { fx.bgmVolume = f; needsRebuild = true; });
            RuntimeUIHelper.Btn(content, "选择BGM文件", () =>
            {
                string path = NativeFilePicker.PickMediaFile("选择BGM");
                if (string.IsNullOrEmpty(path)) return;
                var editor = RuntimeEditor.Instance;
                if (editor != null)
                {
                    fx.bgmFile = RuntimeAssetLoader.Instance.CopyMediaToScene(path, editor.CurrentSceneName);
                    needsRebuild = true;
                }
            });
            if (!string.IsNullOrEmpty(fx.bgmFile))
                RuntimeUIHelper.Label(content, "  " + fx.bgmFile, 10);
        }

        RuntimeUIHelper.ToggleField(content, "改变天气", fx.changeWeather, v => { fx.changeWeather = v; needsRebuild = true; ScheduleRebuild(); });
        if (fx.changeWeather)
        {
            RuntimeUIHelper.DropdownField(content, "天气类型", WeatherNames, Mathf.Clamp(fx.weatherType, 0, WeatherNames.Length - 1), i =>
            {
                fx.weatherType = i;
                needsRebuild = true;
            });
            RuntimeUIHelper.IntField(content, "粒子数量", fx.weatherParticles, i => { fx.weatherParticles = i; needsRebuild = true; });
            BuildColorField("天气颜色", fx.weatherColor, arr => { fx.weatherColor = arr; needsRebuild = true; });
        }

        RuntimeUIHelper.ToggleField(content, "改变背景", fx.changeBackground, v => { fx.changeBackground = v; needsRebuild = true; ScheduleRebuild(); });
        if (fx.changeBackground)
        {
            BuildColorField("背景颜色", fx.backgroundColor, arr => { fx.backgroundColor = arr; needsRebuild = true; });
            RuntimeUIHelper.FloatField(content, "淡入时间", fx.backgroundFade, f => { fx.backgroundFade = f; needsRebuild = true; });
        }

        RuntimeUIHelper.ToggleField(content, "改变亮度", fx.changeBrightness, v => { fx.changeBrightness = v; needsRebuild = true; ScheduleRebuild(); });
        if (fx.changeBrightness)
            RuntimeUIHelper.FloatField(content, "亮度值", fx.brightness, f => { fx.brightness = f; needsRebuild = true; });

        RuntimeUIHelper.ToggleField(content, "加载场景", fx.loadScene, v => { fx.loadScene = v; needsRebuild = true; ScheduleRebuild(); });
        if (fx.loadScene)
        {
            string[] scenes = SceneDataHelper.ListScenes();
            if (scenes.Length > 0)
            {
                int curIdx = System.Array.IndexOf(scenes, fx.sceneName);
                if (curIdx < 0) curIdx = 0;
                RuntimeUIHelper.DropdownField(content, "目标场景", scenes, curIdx, i =>
                {
                    fx.sceneName = scenes[i];
                    needsRebuild = true;
                });
            }
            else
            {
                RuntimeUIHelper.TextField(content, "场景名", fx.sceneName, s => { fx.sceneName = s; needsRebuild = true; });
            }
        }

        RuntimeUIHelper.ToggleField(content, "开关物体", fx.toggleObject, v => { fx.toggleObject = v; needsRebuild = true; ScheduleRebuild(); });
        if (fx.toggleObject)
        {
            RuntimeUIHelper.TextField(content, "目标ID", fx.targetElementId, s => { fx.targetElementId = s; needsRebuild = true; });
            RuntimeUIHelper.ToggleField(content, "显示(开)", fx.objectShow, v => { fx.objectShow = v; needsRebuild = true; });
        }

        if (currentData != null && currentData.type == "npc_dialogue")
        {
            RuntimeUIHelper.ToggleField(content, "跟随玩家", fx.followPlayer, v => { fx.followPlayer = v; needsRebuild = true; });
        }
    }

    // ── Directional Frame Upload Helper ──
    private void BuildNpcDirFrameRow(string label, string[] currentFiles, System.Action<string[]> onChanged)
    {
        int count = currentFiles != null ? currentFiles.Length : 0;
        RuntimeUIHelper.Btn(content, label + " [" + count + "帧]  选择", () =>
        {
            string[] paths = NativeFilePicker.PickMultipleImageFiles("选择" + label + "帧（多选）");
            if (paths == null || paths.Length == 0) return;
            var editor = RuntimeEditor.Instance;
            if (editor == null) return;
            var relFiles = new string[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                relFiles[i] = RuntimeAssetLoader.Instance.CopyMediaToScene(paths[i], editor.CurrentSceneName);
            onChanged(relFiles);
            editor.SetStatus(label + ": 已设置 " + relFiles.Length + " 帧");
            ScheduleRebuild();
        }, count > 0 ? RuntimeUIHelper.AccentGreen : RuntimeUIHelper.BtnNormal);
    }

    // ── Color Picker (simple RGB) ──
    private static readonly Color[] PresetColors = {
        Color.white, Color.black, Color.red, Color.green, Color.blue,
        new Color(1f, 0.5f, 0f), Color.yellow, Color.cyan, Color.magenta,
        new Color(0.3f, 0.3f, 0.5f), new Color(0.05f, 0.05f, 0.1f),
        new Color(0.5f, 0.8f, 0.3f)
    };

    private void BuildColorField(string label, float[] color, System.Action<float[]> onChange)
    {
        if (color == null || color.Length < 3) color = new[] { 1f, 1f, 1f, 1f };
        float[] c = color;

        var go = new GameObject("Color_" + label);
        go.transform.SetParent(content, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 26);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childForceExpandHeight = true;
        go.AddComponent<LayoutElement>().minHeight = 26;

        var lbl = RuntimeUIHelper.Label(go.transform, label, 12);
        lbl.GetComponent<LayoutElement>().preferredWidth = 70;

        var preview = new GameObject("Preview");
        preview.transform.SetParent(go.transform, false);
        preview.AddComponent<RectTransform>();
        var pimg = preview.AddComponent<Image>();
        pimg.color = SceneDataHelper.ToColor(c);
        pimg.raycastTarget = false;
        preview.AddComponent<LayoutElement>().preferredWidth = 26;

        for (int pi = 0; pi < PresetColors.Length; pi++)
        {
            Color preset = PresetColors[pi];
            var cBtn = new GameObject("C");
            cBtn.transform.SetParent(go.transform, false);
            cBtn.AddComponent<RectTransform>();
            var cImg = cBtn.AddComponent<Image>();
            cImg.color = preset;
            cImg.raycastTarget = true;
            var btn = cBtn.AddComponent<Button>();
            btn.targetGraphic = cImg;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() =>
            {
                float[] arr = SceneDataHelper.FromColor(preset);
                pimg.color = preset;
                onChange?.Invoke(arr);
            });
            cBtn.AddComponent<LayoutElement>().preferredWidth = 14;
        }
    }
}

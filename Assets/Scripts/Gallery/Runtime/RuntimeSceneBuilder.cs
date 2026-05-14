using System.Collections.Generic;
using UnityEngine;

public class RuntimeSceneBuilder : MonoBehaviour
{
    private string currentSceneName;
    private List<GameObject> spawnedElements = new List<GameObject>();
    private Dictionary<string, GameObject> elementMap = new Dictionary<string, GameObject>();

    private static Material _sharedLineMat;
    private static Material GetSharedLineMaterial()
    {
        if (_sharedLineMat == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) _sharedLineMat = new Material(shader);
        }
        return _sharedLineMat;
    }

    public static RuntimeSceneBuilder Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private Coroutine loadRoutine;

    public void LoadScene(string sceneName)
    {
        if (loadRoutine != null) StopCoroutine(loadRoutine);
        ClearScene();
        currentSceneName = sceneName;

        string jsonPath = SceneDataHelper.GetSceneJsonPath(sceneName);
        SceneData data = SceneData.Load(jsonPath);
        if (data == null) { Debug.LogWarning($"Scene not found: {jsonPath}"); return; }

        ApplySettings(data.settings);
        loadRoutine = StartCoroutine(LoadElementsAsync(data));
    }

    private System.Collections.IEnumerator LoadElementsAsync(SceneData data)
    {
        float frameStart = Time.realtimeSinceStartup;
        const float BUDGET_MS = 8f;

        foreach (var elem in data.elements)
        {
            if (!elem.enabled) continue;
            GameObject go = SpawnElement(elem);
            if (go != null)
            {
                spawnedElements.Add(go);
                elementMap[elem.id] = go;
            }

            if ((Time.realtimeSinceStartup - frameStart) * 1000f > BUDGET_MS)
            {
                yield return null;
                frameStart = Time.realtimeSinceStartup;
            }
        }

        ResolveObjectReferences(data);
        loadRoutine = null;
    }

    public void ClearScene()
    {
        foreach (var go in spawnedElements)
            if (go != null) Destroy(go);
        spawnedElements.Clear();
        elementMap.Clear();
        RuntimeAssetLoader.Instance.ClearCache();
    }

    public string CurrentSceneName
    {
        get => currentSceneName;
        set => currentSceneName = value;
    }
    public Dictionary<string, GameObject> ElementMap => elementMap;

    public GameObject SpawnAndRegister(ElementData elem)
    {
        GameObject go = SpawnElement(elem);
        if (go != null)
        {
            spawnedElements.Add(go);
            elementMap[elem.id] = go;
        }
        return go;
    }

    public void RemoveElement(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (elementMap.TryGetValue(id, out var go))
        {
            spawnedElements.Remove(go);
            elementMap.Remove(id);
            if (go != null) Destroy(go);
        }
    }

    public void UpdateElement(ElementData elem)
    {
        if (elem == null) return;
        RemoveElement(elem.id);
        SpawnAndRegister(elem);
    }

    private GameObject backgroundGO;

    private void ApplySettings(SceneSettingsData s)
    {
        RenderSettings.ambientLight = SceneDataHelper.ToColor(s.ambientColor) * s.ambientBrightness;

        var bg = FindObjectOfType<GalleryBackground>();
        if (bg != null) bg.SetDefaultColor(SceneDataHelper.ToColor(s.backgroundColor));

        var player = GalleryPlayer.Instance;
        if (player != null) player.transform.position = new Vector3(s.playerStartX, s.playerStartY, 0);

        ApplyBackgroundImage(s);
        ApplyCameraSettings(s);
        ApplyTimeline(s);
        ApplyBlockSettingsManager(s);
    }

    public void ApplyBlockSettingsManager(SceneSettingsData s)
    {
        var cam = Camera.main;
        if (cam == null) return;

        var mgr = cam.GetComponent<BlockSettingsManager>();
        if (s.blockSettings == null || s.blockSettings.Length == 0)
        {
            if (mgr != null) Destroy(mgr);
            return;
        }

        if (mgr == null) mgr = cam.gameObject.AddComponent<BlockSettingsManager>();
        mgr.Init(s, currentSceneName);
    }

    public void ApplyBackgroundImage(SceneSettingsData s)
    {
        if (backgroundGO != null) Destroy(backgroundGO);
        if (string.IsNullOrEmpty(s.backgroundMediaFile)) return;

        Sprite sprite = RuntimeAssetLoader.Instance.LoadSpriteFromScene(currentSceneName, s.backgroundMediaFile);
        if (sprite == null) return;

        backgroundGO = new GameObject("SceneBackground");
        var sr = backgroundGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -1000;
        sr.color = Color.white;
        backgroundGO.transform.position = new Vector3(0, 0, 10f);
        backgroundGO.transform.localScale = new Vector3(s.backgroundScaleX, s.backgroundScaleY, 1f);
    }

    public void ApplyCameraSettings(SceneSettingsData s)
    {
        var cam = Camera.main;
        if (cam == null) return;

        bool hasBoundaries = s.cameraBoundaries != null && s.cameraBoundaries.Length > 0;
        bool hasBlocks = s.cameraBlockCount > 1;

        if (!hasBoundaries && !hasBlocks)
        {
            var existing = cam.GetComponent<GalleryCamera>();
            if (existing != null) Destroy(existing);
            return;
        }

        var gc = cam.GetComponent<GalleryCamera>();
        if (gc == null) gc = cam.gameObject.AddComponent<GalleryCamera>();

        if (hasBoundaries)
        {
            gc.SetBoundaries(s.cameraBoundaries, s.cameraTransitionSpeed, s.cameraY);
            s.cameraBlockCount = s.cameraBoundaries.Length + 1;
        }
        else
        {
            gc.SetParams(s.cameraBlockCount, s.cameraFirstBlockX, s.cameraBlockWidth, s.cameraTransitionSpeed, s.cameraY);
        }
    }

    private GameObject timelineGO;

    public void ApplyTimeline(SceneSettingsData s)
    {
        if (timelineGO != null) Destroy(timelineGO);
        if (s.timelinePoints == null || s.timelinePoints.Length == 0) return;

        timelineGO = new GameObject("RuntimeTimeline");
        var sprite = RuntimeSprite.GetCircle(16);
        Color lineColor = SceneDataHelper.ToColor(s.timelineLineColor);

        for (int i = 0; i < s.timelinePoints.Length - 1; i++)
        {
            var lineGO = new GameObject("TL_Line_" + i);
            lineGO.transform.SetParent(timelineGO.transform);
            var lr = lineGO.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(s.timelinePoints[i].x, s.timelinePoints[i].y, 0));
            lr.SetPosition(1, new Vector3(s.timelinePoints[i + 1].x, s.timelinePoints[i + 1].y, 0));
            lr.startWidth = s.timelineLineWidth;
            lr.endWidth = s.timelineLineWidth;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.sortingOrder = -2;
            lr.sharedMaterial = GetSharedLineMaterial();
            lr.useWorldSpace = true;
        }

        for (int i = 0; i < s.timelinePoints.Length; i++)
        {
            var pt = s.timelinePoints[i];
            Color c = SceneDataHelper.ToColor(pt.color);

            var markerGO = new GameObject("TL_Marker_" + i);
            markerGO.transform.SetParent(timelineGO.transform);
            markerGO.transform.position = new Vector3(pt.x, pt.y, 0);
            var sr = markerGO.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = c;
            sr.sortingOrder = -1;
            markerGO.transform.localScale = Vector3.one * s.timelineDotSize;

            if (!string.IsNullOrEmpty(pt.dateText))
            {
                var textGO = new GameObject("DateText");
                textGO.transform.SetParent(markerGO.transform);
                textGO.transform.localPosition = new Vector3(0, s.timelineDotSize * 3f, 0);
                textGO.transform.localScale = Vector3.one * (1f / s.timelineDotSize);
                var tm = textGO.AddComponent<TextMesh>();
                tm.text = pt.dateText;
                tm.characterSize = s.timelineTextSize;
                tm.fontSize = 60;
                tm.anchor = TextAnchor.LowerCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = c;
                textGO.GetComponent<MeshRenderer>().sortingOrder = 0;
            }
        }
    }

    private GameObject SpawnElement(ElementData elem)
    {
        switch (elem.type)
        {
            case "photo": return SpawnPhoto(elem);
            case "video": return SpawnVideo(elem);
            case "npc_dialogue": return SpawnNPCDialogue(elem);
            case "npc_follower": return SpawnNPCFollower(elem);
            case "weather": return SpawnWeather(elem);
            default:
                Debug.LogWarning($"Unknown element type: {elem.type}");
                return null;
        }
    }

    private GameObject SpawnPhoto(ElementData elem)
    {
        var go = new GameObject($"Photo_{elem.id}");
        go.transform.position = new Vector3(elem.x, elem.y, 0);
        go.transform.localScale = new Vector3(elem.scaleX, elem.scaleY, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, elem.rotation);

        var sr = go.AddComponent<SpriteRenderer>();
        BoxCollider2D col = null;
        if (elem.hasCollider)
            col = go.AddComponent<BoxCollider2D>();
        var frame = go.AddComponent<GalleryFrame>();

        Sprite sprite = null;
        if (!string.IsNullOrEmpty(elem.mediaFile))
        {
            sprite = RuntimeAssetLoader.Instance.LoadSpriteFromScene(currentSceneName, elem.mediaFile);
        }

        if (sprite == null)
        {
            sr.sprite = RuntimeSprite.Get();
            sr.color = new Color(0.3f, 0.5f, 0.8f, 0.8f);
            go.transform.localScale = new Vector3(
                Mathf.Max(elem.scaleX, 2f),
                Mathf.Max(elem.scaleY, 2f), 1f);
        }
        frame.SwapImage(sprite, elem.caption);

        if (col != null && sr.sprite != null)
            col.size = sr.sprite.bounds.size;
        frame.ElementId = elem.id;
        frame.SetSortingOrder(elem.sortingOrder);

        if (elem.photo != null)
            frame.SetFadeIn(elem.photo.fadeInOnApproach, elem.photo.fadeDistance, elem.photo.fadeSpeed);

        if (elem.photo != null && !string.IsNullOrEmpty(elem.caption))
            frame.SetCaption(elem.caption, SceneDataHelper.ToColor(elem.photo.captionColor), elem.photo.captionSize);

        if (elem.photo != null && elem.photo.hasWeather)
            SpawnPhotoWeather(go, elem);

        ApplyInteraction(frame, elem);
        return go;
    }

    private void SpawnPhotoWeather(GameObject parent, ElementData elem)
    {
        var p = elem.photo;
        var weatherGO = new GameObject("PhotoWeather");
        weatherGO.transform.SetParent(parent.transform, false);
        weatherGO.transform.localPosition = Vector3.zero;

        // Use world-space scale so weather doesn't stretch with the image
        var scaleComp = weatherGO.AddComponent<WorldScaleKeeper>();
        scaleComp.targetScale = new Vector3(1f, 1f, 1f);

        var weatherCol = weatherGO.AddComponent<BoxCollider2D>();
        weatherCol.isTrigger = true;
        weatherCol.size = new Vector2(p.weatherSizeX, p.weatherSizeY);

        var weather = weatherGO.AddComponent<GalleryWeather>();
        weather.SetWeather(
            (GalleryWeather.WeatherType)p.weatherType,
            p.weatherParticles,
            SceneDataHelper.ToColor(p.weatherColor));
    }

    private GameObject SpawnVideo(ElementData elem)
    {
        var go = new GameObject($"Video_{elem.id}");
        go.transform.position = new Vector3(elem.x, elem.y, 0);
        go.transform.localScale = new Vector3(elem.scaleX, elem.scaleY, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, elem.rotation);

        go.AddComponent<SpriteRenderer>();
        if (elem.hasCollider)
            go.AddComponent<BoxCollider2D>();
        var video = go.AddComponent<GalleryVideo>();
        video.ElementId = elem.id;

        string videoUrl = RuntimeAssetLoader.Instance.GetVideoUrl(currentSceneName, elem.mediaFile);
        if (!string.IsNullOrEmpty(videoUrl))
            video.SetVideoUrl(videoUrl);

        if (elem.video != null)
        {
            video.SetAutoPlay(elem.video.autoPlay);
            video.SetLoop(elem.video.loop);
            video.SetAudio(elem.video.enableAudio, elem.video.maxVolume, elem.video.audioRange);
            video.SetFadeIn(elem.video.fadeInOnApproach, elem.video.fadeDistance, elem.video.fadeSpeed);
            video.SetTriggerRange(elem.video.triggerRange);

            if (!string.IsNullOrEmpty(elem.video.coverMediaFile))
            {
                Sprite cover = RuntimeAssetLoader.Instance.LoadSpriteFromScene(currentSceneName, elem.video.coverMediaFile);
                video.SetCoverImage(cover);
            }
        }

        if (elem.enableKeyInteract)
            video.SetKeyEffects(true, ParseKeyCode(elem.interactKey), elem.interactDistance, BuildEffectSet(elem.keyEffects));
        if (elem.enableApproachTrigger)
            video.SetApproachEffects(true, elem.approachDistance, elem.approachOnlyOnce, BuildEffectSet(elem.approachEffects));

        return go;
    }

    private GameObject SpawnNPCDialogue(ElementData elem)
    {
        var go = new GameObject($"NPC_{elem.id}");
        go.transform.position = new Vector3(elem.x, elem.y, 0);
        go.transform.localScale = new Vector3(elem.scaleX, elem.scaleY, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        Sprite sprite = RuntimeAssetLoader.Instance.LoadSpriteFromScene(currentSceneName, elem.mediaFile);
        if (sprite != null) sr.sprite = sprite;
        else sr.sprite = RuntimeSprite.Get();
        sr.sortingOrder = elem.sortingOrder;

        var npc = go.AddComponent<GalleryNPCDialogue>();
        npc.ElementId = elem.id;

        if (elem.npcDialogue != null)
        {
            var nd = elem.npcDialogue;
            GalleryNPCDialogue.DialogueLine[] dl = null;
            if (nd.lines != null && nd.lines.Length > 0)
            {
                dl = new GalleryNPCDialogue.DialogueLine[nd.lines.Length];
                for (int i = 0; i < nd.lines.Length; i++)
                    dl[i] = new GalleryNPCDialogue.DialogueLine { text = nd.lines[i].text, duration = nd.lines[i].duration };
            }
            npc.SetDialogueLines(dl);
            npc.SetDialogue(nd.autoTrigger, nd.triggerDistance, ParseKeyCode(nd.dialogueKey), nd.loop);
            npc.SetBubbleStyle(SceneDataHelper.ToColor(nd.bubbleColor), SceneDataHelper.ToColor(nd.textColor), nd.textSize, nd.typeSpeed);
        }

        if (elem.enableKeyInteract)
            npc.SetKeyEffects(true, ParseKeyCode(elem.interactKey), elem.interactDistance, BuildEffectSet(elem.keyEffects));
        if (elem.enableApproachTrigger)
            npc.SetApproachEffects(true, elem.approachDistance, elem.approachOnlyOnce, BuildEffectSet(elem.approachEffects));

        return go;
    }

    private GameObject SpawnNPCFollower(ElementData elem)
    {
        var go = new GameObject($"Follower_{elem.id}");
        go.transform.position = new Vector3(elem.x, elem.y, 0);
        go.transform.localScale = new Vector3(elem.scaleX, elem.scaleY, 1f);

        go.AddComponent<SpriteRenderer>();
        go.AddComponent<CircleCollider2D>();
        var follower = go.AddComponent<GalleryFollower>();
        follower.ElementId = elem.id;

        Sprite sprite = RuntimeAssetLoader.Instance.LoadSpriteFromScene(currentSceneName, elem.mediaFile);
        if (sprite != null) follower.SetSprite(sprite);

        if (elem.npcFollower != null)
        {
            var nf = elem.npcFollower;
            follower.SetFollowParams(nf.followDistance, nf.followSpeed, nf.recordInterval);
            if (nf.walkFrameFiles != null && nf.walkFrameFiles.Length > 0)
            {
                Sprite[] frames = RuntimeAssetLoader.Instance.LoadSpriteArray(currentSceneName, nf.walkFrameFiles);
                follower.SetWalkFrames(frames, nf.animFps);
            }
        }

        return go;
    }

    private GameObject SpawnWeather(ElementData elem)
    {
        var go = new GameObject($"Weather_{elem.id}");
        go.transform.position = new Vector3(elem.x, elem.y, 0);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (elem.weather != null)
            col.size = new Vector2(elem.weather.sizeX, elem.weather.sizeY);

        var weather = go.AddComponent<GalleryWeather>();
        if (elem.weather != null)
        {
            weather.SetWeather(
                (GalleryWeather.WeatherType)elem.weather.weatherType,
                elem.weather.particleCount,
                SceneDataHelper.ToColor(elem.weather.particleColor));
        }

        return go;
    }

    private void ApplyInteraction(GalleryFrame frame, ElementData elem)
    {
        if (elem.enableKeyInteract)
            frame.SetKeyInteract(true, ParseKeyCode(elem.interactKey), elem.interactDistance, BuildEffectSet(elem.keyEffects));
        if (elem.enableApproachTrigger)
            frame.SetApproachTrigger(true, elem.approachDistance, elem.approachOnlyOnce, BuildEffectSet(elem.approachEffects));
    }

    private FrameEffectSet BuildEffectSet(EffectData data)
    {
        if (data == null) return new FrameEffectSet();
        var fx = new FrameEffectSet();
        fx.zoom = data.zoom;
        fx.showText = data.showText;
        fx.text = data.text;
        fx.textDuration = data.textDuration;
        fx.playSound = data.playSound;
        fx.soundVolume = data.soundVolume;
        fx.changeBGM = data.changeBGM;
        fx.bgmVolume = data.bgmVolume;
        fx.changeWeather = data.changeWeather;
        fx.weatherType = (GalleryWeather.WeatherType)data.weatherType;
        fx.weatherParticles = data.weatherParticles;
        fx.weatherColor = SceneDataHelper.ToColor(data.weatherColor);
        fx.changeBackground = data.changeBackground;
        fx.backgroundColor = SceneDataHelper.ToColor(data.backgroundColor);
        fx.backgroundFade = data.backgroundFade;
        fx.changeBrightness = data.changeBrightness;
        fx.brightness = data.brightness;
        fx.loadScene = data.loadScene;
        fx.sceneName = data.sceneName;
        fx.toggleObject = data.toggleObject;
        fx.objectShow = data.objectShow;
        return fx;
    }

    private void ResolveObjectReferences(SceneData data)
    {
        foreach (var elem in data.elements)
        {
            if (!elem.enabled) continue;
            if (!elementMap.ContainsKey(elem.id)) continue;

            if (elem.keyEffects != null && elem.keyEffects.toggleObject && !string.IsNullOrEmpty(elem.keyEffects.targetElementId))
            {
                if (elementMap.TryGetValue(elem.keyEffects.targetElementId, out var target))
                {
                    var frame = elementMap[elem.id].GetComponent<GalleryFrame>();
                    if (frame != null) frame.GetKeyEffects().targetObject = target;
                }
            }
            if (elem.approachEffects != null && elem.approachEffects.toggleObject && !string.IsNullOrEmpty(elem.approachEffects.targetElementId))
            {
                if (elementMap.TryGetValue(elem.approachEffects.targetElementId, out var target))
                {
                    var frame = elementMap[elem.id].GetComponent<GalleryFrame>();
                    if (frame != null) frame.GetApproachEffects().targetObject = target;
                }
            }
        }
    }

    private KeyCode ParseKeyCode(string key)
    {
        if (string.IsNullOrEmpty(key)) return KeyCode.E;
        if (System.Enum.TryParse<KeyCode>(key, true, out var result)) return result;
        if (key.Length == 1) return (KeyCode)System.Enum.Parse(typeof(KeyCode), key.ToUpper());
        return KeyCode.E;
    }
}

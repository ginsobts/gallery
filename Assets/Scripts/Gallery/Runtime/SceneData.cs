using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SceneData
{
    public string sceneName = "Untitled";
    public SceneSettingsData settings = new SceneSettingsData();
    public List<ElementData> elements = new List<ElementData>();

    public string ToJson() => JsonUtility.ToJson(this, true);

    public static SceneData FromJson(string json) => JsonUtility.FromJson<SceneData>(json);

    public static SceneData Load(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        string json = File.ReadAllText(filePath);
        return FromJson(json);
    }

    public void Save(string filePath)
    {
        string dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, ToJson());
    }
}

[Serializable]
public class SceneSettingsData
{
    public float ambientBrightness = 0.3f;
    public float[] ambientColor = { 0.5f, 0.5f, 0.6f, 1f };
    public float[] backgroundColor = { 0.05f, 0.05f, 0.1f, 1f };
    public float playerStartX = 0f;
    public float playerStartY = 0f;
    public string playerMediaFile = "";

    // Player directional animation
    public string[] playerWalkUpFiles;
    public string[] playerWalkDownFiles;
    public string[] playerWalkLeftFiles;
    public string[] playerWalkRightFiles;
    public string[] playerIdleUpFiles;
    public string[] playerIdleDownFiles;
    public string[] playerIdleLeftFiles;
    public string[] playerIdleRightFiles;
    public float playerAnimFps = 6f;
    public float groundWidth = 40f;
    public float groundHeight = 12f;
    public string groundMediaFile = "";
    public string backgroundMediaFile = "";
    public float backgroundScaleX = 20f;
    public float backgroundScaleY = 12f;
    public float backgroundX = 0f;
    public float backgroundY = 0f;

    // Camera block settings
    public int cameraBlockCount = 1;
    public float cameraFirstBlockX = 0f;
    public float cameraBlockWidth = 0f;
    public float cameraTransitionSpeed = 4f;
    public float cameraY = 0f;
    public float[] cameraBoundaries;

    // Per-block settings
    public BlockSettingsData[] blockSettings;

    // Transition types (0=Cut, 1=Fade, 2=Lerp)
    public int backgroundTransition = 1;
    public int lightingTransition = 2;
    public int weatherTransition = 1;
    public int filterTransition = 0;
    public int bgmTransition = 1;
    public float transitionDuration = 1.5f;

    // Timeline
    public TimelinePointData[] timelinePoints;
    public float[] timelineLineColor = { 0.6f, 0.6f, 0.7f, 0.4f };
    public float timelineLineWidth = 0.05f;
    public float timelineDotSize = 0.2f;
    public float timelineTextSize = 0.08f;

    public BlockSettingsData GetBlockSettings(int blockIndex)
    {
        if (blockSettings != null)
        {
            for (int i = 0; i < blockSettings.Length; i++)
                if (blockSettings[i].blockIndex == blockIndex) return blockSettings[i];
        }
        return new BlockSettingsData
        {
            blockIndex = blockIndex,
            backgroundMediaFile = backgroundMediaFile,
            backgroundScaleX = backgroundScaleX,
            backgroundScaleY = backgroundScaleY,
            ambientBrightness = ambientBrightness,
            ambientColor = ambientColor != null ? (float[])ambientColor.Clone() : new float[] { 0.5f, 0.5f, 0.6f, 1f },
            bgColor = backgroundColor != null ? (float[])backgroundColor.Clone() : new float[] { 0.05f, 0.05f, 0.1f, 1f }
        };
    }
}

public enum BlockTransitionType { Cut, Fade, Lerp }

[Serializable]
public class BlockSettingsData
{
    public int blockIndex;

    // Background
    public string backgroundMediaFile = "";
    public float backgroundScaleX = 20f;
    public float backgroundScaleY = 12f;

    // Lighting
    public float ambientBrightness = 0.3f;
    public float[] ambientColor = { 0.5f, 0.5f, 0.6f, 1f };
    public float[] bgColor = { 0.05f, 0.05f, 0.1f, 1f };

    // Weather
    public bool weatherEnabled = false;
    public int weatherType = 1;
    public int weatherParticles = 60;
    public float[] weatherColor = { 1f, 1f, 1f, 1f };

    // Filter
    public int colorFilter = 0;
    public float colorFilterIntensity = 0.7f;
    public int artisticStyle = 0;
    public float artisticIntensity = 0.85f;

    // BGM
    public string bgmFile = "";
    public float bgmVolume = 0.4f;
    public float bgmFadeTime = 1.5f;

    // NPC followers
    public bool dismissFollowers = false;
}

[Serializable]
public class TimelinePointData
{
    public float x, y;
    public string dateText = "";
    public float[] color = { 0.9f, 0.7f, 0.2f, 1f };
}

[Serializable]
public class ElementData
{
    public string id;
    public string type; // "photo", "video", "npc_dialogue", "npc_follower", "weather", "text"
    public float x, y;
    public float scaleX = 1f, scaleY = 1f;
    public float rotation = 0f;
    public int sortingOrder = 0;
    public string mediaFile = "";
    public string caption = "";
    public bool enabled = true;
    public bool hasCollider = true;
    public bool mediaFitted = false;

    // Physics
    public bool pushable = false;
    public float pushFriction = 5f;

    // Photo-specific
    public PhotoData photo;
    // Video-specific
    public VideoData video;
    // NPC Dialogue-specific
    public NPCDialogueData npcDialogue;
    // NPC Follower-specific
    public NPCFollowerData npcFollower;
    // Weather-specific
    public WeatherData weather;

    // Interaction effects
    public bool enableKeyInteract = false;
    public string interactKey = "E";
    public float interactDistance = 3f;
    public EffectData keyEffects;

    public bool enableApproachTrigger = false;
    public float approachDistance = 4f;
    public bool approachOnlyOnce = true;
    public EffectData approachEffects;

    public static ElementData CreateNew(string elementType)
    {
        return new ElementData
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 8),
            type = elementType,
            photo = elementType == "photo" ? new PhotoData() : null,
            video = elementType == "video" ? new VideoData() : null,
            npcDialogue = elementType == "npc_dialogue" ? new NPCDialogueData() : null,
            npcFollower = elementType == "npc_follower" ? new NPCFollowerData() : null,
            weather = elementType == "weather" ? new WeatherData() : null,
        };
    }
}

[Serializable]
public class PhotoData
{
    public bool fadeInOnApproach = false;
    public float fadeDistance = 5f;
    public float fadeSpeed = 2f;
    public float captionSize = 0.15f;
    public float[] captionColor = { 0.9f, 0.9f, 0.9f, 0.8f };

    public bool hasWeather = false;
    public int weatherType = 1; // 0=Rain,1=Snow,2=Fog,3=Sunbeam,4=Fireflies
    public int weatherParticles = 40;
    public float[] weatherColor = { 1f, 1f, 1f, 1f };
    public float weatherSizeX = 4f;
    public float weatherSizeY = 3f;
}

[Serializable]
public class VideoData
{
    public bool autoPlay = true;
    public bool loop = true;
    public string playKey = "E";
    public float triggerRange = 3f;
    public bool fadeInOnApproach = false;
    public float fadeDistance = 6f;
    public float fadeSpeed = 2f;
    public bool enableAudio = true;
    public float maxVolume = 0.5f;
    public float audioRange = 5f;
    public string coverMediaFile = "";
}

[Serializable]
public class NPCDialogueData
{
    public DialogueLineData[] lines;
    public bool loop = false;
    public bool autoTrigger = true;
    public float triggerDistance = 2f;
    public string dialogueKey = "E";
    public float[] bubbleColor = { 0.95f, 0.95f, 0.95f, 0.9f };
    public float[] textColor = { 0.1f, 0.1f, 0.1f, 1f };
    public float textSize = 0.06f;
    public float typeSpeed = 15f;

    // Follow player
    public bool canFollow = false;
    public float followDistance = 1.5f;
    public float followSpeed = 3f;
    public float recordInterval = 0.1f;

    // Directional walk animation (for following)
    public string[] walkUpFiles;
    public string[] walkDownFiles;
    public string[] walkLeftFiles;
    public string[] walkRightFiles;
    public float walkAnimFps = 6f;

    // Directional idle animation
    public string[] idleUpFiles;
    public string[] idleDownFiles;
    public string[] idleLeftFiles;
    public string[] idleRightFiles;
    public float idleAnimFps = 4f;
}

[Serializable]
public class DialogueLineData
{
    public string text = "";
    public float duration = 2f;
    public int textEffect = 0;
}

[Serializable]
public class NPCFollowerData
{
    public float followDistance = -1f;
    public float followSpeed = -1f;
    public float recordInterval = 0.1f;
    public string[] walkFrameFiles; // legacy, treated as walkDown
    public float animFps = 6f;

    // Directional walk/idle animation
    public string[] walkUpFiles;
    public string[] walkDownFiles;
    public string[] walkLeftFiles;
    public string[] walkRightFiles;
    public string[] idleUpFiles;
    public string[] idleDownFiles;
    public string[] idleLeftFiles;
    public string[] idleRightFiles;
}

[Serializable]
public class WeatherData
{
    public int weatherType = 0; // maps to GalleryWeather.WeatherType enum
    public int particleCount = 60;
    public float[] particleColor = { 1f, 1f, 1f, 1f };
    public float intensity = 1f;
    public float sizeX = 10f, sizeY = 6f;
}

[Serializable]
public class EffectData
{
    public bool zoom = false;
    public bool showText = false;
    public string text = "";
    public float textDuration = 4f;
    public int textEffect = 0;
    public bool playSound = false;
    public string soundFile = "";
    public float soundVolume = 0.8f;
    public bool changeBGM = false;
    public string bgmFile = "";
    public float bgmVolume = 0.6f;
    public bool changeWeather = false;
    public int weatherType = 1;
    public int weatherParticles = 60;
    public float[] weatherColor = { 1f, 1f, 1f, 1f };
    public bool changeBackground = false;
    public float[] backgroundColor = { 0.05f, 0.05f, 0.1f, 1f };
    public float backgroundFade = 1.5f;
    public bool changeBrightness = false;
    public float brightness = 0.5f;
    public bool loadScene = false;
    public string sceneName = "";
    public bool toggleObject = false;
    public string targetElementId = "";
    public bool objectShow = true;
    public bool followPlayer = false;
}

public static class SceneDataHelper
{
    public static string GetScenesRootPath()
    {
        string path = Path.Combine(Application.persistentDataPath, "Gallery", "scenes");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    public static string GetScenePath(string sceneName)
    {
        return Path.Combine(GetScenesRootPath(), sceneName);
    }

    public static string GetSceneJsonPath(string sceneName)
    {
        return Path.Combine(GetScenePath(sceneName), "scene.json");
    }

    public static string GetMediaPath(string sceneName)
    {
        string path = Path.Combine(GetScenePath(sceneName), "media");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    public static string[] ListScenes()
    {
        string root = GetScenesRootPath();
        if (!Directory.Exists(root)) return new string[0];
        var dirs = Directory.GetDirectories(root);
        var names = new List<string>();
        foreach (var d in dirs)
        {
            string jsonPath = Path.Combine(d, "scene.json");
            if (File.Exists(jsonPath))
                names.Add(Path.GetFileName(d));
        }
        return names.ToArray();
    }

    public static Color ToColor(float[] arr)
    {
        if (arr == null || arr.Length < 3) return Color.white;
        return new Color(arr[0], arr[1], arr[2], arr.Length > 3 ? arr[3] : 1f);
    }

    public static float[] FromColor(Color c)
    {
        return new[] { c.r, c.g, c.b, c.a };
    }
}

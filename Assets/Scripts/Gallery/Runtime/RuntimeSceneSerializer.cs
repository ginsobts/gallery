using System.Collections.Generic;
using UnityEngine;

public static class RuntimeSceneSerializer
{
    public static SceneData SerializeCurrentScene(string sceneName, Dictionary<GameObject, ElementData> goToData)
    {
        SceneData scene = new SceneData { sceneName = sceneName };
        scene.settings = SerializeSettings();
        scene.elements = new List<ElementData>();

        foreach (var kv in goToData)
        {
            if (kv.Key == null) continue;
            ElementData elem = kv.Value;
            elem.x = kv.Key.transform.position.x;
            elem.y = kv.Key.transform.position.y;
            elem.scaleX = kv.Key.transform.localScale.x;
            elem.scaleY = kv.Key.transform.localScale.y;
            elem.rotation = kv.Key.transform.rotation.eulerAngles.z;
            scene.elements.Add(elem);
        }

        return scene;
    }

    public static SceneSettingsData SerializeSettings()
    {
        var s = new SceneSettingsData();
        s.ambientBrightness = RenderSettings.ambientLight.maxColorComponent;
        s.ambientColor = SceneDataHelper.FromColor(RenderSettings.ambientLight);

        var player = GalleryPlayer.Instance;
        if (player != null)
        {
            s.playerStartX = player.transform.position.x;
            s.playerStartY = player.transform.position.y;
        }

        var bg = Object.FindObjectOfType<GalleryBackground>();
        if (bg != null)
        {
            var sr = bg.GetComponent<SpriteRenderer>();
            if (sr != null) s.backgroundColor = SceneDataHelper.FromColor(sr.color);
        }

        return s;
    }

    public static ElementData SerializeFrame(GalleryFrame frame)
    {
        var elem = new ElementData
        {
            id = frame.ElementId ?? System.Guid.NewGuid().ToString("N").Substring(0, 8),
            type = "photo",
            x = frame.transform.position.x,
            y = frame.transform.position.y,
            scaleX = frame.transform.localScale.x,
            scaleY = frame.transform.localScale.y,
            rotation = frame.transform.rotation.eulerAngles.z,
            caption = frame.GetCaption(),
            photo = new PhotoData(),
            enableKeyInteract = frame.IsKeyInteractEnabled,
            interactKey = frame.GetInteractKey().ToString(),
            interactDistance = frame.GetInteractDistance(),
            enableApproachTrigger = frame.IsApproachTriggerEnabled,
            approachDistance = frame.GetApproachDistance(),
            approachOnlyOnce = frame.GetApproachOnlyOnce(),
        };

        if (frame.IsKeyInteractEnabled)
            elem.keyEffects = SerializeEffectSet(frame.GetKeyEffects());
        if (frame.IsApproachTriggerEnabled)
            elem.approachEffects = SerializeEffectSet(frame.GetApproachEffects());

        return elem;
    }

    public static EffectData SerializeEffectSet(FrameEffectSet fx)
    {
        if (fx == null) return null;
        return new EffectData
        {
            zoom = fx.zoom,
            showText = fx.showText,
            text = fx.text,
            textDuration = fx.textDuration,
            playSound = fx.playSound,
            soundVolume = fx.soundVolume,
            changeBGM = fx.changeBGM,
            bgmVolume = fx.bgmVolume,
            changeWeather = fx.changeWeather,
            weatherType = (int)fx.weatherType,
            weatherParticles = fx.weatherParticles,
            weatherColor = SceneDataHelper.FromColor(fx.weatherColor),
            changeBackground = fx.changeBackground,
            backgroundColor = SceneDataHelper.FromColor(fx.backgroundColor),
            backgroundFade = fx.backgroundFade,
            changeBrightness = fx.changeBrightness,
            brightness = fx.brightness,
            loadScene = fx.loadScene,
            sceneName = fx.sceneName,
            toggleObject = fx.toggleObject,
            objectShow = fx.objectShow,
        };
    }
}

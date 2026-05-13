using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GalleryVideo))]
public class GalleryVideoEditor : Editor
{
    private SerializedProperty videoClip, videoPath;
    private SerializedProperty autoPlay, loop, playKey, triggerRange;
    private SerializedProperty fadeInOnApproach, fadeDistance, fadeSpeed;
    private SerializedProperty enableAudio, maxVolume, audioRange;
    private SerializedProperty coverImage;
    private SerializedProperty enableKeyEffects, effectKey, effectKeyDistance, keyEffects;
    private SerializedProperty enableApproachEffects, approachEffectDistance, approachEffectOnlyOnce, approachEffects;

    private void OnEnable()
    {
        videoClip = serializedObject.FindProperty("videoClip");
        videoPath = serializedObject.FindProperty("videoPath");
        autoPlay = serializedObject.FindProperty("autoPlay");
        loop = serializedObject.FindProperty("loop");
        playKey = serializedObject.FindProperty("playKey");
        triggerRange = serializedObject.FindProperty("triggerRange");
        fadeInOnApproach = serializedObject.FindProperty("fadeInOnApproach");
        fadeDistance = serializedObject.FindProperty("fadeDistance");
        fadeSpeed = serializedObject.FindProperty("fadeSpeed");
        enableAudio = serializedObject.FindProperty("enableAudio");
        maxVolume = serializedObject.FindProperty("maxVolume");
        audioRange = serializedObject.FindProperty("audioRange");
        coverImage = serializedObject.FindProperty("coverImage");
        enableKeyEffects = serializedObject.FindProperty("enableKeyEffects");
        effectKey = serializedObject.FindProperty("effectKey");
        effectKeyDistance = serializedObject.FindProperty("effectKeyDistance");
        keyEffects = serializedObject.FindProperty("keyEffects");
        enableApproachEffects = serializedObject.FindProperty("enableApproachEffects");
        approachEffectDistance = serializedObject.FindProperty("approachEffectDistance");
        approachEffectOnlyOnce = serializedObject.FindProperty("approachEffectOnlyOnce");
        approachEffects = serializedObject.FindProperty("approachEffects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("视频设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(videoClip, new GUIContent("视频文件"));
        EditorGUILayout.PropertyField(videoPath, new GUIContent("视频路径 (StreamingAssets)"));

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("播放设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(autoPlay, new GUIContent("自动播放"));
        EditorGUILayout.PropertyField(loop, new GUIContent("循环播放"));
        if (!autoPlay.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(playKey, new GUIContent("播放按键"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.PropertyField(triggerRange, new GUIContent("播放触发距离"));

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("淡入触发", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fadeInOnApproach, new GUIContent("走近才出现"));
        if (fadeInOnApproach.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fadeDistance, new GUIContent("触发距离"));
            EditorGUILayout.PropertyField(fadeSpeed, new GUIContent("淡入速度"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("声音", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableAudio, new GUIContent("播放声音"));
        if (enableAudio.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(maxVolume, new GUIContent("最大音量"));
            EditorGUILayout.PropertyField(audioRange, new GUIContent("声音范围"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("外观", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(coverImage, new GUIContent("封面图"));

        EditorGUILayout.Space(12);
        DrawUILine(new Color(0.4f, 0.8f, 0.4f));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("按键交互效果", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableKeyEffects, new GUIContent("☑ 启用按键交互"));

        if (enableKeyEffects.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(effectKey, new GUIContent("交互按键"));
            EditorGUILayout.PropertyField(effectKeyDistance, new GUIContent("交互距离"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
            DrawEffectSet(keyEffects, "按键触发效果");
        }

        EditorGUILayout.Space(12);
        DrawUILine(new Color(0.4f, 0.6f, 1f));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("靠近触发效果", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableApproachEffects, new GUIContent("☑ 启用靠近触发"));

        if (enableApproachEffects.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(approachEffectDistance, new GUIContent("触发距离"));
            EditorGUILayout.PropertyField(approachEffectOnlyOnce, new GUIContent("只触发一次"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
            DrawEffectSet(approachEffects, "靠近触发效果");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEffectSet(SerializedProperty fx, string label)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
        EditorGUILayout.Space(2);

        DrawEffect("放大查看画面", fx.FindPropertyRelative("zoom"), null);

        DrawEffect("显示文字", fx.FindPropertyRelative("showText"), () =>
        {
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("text"), new GUIContent("文字内容"));
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("textDuration"), new GUIContent("持续时间"));
        });

        DrawEffect("播放音效", fx.FindPropertyRelative("playSound"), () =>
        {
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("soundClip"), new GUIContent("音效"));
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("soundVolume"), new GUIContent("音量"));
        });

        DrawEffect("切换BGM", fx.FindPropertyRelative("changeBGM"), () =>
        {
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("bgmClip"), new GUIContent("BGM"));
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("bgmVolume"), new GUIContent("音量"));
        });

        DrawEffect("改变天气", fx.FindPropertyRelative("changeWeather"), () =>
        {
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("weatherType"), new GUIContent("天气类型"));
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("weatherParticles"), new GUIContent("粒子数"));
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("weatherColor"), new GUIContent("粒子颜色"));
        });

        DrawEffect("改变背景颜色", fx.FindPropertyRelative("changeBackground"), () =>
        {
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("backgroundColor"), new GUIContent("目标颜色"));
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("backgroundFade"), new GUIContent("过渡时间"));
        });

        DrawEffect("改变灯光亮度", fx.FindPropertyRelative("changeBrightness"), () =>
        {
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("brightness"), new GUIContent("目标亮度"));
        });

        DrawEffect("跳转场景", fx.FindPropertyRelative("loadScene"), () =>
        {
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("sceneName"), new GUIContent("场景名"));
        });

        DrawEffect("显示/隐藏物体", fx.FindPropertyRelative("toggleObject"), () =>
        {
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("targetObject"), new GUIContent("目标物体"));
            EditorGUILayout.PropertyField(fx.FindPropertyRelative("objectShow"), new GUIContent("设为显示"));
        });

        EditorGUILayout.EndVertical();
    }

    private void DrawEffect(string label, SerializedProperty toggle, System.Action drawParams)
    {
        EditorGUILayout.BeginHorizontal();
        toggle.boolValue = EditorGUILayout.ToggleLeft(label, toggle.boolValue, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        if (toggle.boolValue && drawParams != null)
        {
            EditorGUI.indentLevel += 2;
            drawParams();
            EditorGUI.indentLevel -= 2;
            EditorGUILayout.Space(2);
        }
    }

    private static void DrawUILine(Color color, int thickness = 2, int padding = 4)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding * 0.5f;
        EditorGUI.DrawRect(r, color);
    }
}

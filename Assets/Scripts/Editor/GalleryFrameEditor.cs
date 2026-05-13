using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GalleryFrame))]
public class GalleryFrameEditor : Editor
{
    private SerializedProperty image, sortingOrder;
    private SerializedProperty fadeInOnApproach, fadeDistance, fadeSpeed;
    private SerializedProperty caption, captionColor, captionSize;
    private SerializedProperty enableKeyInteract, interactKey, interactDistance, keyEffects;
    private SerializedProperty enableApproachTrigger, approachDistance, approachOnlyOnce, approachEffects;

    private void OnEnable()
    {
        image = serializedObject.FindProperty("image");
        sortingOrder = serializedObject.FindProperty("sortingOrder");
        fadeInOnApproach = serializedObject.FindProperty("fadeInOnApproach");
        fadeDistance = serializedObject.FindProperty("fadeDistance");
        fadeSpeed = serializedObject.FindProperty("fadeSpeed");
        caption = serializedObject.FindProperty("caption");
        captionColor = serializedObject.FindProperty("captionColor");
        captionSize = serializedObject.FindProperty("captionSize");
        enableKeyInteract = serializedObject.FindProperty("enableKeyInteract");
        interactKey = serializedObject.FindProperty("interactKey");
        interactDistance = serializedObject.FindProperty("interactDistance");
        keyEffects = serializedObject.FindProperty("keyEffects");
        enableApproachTrigger = serializedObject.FindProperty("enableApproachTrigger");
        approachDistance = serializedObject.FindProperty("approachDistance");
        approachOnlyOnce = serializedObject.FindProperty("approachOnlyOnce");
        approachEffects = serializedObject.FindProperty("approachEffects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("图片设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(image, new GUIContent("图片"));
        EditorGUILayout.PropertyField(sortingOrder, new GUIContent("排序层级"));

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
        EditorGUILayout.LabelField("说明文字", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(caption, new GUIContent("文字内容"));
        if (!string.IsNullOrEmpty(caption.stringValue))
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(captionColor, new GUIContent("颜色"));
            EditorGUILayout.PropertyField(captionSize, new GUIContent("大小"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(12);
        DrawUILine(new Color(0.4f, 0.8f, 0.4f));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("按键交互", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableKeyInteract, new GUIContent("☑ 启用按键交互"));

        if (enableKeyInteract.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(interactKey, new GUIContent("交互按键"));
            EditorGUILayout.PropertyField(interactDistance, new GUIContent("交互距离"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
            DrawEffectSet(keyEffects, "按键触发效果");
        }

        EditorGUILayout.Space(12);
        DrawUILine(new Color(0.4f, 0.6f, 1f));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("靠近触发", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableApproachTrigger, new GUIContent("☑ 启用靠近触发"));

        if (enableApproachTrigger.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(approachDistance, new GUIContent("触发距离"));
            EditorGUILayout.PropertyField(approachOnlyOnce, new GUIContent("只触发一次"));
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

        DrawEffect("放大查看图片", fx.FindPropertyRelative("zoom"), null);

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

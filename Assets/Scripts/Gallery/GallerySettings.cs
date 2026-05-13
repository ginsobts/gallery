using UnityEngine;

[CreateAssetMenu(fileName = "GallerySettings", menuName = "Game/Gallery Settings")]
public class GallerySettings : ScriptableObject
{
    [Header("场景灯光")]
    [Tooltip("场景默认环境光亮度 (0~1)")]
    [Range(0f, 1f)]
    public float ambientBrightness = 0.3f;
    [Tooltip("环境光颜色")]
    public Color ambientColor = new Color(0.15f, 0.15f, 0.25f);

    [Header("手电筒")]
    [Tooltip("手电筒光圈大小")]
    public float flashlightRadius = 3f;
    [Tooltip("手电筒光颜色")]
    public Color flashlightColor = new Color(1f, 0.95f, 0.8f, 0.8f);
    [Tooltip("手电筒按键")]
    public KeyCode flashlightKey = KeyCode.F;

    [Header("入场黑屏")]
    [Tooltip("黑屏持续时间(秒)")]
    public float introDuration = 3f;
    [Tooltip("黑屏淡出时间(秒)")]
    public float introFadeTime = 1f;

    [Header("跟随 NPC")]
    [Tooltip("NPC 跟随玩家的距离")]
    public float followDistance = 1.5f;
    [Tooltip("NPC 跟随速度")]
    public float followSpeed = 4.5f;

    [Header("场景生成 · 图片")]
    [Tooltip("滤镜预览场景使用的图片（拖入即可一键替换）")]
    public Sprite filterPreviewImage;
    [Tooltip("Gallery 主场景使用的占位图片")]
    public Sprite galleryTestImage;
    [Tooltip("NPC 人物贴图")]
    public Sprite npcSprite;
}

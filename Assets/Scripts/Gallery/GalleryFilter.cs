using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GalleryFilter : MonoBehaviour
{
    public enum ColorFilter
    {
        None,
        Sepia,
        Vintage,
        CoolTone,
        WarmTone,
        Noir,
        Dreamy,
        Sunset,
        Ocean,
        Forest,
        Neon,
        Pastel,
        HighContrast,
        Faded,
        BluePurple,
        GoldenHour
    }

    public enum ArtisticStyle
    {
        None,
        PencilSketch,
        OilPainting,
        Watercolor,
        PixelArt,
        Comic,
        Impressionist,
        Pointillism,
        Woodcut,
        Charcoal,
        LineArt,
        StainedGlass,
        Mosaic,
        PopArt,
        Glitch,
        Ukiyoe,
        LowPoly,
        Emboss,
        Thermal,
        Negative,
        CrossStitch,
        VHS
    }

    [Header("颜色滤镜")]
    [SerializeField] private ColorFilter colorFilter = ColorFilter.None;
    [Range(0f, 1f)]
    [SerializeField] private float colorIntensity = 0.7f;

    [Header("风格化效果")]
    [SerializeField] private ArtisticStyle artisticStyle = ArtisticStyle.None;
    [Range(0f, 1f)]
    [SerializeField] private float styleIntensity = 0.85f;

    [Header("风格化参数")]
    [Tooltip("边缘检测阈值（铅笔/漫画）")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float edgeThreshold = 0.15f;
    [Tooltip("线条颜色（铅笔/漫画）")]
    [SerializeField] private Color edgeColor = new Color(0.1f, 0.08f, 0.05f);
    [Tooltip("纸张/背景颜色（铅笔/水彩）")]
    [SerializeField] private Color paperColor = new Color(0.96f, 0.94f, 0.9f);
    [Tooltip("笔触大小（油画/水彩/印象派）")]
    [Range(1f, 6f)]
    [SerializeField] private float brushSize = 3f;
    [Tooltip("像素块大小（像素风）")]
    [Range(4f, 64f)]
    [SerializeField] private float pixelSize = 12f;
    [Tooltip("色阶数（像素风/漫画）")]
    [Range(3f, 16f)]
    [SerializeField] private float quantizeLevels = 6f;
    [Tooltip("线条/网点密度（铅笔/漫画）")]
    [Range(20f, 200f)]
    [SerializeField] private float hatchDensity = 80f;

    [Header("暗角")]
    [SerializeField] private bool vignette = false;
    [Range(0f, 1f)]
    [SerializeField] private float vignetteStrength = 0.3f;

    private SpriteRenderer sr;
    private Material colorMat;
    private Material artisticMat;
    private static Shader colorShader;
    private static Shader artisticShader;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        Apply();
    }

    public void SetColorFilter(ColorFilter type, float strength = 0.7f)
    {
        colorFilter = type;
        colorIntensity = strength;
        Apply();
    }

    public void SetArtisticStyle(ArtisticStyle style, float strength = 0.85f)
    {
        artisticStyle = style;
        styleIntensity = strength;
        Apply();
    }

    private void Apply()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (artisticStyle != ArtisticStyle.None)
            ApplyArtistic();
        else if (colorFilter != ColorFilter.None || vignette)
            ApplyColor();
        else
            ResetMaterial();
    }

    private void ResetMaterial()
    {
        sr.material = new Material(Shader.Find("Sprites/Default"));
        if (colorMat != null) { Destroy(colorMat); colorMat = null; }
        if (artisticMat != null) { Destroy(artisticMat); artisticMat = null; }
    }

    private void ApplyColor()
    {
        if (artisticMat != null) { Destroy(artisticMat); artisticMat = null; }

        if (colorShader == null)
            colorShader = Shader.Find("Hidden/GalleryFilter");
        if (colorShader == null)
            colorShader = Shader.Find("Sprites/Default");

        if (colorMat == null)
        {
            colorMat = new Material(colorShader);
            sr.material = colorMat;
        }

        Color tint = Color.white;
        float sat = 1f, bright = 0f, contrast = 1f;
        Color overlay = new Color(0, 0, 0, 0);
        float i = colorIntensity;

        switch (colorFilter)
        {
            case ColorFilter.Sepia: tint = new Color(1.1f, 0.9f, 0.65f); sat = 0.3f; break;
            case ColorFilter.Vintage: tint = new Color(1.05f, 0.95f, 0.8f); sat = 0.6f; bright = -0.05f; overlay = new Color(0.4f, 0.2f, 0.1f, 0.15f); break;
            case ColorFilter.CoolTone: tint = new Color(0.85f, 0.92f, 1.1f); sat = 0.85f; break;
            case ColorFilter.WarmTone: tint = new Color(1.15f, 1.0f, 0.85f); sat = 0.9f; bright = 0.03f; break;
            case ColorFilter.Noir: sat = 0f; contrast = 1.3f; break;
            case ColorFilter.Dreamy: tint = new Color(1.05f, 0.98f, 1.1f); sat = 0.7f; bright = 0.08f; contrast = 0.85f; overlay = new Color(0.9f, 0.8f, 1f, 0.1f); break;
            case ColorFilter.Sunset: tint = new Color(1.2f, 0.85f, 0.7f); sat = 1.1f; overlay = new Color(1f, 0.5f, 0.2f, 0.12f); break;
            case ColorFilter.Ocean: tint = new Color(0.8f, 0.95f, 1.15f); sat = 0.9f; overlay = new Color(0.1f, 0.3f, 0.6f, 0.1f); break;
            case ColorFilter.Forest: tint = new Color(0.9f, 1.1f, 0.85f); sat = 0.85f; overlay = new Color(0.1f, 0.3f, 0.1f, 0.08f); break;
            case ColorFilter.Neon: tint = new Color(1f, 0.9f, 1.15f); sat = 1.4f; contrast = 1.2f; bright = 0.05f; break;
            case ColorFilter.Pastel: tint = new Color(1.05f, 1.02f, 1.05f); sat = 0.5f; bright = 0.15f; contrast = 0.8f; break;
            case ColorFilter.HighContrast: sat = 1.1f; contrast = 1.5f; bright = -0.02f; break;
            case ColorFilter.Faded: sat = 0.6f; bright = 0.1f; contrast = 0.8f; overlay = new Color(0.5f, 0.5f, 0.5f, 0.1f); break;
            case ColorFilter.BluePurple: tint = new Color(0.9f, 0.85f, 1.2f); sat = 0.8f; overlay = new Color(0.3f, 0.2f, 0.6f, 0.12f); break;
            case ColorFilter.GoldenHour: tint = new Color(1.2f, 1.05f, 0.8f); sat = 1.05f; bright = 0.05f; overlay = new Color(1f, 0.8f, 0.4f, 0.1f); break;
        }

        colorMat.SetColor("_Tint", Color.Lerp(Color.white, tint, i));
        colorMat.SetFloat("_Saturation", Mathf.Lerp(1f, sat, i));
        colorMat.SetFloat("_Brightness", bright * i);
        colorMat.SetFloat("_Contrast", Mathf.Lerp(1f, contrast, i));
        overlay.a *= i;
        colorMat.SetColor("_Overlay", overlay);
        colorMat.SetFloat("_Vignette", vignette ? vignetteStrength * i : 0f);
    }

    private bool needsTimeUpdate;

    private void ApplyArtistic()
    {
        if (colorMat != null) { Destroy(colorMat); colorMat = null; }

        if (artisticShader == null)
            artisticShader = Shader.Find("Hidden/GalleryArtistic");
        if (artisticShader == null)
        {
            ApplyColor();
            return;
        }

        if (artisticMat == null)
        {
            artisticMat = new Material(artisticShader);
            sr.material = artisticMat;
        }

        artisticMat.SetInt("_Style", (int)artisticStyle);
        artisticMat.SetFloat("_Intensity", styleIntensity);
        artisticMat.SetVector("_TexelSize", new Vector4(0.004f, 0.004f, 0, 0));
        artisticMat.SetFloat("_EdgeThreshold", edgeThreshold);
        artisticMat.SetColor("_EdgeColor", edgeColor);
        artisticMat.SetColor("_PaperColor", paperColor);
        artisticMat.SetFloat("_BrushSize", brushSize);
        artisticMat.SetFloat("_PixelSize", pixelSize);
        artisticMat.SetFloat("_QuantizeLevels", quantizeLevels);
        artisticMat.SetFloat("_HatchDensity", hatchDensity);

        needsTimeUpdate = (artisticStyle == ArtisticStyle.Glitch || artisticStyle == ArtisticStyle.VHS);
    }

    private void Update()
    {
        if (needsTimeUpdate && artisticMat != null)
            artisticMat.SetFloat("_Time2", Time.time);
    }

    private void OnDestroy()
    {
        if (colorMat != null) Destroy(colorMat);
        if (artisticMat != null) Destroy(artisticMat);
    }

    private void OnValidate()
    {
        if (Application.isPlaying && sr != null)
            Apply();
    }
}

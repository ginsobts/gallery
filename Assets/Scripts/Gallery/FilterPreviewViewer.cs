using UnityEngine;

public class FilterPreviewViewer : MonoBehaviour
{
    [HideInInspector] public GalleryFilter.ArtisticStyle artisticStyle;
    [HideInInspector] public float styleIntensity = 0.85f;
    [HideInInspector] public GalleryFilter.ColorFilter colorFilter;
    [HideInInspector] public float colorIntensity = 0.75f;
    [HideInInspector] public bool useVignette;
    [HideInInspector] public float vignetteStrength;

    private static FilterPreviewViewer activeViewer;
    private static GameObject fullscreenGO;

    private SpriteRenderer sr;
    private Camera cam;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (activeViewer == this && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CloseFullscreen();
            return;
        }

        if (activeViewer != null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (sr != null && sr.bounds.Contains(worldPos))
                OpenFullscreen();
        }
    }

    private void OpenFullscreen()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || sr == null || sr.sprite == null) return;

        activeViewer = this;

        fullscreenGO = new GameObject("FilterPreview_Fullscreen");

        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;
        float sprW = sr.sprite.bounds.size.x;
        float sprH = sr.sprite.bounds.size.y;
        float scale = Mathf.Min(camW * 0.85f / sprW, camH * 0.75f / sprH);

        fullscreenGO.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0);

        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(fullscreenGO.transform, false);
        bgGO.transform.localPosition = new Vector3(0, 0, 0.1f);
        bgGO.transform.localScale = new Vector3(camW * 2f, camH * 2f, 1);
        var bgSR = bgGO.AddComponent<SpriteRenderer>();
        bgSR.sprite = RuntimeSprite.Get();
        bgSR.color = new Color(0, 0, 0, 0.88f);
        bgSR.sortingOrder = 99;

        var imgGO = new GameObject("Image");
        imgGO.transform.SetParent(fullscreenGO.transform, false);
        imgGO.transform.localScale = Vector3.one * scale;
        var fsr = imgGO.AddComponent<SpriteRenderer>();
        fsr.sprite = sr.sprite;
        fsr.sortingOrder = 100;

        if (artisticStyle != GalleryFilter.ArtisticStyle.None)
        {
            var f = imgGO.AddComponent<GalleryFilter>();
            f.SetArtisticStyle(artisticStyle, styleIntensity);
        }
        else if (colorFilter != GalleryFilter.ColorFilter.None)
        {
            var f = imgGO.AddComponent<GalleryFilter>();
            f.SetColorFilter(colorFilter, colorIntensity);
        }

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(fullscreenGO.transform, false);
        labelGO.transform.localPosition = new Vector3(0, -camH * 0.42f, 0);
        var tm = labelGO.AddComponent<TextMesh>();
        string displayName = artisticStyle != GalleryFilter.ArtisticStyle.None ? artisticStyle.ToString() : colorFilter.ToString();
        if (artisticStyle == GalleryFilter.ArtisticStyle.None && colorFilter == GalleryFilter.ColorFilter.None) displayName = "Original";
        tm.text = displayName;
        tm.characterSize = 0.12f;
        tm.fontSize = 80;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 1f, 1f, 0.9f);
        labelGO.GetComponent<MeshRenderer>().sortingOrder = 101;
    }

    private static void CloseFullscreen()
    {
        if (fullscreenGO != null) Destroy(fullscreenGO);
        fullscreenGO = null;
        activeViewer = null;
    }

    private void OnDestroy()
    {
        if (activeViewer == this) CloseFullscreen();
    }
}

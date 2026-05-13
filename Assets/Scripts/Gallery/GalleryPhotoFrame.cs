using UnityEngine;

public class GalleryPhotoFrame : MonoBehaviour
{
    public enum FrameStyle
    {
        None,
        SimpleBorder,
        Shadow,
        Polaroid
    }

    [Header("相框样式")]
    [SerializeField] private FrameStyle style = FrameStyle.SimpleBorder;
    [SerializeField] private Color frameColor = new Color(0.95f, 0.93f, 0.88f);
    [Tooltip("边框厚度（归一化，相对于图片尺寸）")]
    [Range(0.02f, 0.3f)]
    [SerializeField] private float borderThickness = 0.08f;
    [Tooltip("阴影偏移")]
    [SerializeField] private Vector2 shadowOffset = new Vector2(0.05f, -0.05f);
    [Tooltip("阴影颜色")]
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.3f);

    private SpriteRenderer frameSR;
    private SpriteRenderer shadowSR;

    private void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        switch (style)
        {
            case FrameStyle.SimpleBorder:
                CreateBorder(sr);
                break;
            case FrameStyle.Shadow:
                CreateShadow(sr);
                break;
            case FrameStyle.Polaroid:
                CreatePolaroid(sr);
                break;
        }
    }

    private void CreateBorder(SpriteRenderer photoSR)
    {
        var go = new GameObject("Frame");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        float bx = 1f + borderThickness * 2f;
        float by = 1f + borderThickness * 2f;
        go.transform.localScale = new Vector3(bx, by, 1f);

        frameSR = go.AddComponent<SpriteRenderer>();
        frameSR.sprite = RuntimeSprite.Get();
        frameSR.color = frameColor;
        frameSR.sortingOrder = photoSR.sortingOrder - 1;
    }

    private void CreateShadow(SpriteRenderer photoSR)
    {
        CreateBorder(photoSR);

        var shGO = new GameObject("Shadow");
        shGO.transform.SetParent(transform);
        shGO.transform.localPosition = new Vector3(shadowOffset.x, shadowOffset.y, 0);

        float bx = 1f + borderThickness * 2f;
        float by = 1f + borderThickness * 2f;
        shGO.transform.localScale = new Vector3(bx, by, 1f);

        shadowSR = shGO.AddComponent<SpriteRenderer>();
        shadowSR.sprite = RuntimeSprite.Get();
        shadowSR.color = shadowColor;
        shadowSR.sortingOrder = photoSR.sortingOrder - 2;
    }

    private void CreatePolaroid(SpriteRenderer photoSR)
    {
        var go = new GameObject("Polaroid");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0, -borderThickness * 0.8f, 0);

        float bx = 1f + borderThickness * 2f;
        float topBorder = borderThickness;
        float bottomBorder = borderThickness * 3.5f;
        float by = 1f + topBorder + bottomBorder;
        go.transform.localScale = new Vector3(bx, by, 1f);

        frameSR = go.AddComponent<SpriteRenderer>();
        frameSR.sprite = RuntimeSprite.Get();
        frameSR.color = frameColor;
        frameSR.sortingOrder = photoSR.sortingOrder - 1;

        var shGO = new GameObject("PolaroidShadow");
        shGO.transform.SetParent(go.transform);
        shGO.transform.localPosition = new Vector3(
            shadowOffset.x / bx, shadowOffset.y / by, 0);
        shGO.transform.localScale = Vector3.one;

        shadowSR = shGO.AddComponent<SpriteRenderer>();
        shadowSR.sprite = RuntimeSprite.Get();
        shadowSR.color = shadowColor;
        shadowSR.sortingOrder = photoSR.sortingOrder - 2;
    }
}

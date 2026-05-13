using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GalleryImageDoor : MonoBehaviour
{
    [Header("门外观")]
    [Tooltip("门的贴图")]
    [SerializeField] private Sprite doorSprite;
    [Tooltip("门的颜色")]
    [SerializeField] private Color doorColor = new Color(0.4f, 0.3f, 0.6f);
    [Tooltip("激活后的颜色")]
    [SerializeField] private Color activatedColor = new Color(0.3f, 0.8f, 0.4f);

    [Header("切换的图片")]
    [Tooltip("激活后要切换到的图片（分配给场景中对应的 GalleryFrame）")]
    [SerializeField] private FrameSwap[] frameSwaps;

    [Header("切换的背景")]
    [Tooltip("激活后背景颜色")]
    [SerializeField] private Color newBackgroundColor = new Color(0.1f, 0.05f, 0.15f);
    [Tooltip("是否切换背景颜色")]
    [SerializeField] private bool changeBackground = true;

    [Header("效果")]
    [Tooltip("切换时的闪烁时间")]
    [SerializeField] private float flashDuration = 0.3f;

    [System.Serializable]
    public struct FrameSwap
    {
        [Tooltip("场景中的 GalleryFrame")]
        public GalleryFrame targetFrame;
        [Tooltip("新的图片")]
        public Sprite newImage;
        [Tooltip("新的说明文字（留空不变）")]
        public string newCaption;
    }

    private bool activated;
    private SpriteRenderer sr;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (doorSprite != null)
        {
            sr.sprite = doorSprite;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = RuntimeSprite.Get();
            sr.color = doorColor;
        }
        sr.sortingOrder = 5;
    }

    public void Activate()
    {
        if (activated) return;
        activated = true;

        sr.color = activatedColor;

        if (frameSwaps != null)
        {
            foreach (var swap in frameSwaps)
            {
                if (swap.targetFrame == null) continue;
                swap.targetFrame.SwapImage(swap.newImage, swap.newCaption);
            }
        }

        if (changeBackground)
        {
            var bg = FindObjectOfType<GalleryBackground>();
            if (bg != null)
                bg.SetDefaultColor(newBackgroundColor);
        }

        StartCoroutine(FlashEffect());
    }

    private System.Collections.IEnumerator FlashEffect()
    {
        var flashGO = new GameObject("DoorFlash");
        flashGO.transform.position = transform.position;
        var flashSR = flashGO.AddComponent<SpriteRenderer>();
        flashSR.sprite = RuntimeSprite.GetCircle(32);
        flashSR.sortingOrder = 50;
        flashSR.color = new Color(1f, 1f, 1f, 0.8f);
        flashGO.transform.localScale = Vector3.one * 0.5f;

        float t = 0;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float p = t / flashDuration;
            flashGO.transform.localScale = Vector3.one * (0.5f + p * 4f);
            flashSR.color = new Color(1f, 1f, 1f, 0.8f * (1f - p));
            yield return null;
        }

        Destroy(flashGO);
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = activated
            ? new Color(0.3f, 0.8f, 0.4f, 0.3f)
            : new Color(0.4f, 0.3f, 0.6f, 0.3f);
        Gizmos.DrawCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            activated ? "Door (OPEN)" : "Door");
#endif
    }
}

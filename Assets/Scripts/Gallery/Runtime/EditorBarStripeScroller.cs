using UnityEngine;
using UnityEngine.UI;

public class EditorBarStripeScroller : MonoBehaviour
{
    private RawImage rawImage;
    private float scrollSpeed = 0.4f;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    private void Update()
    {
        if (rawImage == null) return;
        var r = rawImage.uvRect;
        r.x += scrollSpeed * Time.unscaledDeltaTime;
        if (r.x > 100f) r.x -= 100f;
        rawImage.uvRect = r;
    }
}

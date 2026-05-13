using UnityEngine;

[RequireComponent(typeof(GalleryPlayer))]
public class GalleryFlashlight : MonoBehaviour
{
    private SpriteRenderer glowSR;
    private bool isOn;

    private void Start()
    {
        CreateGlow();
        SetActive(false);
    }

    private void Update()
    {
        var key = KeyCode.F;
        var mgr = GalleryManager.Instance;
        if (mgr != null && mgr.Settings != null)
            key = mgr.Settings.flashlightKey;

        if (Input.GetKeyDown(key))
        {
            isOn = !isOn;
            SetActive(isOn);
        }

        if (isOn && glowSR != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 2f) * 0.05f;
            float radius = 3f;
            var mgr2 = GalleryManager.Instance;
            if (mgr2 != null && mgr2.Settings != null)
                radius = mgr2.Settings.flashlightRadius;
            glowSR.transform.localScale = Vector3.one * radius * 2f * pulse;
        }
    }

    private void CreateGlow()
    {
        var go = new GameObject("FlashlightGlow");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        glowSR = go.AddComponent<SpriteRenderer>();
        glowSR.sprite = RuntimeSprite.GetCircle(64);
        glowSR.sortingOrder = 5;

        Color c = new Color(1f, 0.95f, 0.8f, 0.4f);
        var mgr = GalleryManager.Instance;
        if (mgr != null && mgr.Settings != null)
        {
            c = mgr.Settings.flashlightColor;
            c.a = Mathf.Min(c.a, 0.5f);
        }
        glowSR.color = c;

        float radius = 3f;
        if (mgr != null && mgr.Settings != null)
            radius = mgr.Settings.flashlightRadius;
        go.transform.localScale = Vector3.one * radius * 2f;
    }

    private void SetActive(bool active)
    {
        if (glowSR != null)
            glowSR.enabled = active;
    }
}

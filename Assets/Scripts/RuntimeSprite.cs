using UnityEngine;
using System.Collections.Generic;

public static class RuntimeSprite
{
    private static Sprite _fallback;
    private static Dictionary<int, Sprite> _circles = new Dictionary<int, Sprite>();

    public static Sprite Get()
    {
        if (_fallback != null) return _fallback;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _fallback = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
        _fallback.name = "RuntimeFallback";
        return _fallback;
    }

    public static Sprite GetCircle(int resolution = 32)
    {
        if (_circles.TryGetValue(resolution, out var cached) && cached != null) return cached;
        int size = resolution;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) * 0.5f;
        float radius = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) / (radius * 0.3f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        sprite.name = "RuntimeCircle_" + size;
        _circles[resolution] = sprite;
        return sprite;
    }
}

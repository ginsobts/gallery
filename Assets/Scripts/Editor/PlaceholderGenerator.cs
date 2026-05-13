using UnityEditor;
using UnityEngine;

public static class PlaceholderGenerator
{
    private const string Folder = "Assets/Art/Placeholder";

    public static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Art", "Placeholder");
    }

    public static Sprite GetOrCreate(string name, int w, int h, Color baseColor, PatternType pattern = PatternType.Gradient)
    {
        EnsureFolder();
        string path = $"{Folder}/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        FillPattern(tex, baseColor, pattern);

        DrawLabel(tex, name);

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        System.IO.File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public enum PatternType { Gradient, Stripe, Circle, Checker, Sunset, Ocean, Forest, Night }

    private static void FillPattern(Texture2D tex, Color baseColor, PatternType pattern)
    {
        int w = tex.width, h = tex.height;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / w;
                float v = (float)y / h;
                Color c;

                switch (pattern)
                {
                    case PatternType.Gradient:
                        c = Color.Lerp(baseColor * 0.4f, baseColor, v);
                        break;
                    case PatternType.Stripe:
                        bool stripe = ((x + y) / 8) % 2 == 0;
                        c = stripe ? baseColor : baseColor * 0.6f;
                        break;
                    case PatternType.Circle:
                        float dx = u - 0.5f, dy = v - 0.5f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        c = Color.Lerp(baseColor, baseColor * 0.2f, dist * 2f);
                        break;
                    case PatternType.Checker:
                        bool check = ((x / 16) + (y / 16)) % 2 == 0;
                        c = check ? baseColor : baseColor * 0.5f;
                        break;
                    case PatternType.Sunset:
                        c = Color.Lerp(
                            new Color(0.9f, 0.3f, 0.1f),
                            Color.Lerp(new Color(1f, 0.7f, 0.2f), new Color(0.2f, 0.1f, 0.3f), v),
                            v);
                        float sun = 1f - Mathf.Clamp01(Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.7f) * (v - 0.7f)) * 4f);
                        c += new Color(1f, 0.8f, 0.3f) * sun * 0.5f;
                        break;
                    case PatternType.Ocean:
                        float wave = Mathf.Sin(u * 20f + v * 5f) * 0.1f;
                        c = Color.Lerp(
                            new Color(0.05f, 0.15f, 0.35f),
                            new Color(0.1f, 0.4f, 0.6f),
                            v + wave);
                        break;
                    case PatternType.Forest:
                        float noise = Mathf.PerlinNoise(u * 8f, v * 8f);
                        c = Color.Lerp(
                            new Color(0.05f, 0.2f, 0.05f),
                            new Color(0.2f, 0.5f, 0.15f),
                            noise);
                        break;
                    case PatternType.Night:
                        c = Color.Lerp(
                            new Color(0.02f, 0.02f, 0.08f),
                            new Color(0.05f, 0.05f, 0.15f),
                            v);
                        float star = Mathf.PerlinNoise(u * 50f, v * 50f);
                        if (star > 0.85f)
                            c += new Color(0.8f, 0.8f, 0.9f) * (star - 0.85f) * 5f;
                        break;
                    default:
                        c = baseColor;
                        break;
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
    }

    private static void DrawLabel(Texture2D tex, string label)
    {
        int w = tex.width, h = tex.height;

        int barH = Mathf.Max(h / 6, 16);
        int barY = h / 2 - barH / 2;
        for (int y = barY; y < barY + barH; y++)
            for (int x = 0; x < w; x++)
            {
                Color orig = tex.GetPixel(x, y);
                tex.SetPixel(x, y, Color.Lerp(orig, new Color(0, 0, 0, 0.6f), 0.6f));
            }

        int charW = Mathf.Max(w / (label.Length + 2), 4);
        int charH = Mathf.Min(barH - 4, charW * 2);
        int startX = (w - label.Length * charW) / 2;
        int startY = h / 2 - charH / 2;

        for (int i = 0; i < label.Length; i++)
        {
            bool[,] glyph = GetSimpleGlyph(label[i]);
            if (glyph == null) continue;
            int gw = glyph.GetLength(1), gh = glyph.GetLength(0);
            for (int gy = 0; gy < gh; gy++)
                for (int gx = 0; gx < gw; gx++)
                {
                    if (!glyph[gy, gx]) continue;
                    int px = startX + i * charW + gx * charW / gw;
                    int py = startY + (gh - 1 - gy) * charH / gh;
                    if (px >= 0 && px < w && py >= 0 && py < h)
                        tex.SetPixel(px, py, Color.white);
                }
        }
        tex.Apply();
    }

    private static bool[,] GetSimpleGlyph(char c)
    {
        switch (char.ToUpper(c))
        {
            case '0': return new bool[,]{{true,true,true},{true,false,true},{true,false,true},{true,false,true},{true,true,true}};
            case '1': return new bool[,]{{false,true,false},{true,true,false},{false,true,false},{false,true,false},{true,true,true}};
            case '2': return new bool[,]{{true,true,true},{false,false,true},{true,true,true},{true,false,false},{true,true,true}};
            case '3': return new bool[,]{{true,true,true},{false,false,true},{true,true,true},{false,false,true},{true,true,true}};
            case '4': return new bool[,]{{true,false,true},{true,false,true},{true,true,true},{false,false,true},{false,false,true}};
            case '5': return new bool[,]{{true,true,true},{true,false,false},{true,true,true},{false,false,true},{true,true,true}};
            case '6': return new bool[,]{{true,true,true},{true,false,false},{true,true,true},{true,false,true},{true,true,true}};
            case '7': return new bool[,]{{true,true,true},{false,false,true},{false,false,true},{false,false,true},{false,false,true}};
            case '8': return new bool[,]{{true,true,true},{true,false,true},{true,true,true},{true,false,true},{true,true,true}};
            case '9': return new bool[,]{{true,true,true},{true,false,true},{true,true,true},{false,false,true},{true,true,true}};
            case 'A': return new bool[,]{{false,true,false},{true,false,true},{true,true,true},{true,false,true},{true,false,true}};
            case 'B': return new bool[,]{{true,true,false},{true,false,true},{true,true,false},{true,false,true},{true,true,false}};
            case 'C': return new bool[,]{{true,true,true},{true,false,false},{true,false,false},{true,false,false},{true,true,true}};
            case 'D': return new bool[,]{{true,true,false},{true,false,true},{true,false,true},{true,false,true},{true,true,false}};
            case 'E': return new bool[,]{{true,true,true},{true,false,false},{true,true,false},{true,false,false},{true,true,true}};
            case 'F': return new bool[,]{{true,true,true},{true,false,false},{true,true,false},{true,false,false},{true,false,false}};
            case 'G': return new bool[,]{{true,true,true},{true,false,false},{true,false,true},{true,false,true},{true,true,true}};
            case 'H': return new bool[,]{{true,false,true},{true,false,true},{true,true,true},{true,false,true},{true,false,true}};
            case 'I': return new bool[,]{{true,true,true},{false,true,false},{false,true,false},{false,true,false},{true,true,true}};
            case 'K': return new bool[,]{{true,false,true},{true,false,true},{true,true,false},{true,false,true},{true,false,true}};
            case 'L': return new bool[,]{{true,false,false},{true,false,false},{true,false,false},{true,false,false},{true,true,true}};
            case 'N': return new bool[,]{{true,false,true},{true,true,true},{true,true,true},{true,false,true},{true,false,true}};
            case 'O': return new bool[,]{{true,true,true},{true,false,true},{true,false,true},{true,false,true},{true,true,true}};
            case 'P': return new bool[,]{{true,true,true},{true,false,true},{true,true,true},{true,false,false},{true,false,false}};
            case 'R': return new bool[,]{{true,true,false},{true,false,true},{true,true,false},{true,false,true},{true,false,true}};
            case 'S': return new bool[,]{{true,true,true},{true,false,false},{true,true,true},{false,false,true},{true,true,true}};
            case 'T': return new bool[,]{{true,true,true},{false,true,false},{false,true,false},{false,true,false},{false,true,false}};
            case 'U': return new bool[,]{{true,false,true},{true,false,true},{true,false,true},{true,false,true},{true,true,true}};
            case 'V': return new bool[,]{{true,false,true},{true,false,true},{true,false,true},{false,true,false},{false,true,false}};
            case 'W': return new bool[,]{{true,false,true},{true,false,true},{true,true,true},{true,true,true},{true,false,true}};
            case '-': return new bool[,]{{false,false,false},{false,false,false},{true,true,true},{false,false,false},{false,false,false}};
            case '_': return new bool[,]{{false,false,false},{false,false,false},{false,false,false},{false,false,false},{true,true,true}};
            case ' ': return new bool[,]{{false,false},{false,false},{false,false},{false,false},{false,false}};
            default: return null;
        }
    }
}

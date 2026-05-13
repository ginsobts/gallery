using UnityEditor;
using UnityEngine;
using System.IO;

public static class PathTextureGenerator
{
    private const string TextureFolder = "Assets/Art/PathTextures";
    private const string MaterialFolder = "Assets/Art/PathMaterials";

    [MenuItem("Tools/Gallery/生成石头路材质")]
    public static void GenerateStonePath()
    {
        EnsureFolders();
        var tex = CreateStoneTexture(256, 256);
        string texPath = SaveTexture(tex, "StonePath", true);
        Object.DestroyImmediate(tex);

        string matPath = MaterialFolder + "/StonePath.mat";
        var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existingMat == null)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.name = "StonePath";
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            existingMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            EditorUtility.SetDirty(existingMat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"石头路材质已生成:\n  纹理: {texPath}\n  材质: {matPath}\n\n将材质拖到 GalleryPath 的 Custom Material 字段即可使用。");
    }

    private static Texture2D CreateStoneTexture(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        int stoneCountX = 6;
        int stoneCountY = 12;
        float cellW = (float)w / stoneCountX;
        float cellH = (float)h / stoneCountY;

        Color baseStone = new Color(0.72f, 0.7f, 0.66f);
        Color grout = new Color(0.45f, 0.42f, 0.38f);
        Color darkStone = new Color(0.58f, 0.56f, 0.52f);
        Color lightStone = new Color(0.82f, 0.8f, 0.76f);

        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = grout;

        float groutWidth = 2.5f;

        for (int sy = 0; sy < stoneCountY; sy++)
        {
            float rowOffset = (sy % 2 == 0) ? 0f : cellW * 0.5f;
            for (int sx = 0; sx < stoneCountX + 1; sx++)
            {
                float cx = sx * cellW + rowOffset;
                float cy = sy * cellH + cellH * 0.5f;

                float stoneHue = Mathf.PerlinNoise(sx * 3.7f + sy * 0.5f, sy * 2.3f + sx * 0.8f);
                Color stoneColor = Color.Lerp(darkStone, lightStone, stoneHue);

                float sizeVarX = 0.8f + Mathf.PerlinNoise(sx * 5f, sy * 5f) * 0.35f;
                float sizeVarY = 0.8f + Mathf.PerlinNoise(sx * 5f + 50f, sy * 5f + 50f) * 0.35f;
                float halfW = (cellW * 0.5f - groutWidth) * sizeVarX;
                float halfH = (cellH * 0.5f - groutWidth) * sizeVarY;

                for (int py = Mathf.Max(0, (int)(cy - halfH)); py < Mathf.Min(h, (int)(cy + halfH)); py++)
                {
                    for (int px = 0; px < w; px++)
                    {
                        float wrappedX = px;
                        float dx = Mathf.Abs(WrapDist(wrappedX, cx, w));
                        float dy = Mathf.Abs(py - cy);

                        if (dx > halfW || dy > halfH) continue;

                        float edgeDist = Mathf.Min(
                            Mathf.Min(halfW - dx, halfH - dy),
                            Mathf.Min(dx + halfW, dy + halfH)
                        );
                        float edgeFade = Mathf.Clamp01(edgeDist / 2f);

                        float surfaceNoise = Mathf.PerlinNoise(
                            px * 0.15f + sx * 10f,
                            py * 0.15f + sy * 10f
                        ) * 0.12f - 0.06f;

                        float fineNoise = Mathf.PerlinNoise(
                            px * 0.5f + sx * 20f,
                            py * 0.5f + sy * 20f
                        ) * 0.06f - 0.03f;

                        Color c = stoneColor;
                        c.r += surfaceNoise + fineNoise;
                        c.g += surfaceNoise + fineNoise;
                        c.b += surfaceNoise + fineNoise;

                        float highlight = Mathf.Clamp01((halfH - dy) / halfH) * 0.08f;
                        c.r += highlight; c.g += highlight; c.b += highlight;

                        Color existing = pixels[py * w + px];
                        pixels[py * w + px] = Color.Lerp(existing, c, edgeFade);
                    }
                }
            }
        }

        tex.SetPixels(pixels);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float edgeU = (float)x / w;
                float edgeAlpha = 1f;
                float fadeWidth = 0.08f;
                if (edgeU < fadeWidth) edgeAlpha = edgeU / fadeWidth;
                else if (edgeU > 1f - fadeWidth) edgeAlpha = (1f - edgeU) / fadeWidth;

                if (edgeAlpha < 1f)
                {
                    Color c = tex.GetPixel(x, y);
                    c.a = edgeAlpha;
                    tex.SetPixel(x, y, c);
                }
            }
        }

        tex.Apply();
        return tex;
    }

    private static float WrapDist(float a, float b, float size)
    {
        float d = a - b;
        if (d > size * 0.5f) d -= size;
        if (d < -size * 0.5f) d += size;
        return d;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(TextureFolder))
            AssetDatabase.CreateFolder("Assets/Art", "PathTextures");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Art", "PathMaterials");
    }

    private static string SaveTexture(Texture2D tex, string name, bool tiling)
    {
        string path = TextureFolder + "/" + name + ".png";
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        return path;
    }
}

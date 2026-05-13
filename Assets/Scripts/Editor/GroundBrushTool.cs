using UnityEngine;
using UnityEditor;
using System.IO;

public class GroundBrushTool : EditorWindow
{
    private GalleryGround targetGround;
    private float brushSize = 2f;
    private float brushSoftness = 0.6f;
    private float brushOpacity = 0.8f;
    private bool eraseMode = false;
    private int texResolution = 1024;
    private bool isPainting = false;
    private Vector2 scrollPos;

    private enum BrushMode { Color, Texture }
    private BrushMode brushMode = BrushMode.Color;
    private Color brushColor = new Color(0.55f, 0.45f, 0.3f);
    private Color eraseColor = new Color(0.25f, 0.25f, 0.22f);
    private Texture2D brushTexture;
    private float textureScale = 0f;
    private bool autoFitTexture = true;
    private float textureSizePercent = 1f;

    [MenuItem("Tools/Gallery/地面笔刷工具")]
    public static void Open()
    {
        var win = GetWindow<GroundBrushTool>("地面笔刷");
        win.minSize = new Vector2(280, 420);
        win.Show();
    }

    private void OnEnable() { SceneView.duringSceneGui += OnSceneGUI; if (targetGround == null) targetGround = FindObjectOfType<GalleryGround>(); }
    private void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("地面笔刷工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        targetGround = (GalleryGround)EditorGUILayout.ObjectField("目标 GalleryGround", targetGround, typeof(GalleryGround), true);
        if (targetGround == null)
        {
            EditorGUILayout.HelpBox("请在场景中放置一个 GalleryGround 组件。", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        // ── 地面贴图 ──
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("地面贴图", EditorStyles.boldLabel);

        if (targetGround.groundTexture == null)
        {
            texResolution = EditorGUILayout.IntPopup("分辨率", texResolution,
                new[] { "512", "1024", "2048" }, new[] { 512, 1024, 2048 });
            eraseColor = EditorGUILayout.ColorField("初始底色", eraseColor);
            if (GUILayout.Button("创建地面贴图", GUILayout.Height(28)))
                CreateGroundTexture();
        }
        else
        {
            EditorGUILayout.LabelField($"  贴图: {targetGround.groundTexture.width} x {targetGround.groundTexture.height}");
        }

        // ── 笔刷模式 ──
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("画笔", EditorStyles.boldLabel);

        brushMode = (BrushMode)EditorGUILayout.EnumPopup("模式", brushMode);

        if (brushMode == BrushMode.Color)
        {
            brushColor = EditorGUILayout.ColorField("颜色", brushColor);
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            brushTexture = (Texture2D)EditorGUILayout.ObjectField("纹理贴图", brushTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && brushTexture != null && autoFitTexture)
                textureScale = CalcAutoFitScale();

            autoFitTexture = EditorGUILayout.Toggle("自动适配大小", autoFitTexture);
            if (autoFitTexture && brushTexture != null)
            {
                float baseScale = CalcAutoFitScale();
                EditorGUILayout.LabelField($"  原始大小: {baseScale:F2} 世界单位");
                textureSizePercent = EditorGUILayout.Slider("缩放比例", textureSizePercent, 0.1f, 3f);
                textureScale = baseScale * textureSizePercent;
                EditorGUILayout.LabelField($"  实际大小: {textureScale:F2} 世界单位");
            }
            else if (!autoFitTexture)
            {
                textureScale = EditorGUILayout.Slider("单块大小（世界单位）", textureScale, 0.5f, 30f);
            }

            if (brushTexture != null)
            {
                EditorGUILayout.BeginHorizontal();
                Rect r = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64));
                EditorGUI.DrawPreviewTexture(r, brushTexture);
                EditorGUILayout.LabelField($"{brushTexture.width} x {brushTexture.height} px", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("请拖入一张纹理图片（如石头路.png）", MessageType.Info);
            }
        }

        // ── 笔刷参数 ──
        EditorGUILayout.Space(8);
        brushSize = EditorGUILayout.Slider("笔刷大小", brushSize, 0.1f, 15f);
        brushSoftness = EditorGUILayout.Slider("柔和度", brushSoftness, 0f, 1f);
        brushOpacity = EditorGUILayout.Slider("不透明度", brushOpacity, 0.01f, 1f);
        eraseMode = EditorGUILayout.Toggle("擦除（恢复底色）", eraseMode);

        if (eraseMode)
            eraseColor = EditorGUILayout.ColorField("底色", eraseColor);

        // ── 操作 ──
        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("保存到磁盘", GUILayout.Height(30)))
            SaveTexture();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);
        if (GUILayout.Button("填充整个地面", GUILayout.Height(24)))
        {
            if (EditorUtility.DisplayDialog("填充", "用当前画笔填充整个地面？", "确认", "取消"))
                FillAll();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "在 Scene 视图:\n" +
            "• 鼠标左键涂抹\n" +
            "• 滚轮调整大小\n" +
            "• E 切换擦除",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (targetGround == null || targetGround.groundTexture == null) return;

        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane plane = new Plane(Vector3.forward, targetGround.transform.position);
        float enter;
        if (!plane.Raycast(ray, out enter)) return;
        Vector3 worldPoint = ray.GetPoint(enter);

        Color preview = eraseMode ? new Color(1, 1, 1, 0.3f) : new Color(brushColor.r, brushColor.g, brushColor.b, 0.5f);
        if (brushMode == BrushMode.Texture && !eraseMode) preview = new Color(0.4f, 0.8f, 1f, 0.5f);
        Handles.color = preview;
        Handles.DrawWireDisc(worldPoint, Vector3.forward, brushSize);
        float innerR = brushSize * (1f - brushSoftness);
        if (innerR > 0.01f && innerR < brushSize - 0.01f)
        {
            preview.a *= 0.4f;
            Handles.color = preview;
            Handles.DrawWireDisc(worldPoint, Vector3.forward, innerR);
        }
        if (eraseMode) Handles.Label(worldPoint + Vector3.up * (brushSize + 0.3f), "ERASE");

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.E) { eraseMode = !eraseMode; e.Use(); Repaint(); }
        if (e.type == EventType.ScrollWheel) { brushSize = Mathf.Clamp(brushSize - e.delta.y * 0.15f, 0.1f, 15f); e.Use(); Repaint(); }

        bool wantPaint = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0;
        if (wantPaint) { PaintAt(worldPoint); isPainting = true; e.Use(); }
        if (e.type == EventType.MouseUp && e.button == 0 && isPainting)
        {
            isPainting = false;
            targetGround.groundTexture.Apply();
            targetGround.RefreshTexture();
            e.Use();
        }
        sceneView.Repaint();
    }

    private void PaintAt(Vector3 worldPos)
    {
        Texture2D tex = targetGround.groundTexture;
        if (tex == null) return;

        Vector2 center = targetGround.WorldToPixel(worldPos);
        float ppu = tex.width / targetGround.GroundWidth;
        float radiusPx = brushSize * ppu;
        int r = Mathf.CeilToInt(radiusPx);
        int cx = Mathf.RoundToInt(center.x), cy = Mathf.RoundToInt(center.y);
        int xMin = Mathf.Max(0, cx - r), xMax = Mathf.Min(tex.width - 1, cx + r);
        int yMin = Mathf.Max(0, cy - r), yMax = Mathf.Min(tex.height - 1, cy + r);
        float softR = radiusPx * (1f - brushSoftness);
        float invFade = 1f / Mathf.Max(radiusPx - softR, 0.001f);

        bool useTexture = !eraseMode && brushMode == BrushMode.Texture && brushTexture != null && brushTexture.isReadable;

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                float dx = x - center.x, dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radiusPx) continue;

                float falloff = 1f - Mathf.Clamp01((dist - softR) * invFade);
                float strength = brushOpacity * falloff * 0.15f;

                Color existing = tex.GetPixel(x, y);
                Color target;

                if (eraseMode)
                {
                    target = eraseColor;
                }
                else if (useTexture)
                {
                    float wx = (float)x / tex.width * targetGround.GroundWidth;
                    float wy = (float)y / tex.height * targetGround.GroundHeight;
                    float tileW = textureScale;
                    float tileH = tileW * ((float)brushTexture.height / brushTexture.width);
                    float tu = (wx / tileW) % 1f;
                    float tv = (wy / tileH) % 1f;
                    if (tu < 0) tu += 1f;
                    if (tv < 0) tv += 1f;
                    target = brushTexture.GetPixelBilinear(tu, tv);
                }
                else
                {
                    target = brushColor;
                }

                Color result = Color.Lerp(existing, target, strength);
                result.a = 1f;
                tex.SetPixel(x, y, result);
            }
        }
        tex.Apply();
        targetGround.RefreshTexture();
    }

    private void FillAll()
    {
        Texture2D tex = targetGround.groundTexture;
        if (tex == null) return;

        bool useTexture = brushMode == BrushMode.Texture && brushTexture != null && brushTexture.isReadable;
        Color[] pixels = new Color[tex.width * tex.height];
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                if (useTexture)
                {
                    float wx = (float)x / tex.width * targetGround.GroundWidth;
                    float wy = (float)y / tex.height * targetGround.GroundHeight;
                    float tileW = textureScale;
                    float tileH = tileW * ((float)brushTexture.height / brushTexture.width);
                    float tu = (wx / tileW) % 1f;
                    float tv = (wy / tileH) % 1f;
                    pixels[y * tex.width + x] = brushTexture.GetPixelBilinear(tu, tv);
                }
                else
                {
                    pixels[y * tex.width + x] = brushColor;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        targetGround.RefreshTexture();
    }

    private void CreateGroundTexture()
    {
        string dir = "Assets/Gallery/GroundTextures";
        if (!AssetDatabase.IsValidFolder("Assets/Gallery"))
            AssetDatabase.CreateFolder("Assets", "Gallery");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Gallery", "GroundTextures");

        Texture2D tex = new Texture2D(texResolution, texResolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[texResolution * texResolution];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = eraseColor;
        tex.SetPixels(pixels);
        tex.Apply();

        string path = AssetDatabase.GenerateUniqueAssetPath(dir + "/Ground.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Default;
            imp.isReadable = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = texResolution;
            imp.sRGBTexture = true;
            imp.SaveAndReimport();
        }

        var loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var so = new SerializedObject(targetGround);
        so.FindProperty("groundTexture").objectReferenceValue = loaded;
        so.ApplyModifiedProperties();
        Repaint();
    }

    private float CalcAutoFitScale()
    {
        if (brushTexture == null || targetGround == null || targetGround.groundTexture == null)
            return 4f;
        float worldPerPixel = targetGround.GroundWidth / targetGround.groundTexture.width;
        return brushTexture.width * worldPerPixel;
    }

    private void SaveTexture()
    {
        if (targetGround == null || targetGround.groundTexture == null) return;
        string path = AssetDatabase.GetAssetPath(targetGround.groundTexture);
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllBytes(path, targetGround.groundTexture.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null) { imp.isReadable = true; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport(); }
        Debug.Log($"地面贴图已保存: {path}");
    }
}

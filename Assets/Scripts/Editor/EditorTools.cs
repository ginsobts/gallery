using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;

public static class EditorTools
{
    [MenuItem("Tools/切换场景中 UI 显示 %h")]
    public static void ToggleUISceneVisibility()
    {
        var canvases = Object.FindObjectsOfType<Canvas>(true);
        if (canvases.Length == 0)
        {
            Debug.Log("场景中没有 Canvas");
            return;
        }

        var svm = SceneVisibilityManager.instance;
        bool anyVisible = false;
        foreach (var c in canvases)
        {
            if (!svm.IsHidden(c.gameObject))
            {
                anyVisible = true;
                break;
            }
        }

        foreach (var c in canvases)
        {
            if (anyVisible)
                svm.Hide(c.gameObject, true);
            else
                svm.Show(c.gameObject, true);
        }

        Debug.Log(anyVisible ? "UI 已在 Scene 视图中隐藏" : "UI 已在 Scene 视图中显示");
    }

    [MenuItem("Tools/关卡/创建 SignPost Prefab")]
    public static void CreateSignPostPrefab()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string path = folder + "/SignPost.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("SignPost.prefab 已存在：" + path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return;
        }

        var go = new GameObject("SignPost");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Get();
        sr.color = new Color(0.6f, 0.45f, 0.2f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        go.AddComponent<SignPost>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Selection.activeObject = prefab;
        Debug.Log("已创建 SignPost Prefab：" + path);
    }

    [MenuItem("Tools/关卡/创建 GlowPoint Prefab")]
    public static void CreateGlowPointPrefab()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string path = folder + "/GlowPoint.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("GlowPoint.prefab 已存在：" + path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return;
        }

        var go = new GameObject("GlowPoint");
        go.AddComponent<GlowPoint>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Selection.activeObject = prefab;
        Debug.Log("已创建 GlowPoint Prefab：" + path);
    }

    [MenuItem("Tools/关卡/配置帧动画到 Player 和 Monster Prefab")]
    public static void AssignFrameAnimations()
    {
        string[] animFolders = new[]
        {
            "Assets/Art/animation/角色1",
            "Assets/Art/animation/角色2",
            "Assets/Art/animation/猫",
            "Assets/Art/animation/狗",
            "Assets/Art/animation/鸟"
        };
        EnsureSpritesImported(animFolders);

        int changes = 0;

        // Player prefab
        string playerPath = "Assets/Resources/Prefabs/Player.prefab";
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        if (playerPrefab != null)
        {
            var pc = playerPrefab.GetComponent<PlayerController>();
            if (pc != null)
            {
                var so = new SerializedObject(pc);

                var darkFramesProp = so.FindProperty("darkFrames");
                var lightFramesProp = so.FindProperty("lightFrames");
                var animFpsProp = so.FindProperty("animFps");
                var lightSpriteProp = so.FindProperty("lightSprite");
                var darkSpriteProp = so.FindProperty("darkSprite");

                var dark = LoadSortedSprites("Assets/Art/animation/角色1");
                var light = LoadSortedSprites("Assets/Art/animation/角色2");

                if (dark.Length > 0)
                {
                    darkFramesProp.arraySize = dark.Length;
                    for (int i = 0; i < dark.Length; i++)
                        darkFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = dark[i];
                    darkSpriteProp.objectReferenceValue = null;
                }

                if (light.Length > 0)
                {
                    lightFramesProp.arraySize = light.Length;
                    for (int i = 0; i < light.Length; i++)
                        lightFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = light[i];
                    lightSpriteProp.objectReferenceValue = null;
                }

                animFpsProp.floatValue = 3f;
                so.ApplyModifiedProperties();

                var sr = playerPrefab.GetComponent<SpriteRenderer>();
                if (sr != null && light.Length > 0)
                {
                    sr.sprite = light[0];
                    sr.color = Color.white;
                    EditorUtility.SetDirty(sr);
                }

                EditorUtility.SetDirty(playerPrefab);
                changes++;
                Debug.Log($"[Player] darkFrames={dark.Length}, lightFrames={light.Length}, fps=3, 旧单张贴图已清除");
            }
        }
        else
        {
            Debug.LogWarning("找不到 Player Prefab: " + playerPath);
        }

        // Monster prefab
        string monsterPath = "Assets/Prefabs/Monster.prefab";
        var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(monsterPath);
        if (monsterPrefab != null)
        {
            var monster = monsterPrefab.GetComponent<Monster>();
            if (monster != null)
            {
                var so = new SerializedObject(monster);
                var catFramesProp = so.FindProperty("catFrames");
                var dogFramesProp = so.FindProperty("dogFrames");
                var birdFramesProp = so.FindProperty("birdFrames");
                var fpsProp = so.FindProperty("animFps");

                var catSprites = LoadSortedSprites("Assets/Art/animation/猫");
                var dogSprites = LoadSortedSprites("Assets/Art/animation/狗");
                var birdSprites = LoadSortedSprites("Assets/Art/animation/鸟");

                if (catSprites.Length > 0)
                {
                    catFramesProp.arraySize = catSprites.Length;
                    for (int i = 0; i < catSprites.Length; i++)
                        catFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = catSprites[i];
                }

                if (dogSprites.Length > 0)
                {
                    dogFramesProp.arraySize = dogSprites.Length;
                    for (int i = 0; i < dogSprites.Length; i++)
                        dogFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = dogSprites[i];
                }

                if (birdSprites.Length > 0)
                {
                    birdFramesProp.arraySize = birdSprites.Length;
                    for (int i = 0; i < birdSprites.Length; i++)
                        birdFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = birdSprites[i];
                }

                fpsProp.floatValue = 3f;
                so.ApplyModifiedProperties();

                var sr = monsterPrefab.GetComponent<SpriteRenderer>();
                if (sr != null && catSprites.Length > 0)
                {
                    sr.sprite = catSprites[0];
                    sr.color = Color.white;
                    EditorUtility.SetDirty(sr);
                }

                EditorUtility.SetDirty(monsterPrefab);
                changes++;
                Debug.Log($"[Monster] catFrames={catSprites.Length}, dogFrames={dogSprites.Length}, birdFrames={birdSprites.Length}, fps=3");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"帧动画配置完成，修改了 {changes} 个 Prefab。\n" +
                  "Monster 已挂好猫和狗两组帧动画，在 Inspector 中切换 Monster Type 即可。");
    }

    private static void EnsureSpritesImported(string[] folders)
    {
        bool reimport = false;
        foreach (var folder in folders)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                if (importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    reimport = true;
                }
            }
        }
        if (reimport)
        {
            AssetDatabase.Refresh();
            Debug.Log("已将动画图片全部设为 Sprite 格式并重新导入");
        }
    }

    [MenuItem("Tools/关卡/设置动画图片 Pixels Per Unit")]
    public static void SetAnimationPPU()
    {
        PixelsPerUnitWindow.Open();
    }

    private static Sprite[] LoadSortedSprites(string folderPath)
    {
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        var sprites = new System.Collections.Generic.List<Sprite>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null) sprites.Add(spr);
        }

        sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        return sprites.ToArray();
    }

    [MenuItem("Tools/关卡/在 MainMenu 添加语言切换按钮")]
    public static void AddLanguageButton()
    {
        var menuUI = Object.FindObjectOfType<MainMenuUI>();
        if (menuUI == null)
        {
            Debug.LogError("当前场景没有 MainMenuUI，请先打开 MainMenu 场景");
            return;
        }

        if (Object.FindObjectOfType<LanguageManager>() == null)
        {
            var lmGo = new GameObject("LanguageManager");
            lmGo.AddComponent<LanguageManager>();
            Undo.RegisterCreatedObjectUndo(lmGo, "Create LanguageManager");
            Debug.Log("已创建 LanguageManager 物体");
        }

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("场景中没有 Canvas");
            return;
        }

        var btnGo = new GameObject("LanguageButton");
        Undo.RegisterCreatedObjectUndo(btnGo, "Create Language Button");
        btnGo.transform.SetParent(canvas.transform, false);

        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
        rt.sizeDelta = new Vector2(200, 50);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
        btn.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.text = "中文 / English";
        text.font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        UnityEventTools.AddPersistentListener(btn.onClick,
            new UnityEngine.Events.UnityAction(menuUI.OnToggleLanguage));

        var so = new SerializedObject(menuUI);
        var prop = so.FindProperty("languageButtonText");
        if (prop != null)
        {
            prop.objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = btnGo;
        Debug.Log("已添加语言切换按钮并自动关联 MainMenuUI");
    }

    [MenuItem("Tools/关卡/创建并烘焙 NavMesh 2D")]
    public static void CreateAndBakeNavMesh2D()
    {
        PrepareNavMeshScene();

        foreach (var wallCol in Object.FindObjectsOfType<WallSpriteCollider2D>(true))
            wallCol.SyncColliderNow();

        var surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface == null)
        {
            var go = new GameObject("NavMesh2D");
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = NavMeshPlus.Components.CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.defaultArea = 0;

            go.AddComponent<CollectSources2d>();
        }

        surface.agentTypeID = 0;
        surface.BuildNavMesh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        var settings = NavMesh.GetSettingsByID(0);
        Debug.Log($"NavMesh 2D 烘焙完成！agentRadius={settings.agentRadius}  按 Ctrl+S 保存场景。");
    }

    [MenuItem("Tools/关卡/设置 NavMesh Agent 半径")]
    public static void SetNavMeshAgentRadius()
    {
        NavAgentRadiusWindow.Open();
    }

    static void PrepareNavMeshScene()
    {
        EnsureNavFloor();
        MarkWallsNotWalkable();
    }

    static void EnsureNavFloor()
    {
        const string floorName = "NavFloor";
        var existing = GameObject.Find(floorName);
        if (existing == null)
        {
            existing = new GameObject(floorName);
            existing.AddComponent<BoxCollider2D>();
        }

        var allColliders = Object.FindObjectsOfType<Collider2D>();
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var c in allColliders)
        {
            if (c.gameObject == existing) continue;
            if (first) { bounds = c.bounds; first = false; }
            else bounds.Encapsulate(c.bounds);
        }

        if (first)
        {
            Debug.LogWarning("场景中没有找到任何碰撞体，无法计算地面范围。");
            return;
        }

        float padding = 3f;
        existing.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
        var box = existing.GetComponent<BoxCollider2D>();
        box.size = new Vector2(bounds.size.x + padding * 2, bounds.size.y + padding * 2);
        box.isTrigger = true;

        var mod = existing.GetComponent<NavMeshModifier>();
        if (mod == null) mod = existing.AddComponent<NavMeshModifier>();
        mod.overrideArea = true;
        mod.area = 0;

        Debug.Log($"NavFloor 地面：center={bounds.center}, size={box.size}");
    }

    static void MarkWallsNotWalkable()
    {
        int wallCount = 0;

        var wallRoot = GameObject.Find("Wall");
        if (wallRoot != null)
        {
            wallCount += MarkChildrenNotWalkable(wallRoot.transform);
        }

        var allColliders = Object.FindObjectsOfType<Collider2D>();
        foreach (var col in allColliders)
        {
            if (col.isTrigger) continue;
            if (col.GetComponent<NavMeshModifier>() != null) continue;

            bool isWall = col.gameObject.name.Contains("Wall") || col.gameObject.name.Contains("wall");
            if (!isWall && wallRoot != null && col.transform.IsChildOf(wallRoot.transform))
                isWall = true;

            if (isWall)
            {
                var mod = col.gameObject.AddComponent<NavMeshModifier>();
                mod.overrideArea = true;
                mod.area = 1;
                wallCount++;
            }
        }

        Debug.Log($"已标记 {wallCount} 个墙壁为 Not Walkable");
    }

    static int MarkChildrenNotWalkable(Transform parent)
    {
        int count = 0;
        var colliders = parent.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col.isTrigger) continue;
            if (col.GetComponent<NavMeshModifier>() != null) continue;

            var mod = col.gameObject.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = 1;
            count++;
        }
        return count;
    }

    // ── LevelGlobals Prefab ──

    private const string LevelGlobalsPrefabPath = "Assets/Prefabs/LevelGlobals.prefab";

    [MenuItem("Tools/关卡/创建 LevelGlobals Prefab")]
    public static void CreateLevelGlobalsPrefab()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(LevelGlobalsPrefabPath);
        if (existing != null)
        {
            Debug.Log("LevelGlobals.prefab 已存在：" + LevelGlobalsPrefabPath);
            Selection.activeObject = existing;
            return;
        }

        var go = new GameObject("LevelGlobals");
        go.AddComponent<LevelPhaseManager>();
        go.AddComponent<CursorManager>();
        go.AddComponent<DarkPhaseParticles>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, LevelGlobalsPrefabPath);
        Object.DestroyImmediate(go);

        Selection.activeObject = prefab;
        Debug.Log("已创建 LevelGlobals Prefab，包含 LevelPhaseManager + CursorManager + DarkPhaseParticles");
    }

    [MenuItem("Tools/关卡/同步 LevelGlobals 到所有关卡场景")]
    public static void SyncLevelGlobalsToAllScenes()
    {
        const string sourceScenePath = "Assets/Scenes/level_xm_1.unity";
        if (!System.IO.File.Exists(sourceScenePath))
        {
            Debug.LogError("找不到源场景 level_xm_1，请确认 Assets/Scenes/level_xm_1.unity 存在");
            return;
        }

        var currentScene = EditorSceneManager.GetActiveScene().path;

        var srcScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Single);
        GameObject srcGlobals = null;
        foreach (var root in srcScene.GetRootGameObjects())
        {
            if (root.GetComponent<LevelPhaseManager>() != null)
            {
                srcGlobals = root;
                break;
            }
        }

        if (srcGlobals == null)
        {
            Debug.LogError("level_xm_1 场景中没有找到 LevelGlobals（含 LevelPhaseManager 的根物体）");
            return;
        }

        var tempPrefabPath = "Assets/Prefabs/_TempLevelGlobalsSync.prefab";
        PrefabUtility.SaveAsPrefabAsset(srcGlobals, tempPrefabPath);
        var tempPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tempPrefabPath);

        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        int updated = 0;

        foreach (var guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName == "MainMenu" || sceneName == "SampleScene" || scenePath == sourceScenePath)
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject oldGlobals = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<LevelPhaseManager>() != null)
                {
                    oldGlobals = root;
                    break;
                }
            }

            if (oldGlobals != null)
                Object.DestroyImmediate(oldGlobals);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(tempPrefab, scene);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "LevelGlobals";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            updated++;
            Debug.Log($"  [{sceneName}] 已从 level_xm_1 同步 LevelGlobals");
        }

        AssetDatabase.DeleteAsset(tempPrefabPath);
        var metaPath = tempPrefabPath + ".meta";
        if (System.IO.File.Exists(metaPath))
            AssetDatabase.DeleteAsset(metaPath);

        if (!string.IsNullOrEmpty(currentScene))
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);

        Debug.Log($"已将 level_xm_1 的 LevelGlobals 同步到 {updated} 个关卡场景。");
    }

    [MenuItem("Tools/关卡/批量设置 Monster Combo Sequence")]
    public static void OpenMonsterComboEditor()
    {
        MonsterComboWindow.Open();
    }

    [MenuItem("Tools/关卡/创建 ImagePanel Prefab")]
    public static void CreateImagePanelPrefab()
    {
        string folder = "Assets/Resources/Prefabs";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        string path = folder + "/ImagePanel.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("ImagePanel.prefab 已存在：" + path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return;
        }

        var root = new GameObject("ImagePanel");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // ── ShowButton: 打开面板的按钮 ──
        var showBtnGo = new GameObject("ShowButton");
        showBtnGo.transform.SetParent(root.transform, false);
        var showRect = showBtnGo.AddComponent<RectTransform>();
        showRect.anchorMin = new Vector2(0.5f, 0f);
        showRect.anchorMax = new Vector2(0.5f, 0f);
        showRect.pivot = new Vector2(0.5f, 0f);
        showRect.anchoredPosition = new Vector2(0f, 60f);
        showRect.sizeDelta = new Vector2(300f, 80f);

        var showImg = showBtnGo.AddComponent<Image>();
        showImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        showBtnGo.AddComponent<Button>();

        var showTextGo = new GameObject("Text");
        showTextGo.transform.SetParent(showBtnGo.transform, false);
        var showTxtRect = showTextGo.AddComponent<RectTransform>();
        showTxtRect.anchorMin = Vector2.zero;
        showTxtRect.anchorMax = Vector2.one;
        showTxtRect.offsetMin = Vector2.zero;
        showTxtRect.offsetMax = Vector2.zero;
        var showText = showTextGo.AddComponent<Text>();
        showText.text = "查看图片";
        showText.font = LoadFont();
        showText.fontSize = 36;
        showText.alignment = TextAnchor.MiddleCenter;
        showText.color = Color.white;

        // ── Panel: 全屏图片面板（默认隐藏） ──
        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(root.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.85f);

        // ── DisplayImage: 中央展示的图片 ──
        var displayGo = new GameObject("DisplayImage");
        displayGo.transform.SetParent(panelGo.transform, false);
        var displayRect = displayGo.AddComponent<RectTransform>();
        displayRect.anchorMin = new Vector2(0.1f, 0.1f);
        displayRect.anchorMax = new Vector2(0.9f, 0.9f);
        displayRect.offsetMin = Vector2.zero;
        displayRect.offsetMax = Vector2.zero;
        var displayImg = displayGo.AddComponent<Image>();
        displayImg.color = new Color(1f, 1f, 1f, 0.9f);
        displayImg.preserveAspect = true;

        // ── BackButton: 返回按钮 ──
        var backBtnGo = new GameObject("BackButton");
        backBtnGo.transform.SetParent(panelGo.transform, false);
        var backRect = backBtnGo.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(1f, 1f);
        backRect.anchorMax = new Vector2(1f, 1f);
        backRect.pivot = new Vector2(1f, 1f);
        backRect.anchoredPosition = new Vector2(-30f, -30f);
        backRect.sizeDelta = new Vector2(120f, 60f);

        var backImg = backBtnGo.AddComponent<Image>();
        backImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        backBtnGo.AddComponent<Button>();

        var backTextGo = new GameObject("Text");
        backTextGo.transform.SetParent(backBtnGo.transform, false);
        var backTxtRect = backTextGo.AddComponent<RectTransform>();
        backTxtRect.anchorMin = Vector2.zero;
        backTxtRect.anchorMax = Vector2.one;
        backTxtRect.offsetMin = Vector2.zero;
        backTxtRect.offsetMax = Vector2.zero;
        var backText = backTextGo.AddComponent<Text>();
        backText.text = "返回";
        backText.font = LoadFont();
        backText.fontSize = 30;
        backText.alignment = TextAnchor.MiddleCenter;
        backText.color = Color.white;

        panelGo.SetActive(false);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        Debug.Log("已创建 ImagePanel Prefab：" + path +
                  "\n结构：ImagePanel / ShowButton（查看图片按钮）+ Panel / DisplayImage（图片）+ BackButton（返回）" +
                  "\n双击 Prefab 即可修改美术、调整布局");
    }

    private static Font LoadFont()
    {
        var font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    [MenuItem("Tools/关卡/添加 BgmManager 到 MainMenu 场景")]
    public static void AddBgmManager()
    {
        if (Object.FindObjectOfType<BgmManager>() != null)
        {
            Debug.Log("场景中已存在 BgmManager");
            Selection.activeGameObject = Object.FindObjectOfType<BgmManager>().gameObject;
            return;
        }

        var go = new GameObject("BgmManager");
        go.AddComponent<BgmManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create BgmManager");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;
        Debug.Log("已创建 BgmManager，请在 Inspector 中拖入 BGM AudioClip（主菜单、白天、黑夜）");
    }

    public static void SetHumanoidAgentRadius(float radius)
    {
        const string path = "ProjectSettings/NavMeshAreas.asset";
        var text = System.IO.File.ReadAllText(path);
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"(agentTypeID: 0\s+agentRadius: )\S+",
            "${1}" + radius.ToString("F2"));
        System.IO.File.WriteAllText(path, text);
    }
}

public class NavAgentRadiusWindow : EditorWindow
{
    private float radius;

    public static void Open()
    {
        var win = GetWindow<NavAgentRadiusWindow>("NavMesh Agent 半径");
        var settings = NavMesh.GetSettingsByID(0);
        win.radius = settings.agentRadius;
        win.minSize = new Vector2(300, 100);
        win.maxSize = new Vector2(400, 120);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("越小 → 窄通道覆盖越多；越大 → 离墙越远\n建议 0.05 ~ 0.5", EditorStyles.wordWrappedLabel);
        GUILayout.Space(5);
        radius = EditorGUILayout.Slider("Agent 半径", radius, 0.01f, 0.5f);
        GUILayout.Space(5);

        if (GUILayout.Button("保存并重新烘焙"))
        {
            EditorTools.SetHumanoidAgentRadius(radius);
            AssetDatabase.Refresh();
            Close();
            EditorTools.CreateAndBakeNavMesh2D();
        }
    }
}

public class PixelsPerUnitWindow : EditorWindow
{
    private float ppu = 200f;

    public static void Open()
    {
        var win = GetWindow<PixelsPerUnitWindow>("设置 Pixels Per Unit");
        win.minSize = new Vector2(320, 120);
        win.maxSize = new Vector2(420, 140);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("修改 Art/animation 下所有图片的 Pixels Per Unit\n数值越大，Sprite 在场景中越小", EditorStyles.wordWrappedLabel);
        GUILayout.Space(5);
        ppu = EditorGUILayout.FloatField("Pixels Per Unit", ppu);
        GUILayout.Space(5);

        if (GUILayout.Button("应用到所有动画图片"))
        {
            string[] folders = new[]
            {
                "Assets/Art/animation/角色1",
                "Assets/Art/animation/角色2",
                "Assets/Art/animation/猫",
                "Assets/Art/animation/狗",
                "Assets/Art/animation/鸟"
            };

            int count = 0;
            foreach (var folder in folders)
            {
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    importer.spritePixelsPerUnit = ppu;
                    importer.SaveAndReimport();
                    count++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"已将 {count} 张动画图片的 Pixels Per Unit 设为 {ppu}");
            Close();
        }
    }
}

public class MonsterComboWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<BulletType> templateCombo = new List<BulletType> { BulletType.Dot, BulletType.Dot, BulletType.Line };
    private float templateDetectRange = 5f;

    private struct MonsterEntry
    {
        public Monster monster;
        public List<BulletType> combo;
        public float detectRange;
        public bool foldout;
    }

    private List<MonsterEntry> entries = new List<MonsterEntry>();

    public static void Open()
    {
        var win = GetWindow<MonsterComboWindow>("批量设置 Monster Combo");
        win.minSize = new Vector2(420, 350);
        win.RefreshMonsters();
    }

    private void OnEnable()
    {
        RefreshMonsters();
    }

    private void RefreshMonsters()
    {
        entries.Clear();
        var monsters = Object.FindObjectsOfType<Monster>(true);
        foreach (var m in monsters)
        {
            var so = new SerializedObject(m);
            var prop = so.FindProperty("requiredCombo");
            var combo = new List<BulletType>();
            for (int i = 0; i < prop.arraySize; i++)
                combo.Add((BulletType)prop.GetArrayElementAtIndex(i).enumValueIndex);
            float dr = so.FindProperty("detectRange").floatValue;
            entries.Add(new MonsterEntry { monster = m, combo = combo, detectRange = dr, foldout = true });
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"场景中共 {entries.Count} 个 Monster", EditorStyles.boldLabel);
        if (GUILayout.Button("刷新列表", GUILayout.Width(80)))
            RefreshMonsters();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("批量模板", EditorStyles.boldLabel);
        DrawComboList(templateCombo, "模板");
        templateDetectRange = EditorGUILayout.FloatField("Detect Range", templateDetectRange);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用模板到全部 Monster"))
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                e.combo = new List<BulletType>(templateCombo);
                e.detectRange = templateDetectRange;
                entries[i] = e;
            }
            ApplyAll();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("逐个设置", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int idx = 0; idx < entries.Count; idx++)
        {
            var e = entries[idx];
            if (e.monster == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            e.foldout = EditorGUILayout.Foldout(e.foldout, e.monster.gameObject.name, true, EditorStyles.foldoutHeader);
            if (GUILayout.Button("选中", GUILayout.Width(42)))
                Selection.activeGameObject = e.monster.gameObject;
            if (GUILayout.Button("用模板", GUILayout.Width(52)))
            {
                e.combo = new List<BulletType>(templateCombo);
                e.detectRange = templateDetectRange;
                ApplyEntry(e);
            }
            EditorGUILayout.EndHorizontal();

            if (e.foldout)
            {
                EditorGUI.indentLevel++;
                bool changed = DrawComboList(e.combo, e.monster.gameObject.name);

                float newRange = EditorGUILayout.FloatField("Detect Range", e.detectRange);
                if (newRange != e.detectRange)
                {
                    e.detectRange = newRange;
                    changed = true;
                }

                if (changed) ApplyEntry(e);
                EditorGUI.indentLevel--;
            }

            entries[idx] = e;
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private bool DrawComboList(List<BulletType> combo, string label)
    {
        bool changed = false;

        for (int i = 0; i < combo.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15);
            GUILayout.Label($"[{i}]", GUILayout.Width(28));

            var newVal = (BulletType)EditorGUILayout.EnumPopup(combo[i], GUILayout.Width(80));
            if (newVal != combo[i]) { combo[i] = newVal; changed = true; }

            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                combo.RemoveAt(i);
                changed = true;
                GUILayout.EndHorizontal();
                break;
            }

            if (i > 0 && GUILayout.Button("↑", GUILayout.Width(22)))
            {
                (combo[i], combo[i - 1]) = (combo[i - 1], combo[i]);
                changed = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUI.indentLevel * 15 + 30);
        if (GUILayout.Button("+ Dot", GUILayout.Width(60)))
        {
            combo.Add(BulletType.Dot);
            changed = true;
        }
        if (GUILayout.Button("+ Line", GUILayout.Width(60)))
        {
            combo.Add(BulletType.Line);
            changed = true;
        }
        EditorGUILayout.EndHorizontal();

        return changed;
    }

    private void ApplyEntry(MonsterEntry entry)
    {
        if (entry.monster == null) return;
        Undo.RecordObject(entry.monster, "Set Monster Properties");
        var so = new SerializedObject(entry.monster);

        var comboProp = so.FindProperty("requiredCombo");
        comboProp.arraySize = entry.combo.Count;
        for (int i = 0; i < entry.combo.Count; i++)
            comboProp.GetArrayElementAtIndex(i).enumValueIndex = (int)entry.combo[i];

        so.FindProperty("detectRange").floatValue = entry.detectRange;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(entry.monster);
        EditorSceneManager.MarkSceneDirty(entry.monster.gameObject.scene);
    }

    private void ApplyAll()
    {
        foreach (var e in entries)
            ApplyEntry(e);
        Debug.Log($"已将模板应用到 {entries.Count} 个 Monster（Combo + Detect Range）");
    }
}

public static partial class EditorToolsExtra
{
    private const string GalleryPrefabFolder = "Assets/Prefabs/Gallery";

    private static void EnsureGalleryFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(GalleryPrefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Gallery");
    }

    // ────── Gallery: only 2 menu items ──────

    [MenuItem("Tools/Gallery/初始化 Gallery（Prefab + 设置）")]
    public static void InitGallery()
    {
        EnsureGalleryFolder();

        CreatePrefabIfNeeded("GalleryPlayer", go => {
            go.tag = "Player";
            go.AddComponent<GalleryPlayer>();
            go.AddComponent<GalleryFlashlight>();
            go.AddComponent<GalleryFootsteps>();
            go.AddComponent<GalleryVehicle>();
            go.AddComponent<GalleryFootprints>();
            go.AddComponent<GalleryVehicleEffects>();
        });
        CreatePrefabIfNeeded("GalleryFollower",  go => go.AddComponent<GalleryFollower>());
        CreatePrefabIfNeeded("GalleryFrame",     go => go.AddComponent<GalleryFrame>());
        CreatePrefabIfNeeded("GalleryWall",      go => go.AddComponent<GalleryWall>());
        CreatePrefabIfNeeded("GallerySign",      go => go.AddComponent<GallerySign>());
        CreatePrefabIfNeeded("GalleryVideo",     go => go.AddComponent<GalleryVideo>());
        CreatePrefabIfNeeded("GalleryPhotoViewer", go => go.AddComponent<GalleryPhotoViewer>());
        CreatePrefabIfNeeded("GalleryBGMZone",   go => go.AddComponent<GalleryBGMZone>());
        CreatePrefabIfNeeded("GalleryGroundType", go => go.AddComponent<GalleryGroundType>());
        CreatePrefabIfNeeded("GalleryAreaTitle", go => go.AddComponent<GalleryAreaTitle>());
        CreatePrefabIfNeeded("GalleryAreaParticles", go => go.AddComponent<GalleryAreaParticles>());
        CreatePrefabIfNeeded("GalleryTimeline",  go => go.AddComponent<GalleryTimeline>());
        CreatePrefabIfNeeded("GalleryMemory",    go => go.AddComponent<GalleryMemory>());
        CreatePrefabIfNeeded("GalleryPortal",    go => go.AddComponent<GalleryPortal>());
        CreatePrefabIfNeeded("GalleryBackground", go => go.AddComponent<GalleryBackground>());
        CreatePrefabIfNeeded("GalleryWeather", go => go.AddComponent<GalleryWeather>());
        CreatePrefabIfNeeded("GalleryParallaxFrame", go => go.AddComponent<GalleryParallaxFrame>());
        CreatePrefabIfNeeded("GalleryCollectible", go => go.AddComponent<GalleryCollectible>());
        CreatePrefabIfNeeded("GalleryPushBlock", go => go.AddComponent<GalleryPushBlock>());
        CreatePrefabIfNeeded("GallerySlideshow", go => go.AddComponent<GallerySlideshow>());
        CreatePrefabIfNeeded("GalleryImageDoor", go => go.AddComponent<GalleryImageDoor>());
        CreatePrefabIfNeeded("GalleryGround", go => go.AddComponent<GalleryGround>());
        CreatePrefabIfNeeded("GalleryPath", go => go.AddComponent<GalleryPath>());
        CreatePrefabIfNeeded("GallerySecretDoor", go => go.AddComponent<GallerySecretDoor>());
        CreatePrefabIfNeeded("GalleryJigsaw", go => go.AddComponent<GalleryJigsaw>());
        CreatePrefabIfNeeded("GalleryNPCDialogue", go => {
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<GalleryNPCDialogue>();
        });
        CreatePrefabIfNeeded("GalleryManager",   go => {
            go.AddComponent<GalleryManager>();
            go.AddComponent<GalleryMinimap>();
            go.AddComponent<GalleryShowcase>();
        });

        // 确保已有的 Player Prefab 也挂上新组件
        var existingPlayer = AssetDatabase.LoadAssetAtPath<GameObject>(GalleryPrefabFolder + "/GalleryPlayer.prefab");
        if (existingPlayer != null && existingPlayer.GetComponent<GalleryVehicleEffects>() == null)
        {
            existingPlayer.AddComponent<GalleryVehicleEffects>();
            EditorUtility.SetDirty(existingPlayer);
        }

        EnsureGallerySettings();

        AssetDatabase.SaveAssets();
        Debug.Log($"Gallery 初始化完成！\n  Prefab: {GalleryPrefabFolder}/\n  设置: Assets/Resources/GallerySettings.asset");
    }

    private static GallerySettings GetSettings()
    {
        return AssetDatabase.LoadAssetAtPath<GallerySettings>("Assets/Resources/GallerySettings.asset");
    }

    private static Sprite ImportSpriteAt(string path, float baseWorldWidth = 2.56f)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return null;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            { importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            float targetPPU = tex.width / baseWorldWidth;
            if (Mathf.Abs(importer.spritePixelsPerUnit - targetPPU) > 1f)
            { importer.spritePixelsPerUnit = targetPPU; changed = true; }
            if (importer.maxTextureSize < tex.width)
            { importer.maxTextureSize = Mathf.NextPowerOfTwo(tex.width); changed = true; }
            if (changed) importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite LoadTestPhoto(string fallbackPath = "Assets/Photo/test.png")
    {
        var settings = GetSettings();
        if (settings != null && settings.galleryTestImage != null)
            return settings.galleryTestImage;
        var sprite = ImportSpriteAt(fallbackPath);
        if (sprite != null) return sprite;
        Debug.LogWarning("找不到占位图片，使用默认生成");
        return PlaceholderGenerator.GetOrCreate("ph_default", 256, 192,
            new Color(0.5f, 0.5f, 0.5f), PlaceholderGenerator.PatternType.Gradient);
    }

    private static Sprite LoadFilterPreviewPhoto()
    {
        var settings = GetSettings();
        if (settings != null && settings.filterPreviewImage != null)
            return settings.filterPreviewImage;
        return ImportSpriteAt("Assets/Photo/cat.png") ?? LoadTestPhoto();
    }

    [MenuItem("Tools/Gallery/创建 Gallery 场景")]
    public static void CreateGalleryScene()
    {
        InitGallery();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── 布局常量：4 个区块，每块宽 36 单位 ──
        const float BW = 36f;        // block width
        const float BH = 20f;        // block height (camera orthoSize*2)
        // Block centers: 0, 36, 72, 108
        float B0 = 0f, B1 = BW, B2 = BW * 2, B3 = BW * 3;
        float totalW = BW * 4;       // 144
        float midX = totalW * 0.5f - BW * 0.5f; // 54

        // ── Camera ──
        var cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 10;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.1f);
            cam.transform.position = new Vector3(B0, 0, -10);

            var gc = cam.gameObject.AddComponent<GalleryCamera>();
            var gcSO = new SerializedObject(gc);
            gcSO.FindProperty("blockCount").intValue = 4;
            gcSO.FindProperty("firstBlockCenterX").floatValue = B0;
            gcSO.FindProperty("transitionSpeed").floatValue = 4f;
            gcSO.FindProperty("cameraY").floatValue = 0f;
            gcSO.ApplyModifiedProperties();
        }

        // ━━━━━━━━━━ 占位图片（使用 test.png） ━━━━━━━━━━
        var testPhoto = LoadTestPhoto();
        Sprite ph_kinkaku = testPhoto, ph_bamboo = testPhoto, ph_fushimi = testPhoto;
        Sprite ph_bund = testPhoto, ph_yugarden = testPhoto, ph_nanjing = testPhoto;
        Sprite ph_church = testPhoto, ph_glacier = testPhoto, ph_sand = testPhoto;
        Sprite ph_future_a = testPhoto, ph_future_b = testPhoto;
        Sprite ph_album1 = testPhoto, ph_album2 = testPhoto, ph_album3 = testPhoto, ph_album4 = testPhoto;
        Sprite ph_video = testPhoto;

        // ━━━━━━━━━━ 全局 ━━━━━━━━━━

        var mgr = Instantiate("GalleryManager", Vector3.zero);
        if (mgr != null)
        {
            var gm = mgr.GetComponent<GalleryManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                so.FindProperty("introText").stringValue = "欢迎来到旅行画廊\nWelcome to the Travel Gallery";
                so.FindProperty("introTextPosition").vector2Value = new Vector2(0.5f, 0.45f);
                so.FindProperty("introFontSize").intValue = 32;
                so.ApplyModifiedProperties();
            }
        }

        // Background（4 区域渐变）
        var bg = Instantiate("GalleryBackground", Vector3.zero);
        if (bg != null)
        {
            var bgComp = bg.GetComponent<GalleryBackground>();
            if (bgComp != null)
            {
                var so = new SerializedObject(bgComp);
                so.FindProperty("defaultColor").colorValue = new Color(0.08f, 0.06f, 0.12f);
                var zones = so.FindProperty("zones");
                zones.arraySize = 4;

                var z0 = zones.GetArrayElementAtIndex(0);
                z0.FindPropertyRelative("center").vector2Value = new Vector2(B0, 0);
                z0.FindPropertyRelative("fallbackColor").colorValue = new Color(0.22f, 0.10f, 0.06f);
                z0.FindPropertyRelative("radius").floatValue = BW * 0.6f;

                var z1 = zones.GetArrayElementAtIndex(1);
                z1.FindPropertyRelative("center").vector2Value = new Vector2(B1, 0);
                z1.FindPropertyRelative("fallbackColor").colorValue = new Color(0.04f, 0.14f, 0.06f);
                z1.FindPropertyRelative("radius").floatValue = BW * 0.6f;

                var z2 = zones.GetArrayElementAtIndex(2);
                z2.FindPropertyRelative("center").vector2Value = new Vector2(B2, 0);
                z2.FindPropertyRelative("fallbackColor").colorValue = new Color(0.04f, 0.10f, 0.25f);
                z2.FindPropertyRelative("radius").floatValue = BW * 0.6f;

                var z3 = zones.GetArrayElementAtIndex(3);
                z3.FindPropertyRelative("center").vector2Value = new Vector2(B3, 0);
                z3.FindPropertyRelative("fallbackColor").colorValue = new Color(0.18f, 0.06f, 0.18f);
                z3.FindPropertyRelative("radius").floatValue = BW * 0.6f;

                so.ApplyModifiedProperties();
            }
        }

        // Player
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GalleryPrefabFolder + "/GalleryPlayer.prefab");
        if (playerPrefab != null)
        {
            var p = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            p.transform.position = new Vector3(B0, -2, 0);
        }

        // NPC 跟随者
        var follower1 = Instantiate("GalleryFollower", new Vector3(B0 + 3, -3, 0));
        if (follower1 != null)
        {
            follower1.name = "Follower_Companion";
            var npcSpr = LoadNPCSprite();
            if (npcSpr != null)
            {
                var fSR = follower1.GetComponent<SpriteRenderer>();
                if (fSR != null) { fSR.sprite = npcSpr; fSR.color = Color.white; }
                var fSO = new SerializedObject(follower1.GetComponent<GalleryFollower>());
                fSO.FindProperty("npcSprite").objectReferenceValue = npcSpr;
                fSO.ApplyModifiedProperties();
            }
        }

        // ━━━━━━━━━━ Block 0（x≈0）: 2018-2019 · 京都 — 暖色·和风 ━━━━━━━━━━

        MakeAreaTitle("AreaTitle_Kyoto", B0, "京都", "Kyoto · 2018-2019", new Color(1f, 0.85f, 0.7f), BW, BH);
        MakeParticles("Particles_Sakura_B0", B0, 0, 45, new Color(1f, 0.7f, 0.8f, 0.6f), false, BW, BH);
        MakeWeather("Weather_LightRain_B0", B0, 0, 50, new Color(0.7f, 0.75f, 0.9f, 0.3f), 0.6f, BW, BH);
        MakeGround("Ground_Wood_B0", B0, 0, BW, BH, 4);
        MakeBGMZone("BGM_Kyoto", B0, BW, BH);

        // 主视觉：大幅金阁寺（居中偏左上，Polaroid + 浮世绘滤镜）
        MakeFrame("Frame_Kinkakuji", B0 - 4, 5, 6, 4f, "金阁寺 · Kinkaku-ji", new Color(1f, 0.9f, 0.7f, 0.9f), false, testPhoto);
        // 右侧中等竹林（稍低，淡入 + 水彩滤镜）
        MakeFrame("Frame_Bamboo", B0 + 6, 3.5f, 4, 3f, "岚山竹林 · Arashiyama", Color.white, true, testPhoto);
        // 左下小幅伏见（走近淡入）
        MakeFrame("Frame_Fushimi", B0 - 10, 1, 2.5f, 1.8f, "伏见稻荷 · 千本鸟居", new Color(0.9f, 0.5f, 0.3f, 0.85f), true, testPhoto);
        // 右上小幅（点缀）
        MakeFrame("Frame_Kyoto_Tea", B0 + 12, 6, 2, 1.5f, "抹茶时光", new Color(0.7f, 0.9f, 0.6f, 0.8f), false, testPhoto);
        // 底部横幅（怀旧风）
        MakeFrame("Frame_Kyoto_Street", B0 + 2, -3, 5, 2f, "京都的小巷", new Color(0.95f, 0.85f, 0.7f, 0.7f), false, testPhoto);

        AddFrameFilter("Frame_Kinkakuji", GalleryFilter.ArtisticStyle.Ukiyoe, 0.6f);
        AddFrameFilter("Frame_Bamboo", GalleryFilter.ArtisticStyle.Watercolor, 0.7f);
        AddFrameFilter("Frame_Kyoto_Street", GalleryFilter.ArtisticStyle.Charcoal, 0.5f);
        AddPhotoFrame("Frame_Kinkakuji", 3, new Color(0.98f, 0.96f, 0.92f), 0.06f);
        AddPhotoFrame("Frame_Bamboo", 1, new Color(0.85f, 0.9f, 0.8f), 0.04f);
        AddPhotoFrame("Frame_Fushimi", 3, new Color(0.95f, 0.9f, 0.85f), 0.05f);

        MakeParallax("Parallax_Kyoto", B0 - 8, 5, "", 0.02f, 0.07f);
        MakePhotoViewer("PhotoViewer_Kyoto", B0 + 10, 0, 3, 2.2f, new Sprite[]{ testPhoto, testPhoto, testPhoto, testPhoto });
        MakeMemory("Memory_Kyoto", B0 - 12, -2, "十月的京都，枫叶还没有完全红透。\n清水寺的舞台上，风带着木头的气味。\n这一刻好像可以永远停在这里。", 0.04f, 0f);
        MakeMemory("Memory_Matcha", B0 + 8, -5, "抹茶冰淇淋，真的很好吃。\n店主说这是今年最后一批了。", 0.06f, 4f);
        MakeSign("Sign_Kyoto", B0 + 14, -2, "京都，日本的古都。\n1600 多座寺庙。\n→ 前方：上海", 5f);
        MakeCollectible("金阁寺御守", B0 - 14, 7, 0, new Color(1f, 0.85f, 0.3f), false);
        MakeCollectible("竹林风铃", B0 + 14, 3, 1, new Color(0.5f, 0.9f, 0.5f), true);

        // ━━━━━━━━━━ Block 1（x≈36）: 2020-2021 · 上海 — 霓虹·都市 ━━━━━━━━━━

        MakeAreaTitle("AreaTitle_Shanghai", B1, "上海", "Shanghai · 2020-2021", new Color(0.9f, 1f, 0.7f), BW, BH);
        MakeParticles("Particles_Firefly_B1", B1, 2, 30, new Color(1f, 0.95f, 0.5f, 0.6f), true, BW, BH);
        MakeWeather("Weather_Fog_B1", B1, 2, 35, new Color(0.7f, 0.75f, 0.8f, 0.35f), 0.4f, BW, BH);
        MakeGround("Ground_Stone_B1", B1, 0, BW, BH, 2);
        MakeBGMZone("BGM_Shanghai", B1, BW, BH);

        // 主视觉：外滩全景大图（居中上方，漫画风滤镜 + Shadow框）
        MakeFrame("Frame_Bund", B1 - 2, 5.5f, 7, 3.5f, "外滩夜景 · The Bund", new Color(1f, 0.95f, 0.6f, 0.9f), false, testPhoto);
        // 左侧中等竖构图（豫园，走近淡入 + PopArt）
        MakeFrame("Frame_YuGarden", B1 - 10, 2, 3, 3.5f, "豫园 · 九曲桥", Color.white, true, testPhoto);
        // 右下小幅（南京路，霓虹色调）
        MakeFrame("Frame_Nanjing", B1 + 10, 1, 3, 2f, "南京路步行街", new Color(0.9f, 0.8f, 0.6f, 0.85f), false, testPhoto);
        // 左下点缀小图
        MakeFrame("Frame_SH_Alley", B1 - 6, -2, 2.5f, 1.8f, "弄堂里的猫", new Color(0.95f, 0.85f, 0.7f, 0.8f), false, testPhoto);
        // 右上角小图
        MakeFrame("Frame_SH_Food", B1 + 8, 5, 2, 1.5f, "小笼包", new Color(0.95f, 0.9f, 0.8f, 0.8f), false, testPhoto);

        AddFrameFilter("Frame_Bund", GalleryFilter.ArtisticStyle.Comic, 0.6f);
        AddFrameFilter("Frame_YuGarden", GalleryFilter.ArtisticStyle.PopArt, 0.5f);
        AddPhotoFrame("Frame_Bund", 2, new Color(0.15f, 0.15f, 0.2f), 0.05f);
        AddPhotoFrame("Frame_Nanjing", 1, new Color(0.9f, 0.85f, 0.7f), 0.04f);
        AddPhotoFrame("Frame_SH_Alley", 3, new Color(0.95f, 0.93f, 0.9f), 0.05f);

        MakeVideo("Video_Shanghai_Night", B1 + 4, -1, 4.5f, 2.8f, true, true, true, 7f, true, 0.6f, 8f);
        MakeVideo("Video_Pudong", B1 - 12, 5, 3, 2f, true, true, false, 0, false, 0, 0);
        MakePhotoViewer("PhotoViewer_Shanghai", B1 + 13, 4, 2.5f, 2f, new Sprite[]{ testPhoto, testPhoto, testPhoto });
        MakeMemory("Memory_Shanghai", B1 + 6, -5, "黄浦江的风是湿热的。\n城市的灯光倒映在水面上。\n那晚我们走了很久很久。", 0.05f, 0f);
        MakeSign("Sign_Shanghai", B1 + 15, -3, "上海 → 前方：冰岛\n按 Tab 换交通工具", 0f);
        MakeCollectible("弄堂猫咪", B1 - 14, -4, 2, new Color(0.9f, 0.6f, 0.3f), false);
        MakeCollectible("小笼包", B1 + 6, 8, 3, new Color(0.95f, 0.85f, 0.6f), true);

        // ━━━━━━━━━━ Block 2（x≈72）: 2022-2023 · 冰岛 — 冷色·空灵 ━━━━━━━━━━

        MakeAreaTitle("AreaTitle_Iceland", B2, "冰岛", "Iceland · 2022-2023", new Color(0.7f, 0.9f, 1f), BW, BH);
        MakeParticles("Particles_Snow_B2", B2, 1, 55, new Color(0.92f, 0.95f, 1f, 0.5f), false, BW, BH);
        MakeWeather("Weather_Snow_B2", B2, 1, 70, new Color(0.95f, 0.97f, 1f, 0.6f), 0.8f, BW, BH);
        MakeGround("Ground_Sand_B2", B2, 0, BW, BH, 1);
        MakeBGMZone("BGM_Iceland", B2, BW, BH);

        // 主视觉：冰川大图（右侧偏上，印象派 + 简约白框）
        MakeFrame("Frame_Glacier", B2 + 4, 5, 6, 4f, "冰川徒步 · Glacier Walk", new Color(0.85f, 0.95f, 1f, 0.9f), true, testPhoto);
        // 左侧中等（教堂，走近淡入 + 点彩画风）
        MakeFrame("Frame_Church", B2 - 8, 4, 4, 3f, "草帽山教堂 · Kirkjufell", new Color(0.7f, 0.85f, 1f, 0.85f), false, testPhoto);
        // 中下小幅（黑沙滩，炭笔风）
        MakeFrame("Frame_BlackSand", B2 - 2, -1, 3.5f, 2f, "黑沙滩 · Reynisfjara", Color.white, false, testPhoto);
        // 最右小点缀
        MakeFrame("Frame_Iceland_Puffin", B2 + 13, 2, 2, 1.8f, "海鹦", new Color(0.8f, 0.9f, 1f, 0.8f), true, testPhoto);
        // 左下角小幅
        MakeFrame("Frame_Iceland_Cabin", B2 - 12, 0, 2.5f, 1.8f, "冰岛小屋", new Color(0.9f, 0.92f, 0.95f, 0.7f), false, testPhoto);

        AddFrameFilter("Frame_Glacier", GalleryFilter.ArtisticStyle.Impressionist, 0.55f);
        AddFrameFilter("Frame_Church", GalleryFilter.ArtisticStyle.Pointillism, 0.5f);
        AddFrameFilter("Frame_BlackSand", GalleryFilter.ArtisticStyle.Charcoal, 0.65f);
        AddPhotoFrame("Frame_Glacier", 1, new Color(0.95f, 0.97f, 1f), 0.05f);
        AddPhotoFrame("Frame_Church", 3, new Color(0.9f, 0.93f, 0.97f), 0.06f);
        AddPhotoFrame("Frame_Iceland_Cabin", 2, new Color(0.3f, 0.35f, 0.4f), 0.04f);

        MakeParallax("Parallax_Iceland", B2 - 4, 6, "", 0.01f, 0.1f);
        MakeVideo("Video_Aurora", B2 + 10, -2, 5, 3f, true, true, true, 8f, true, 0.6f, 8f);
        MakeVideo("Video_Waterfall", B2 - 12, 5, 3, 2f, true, true, false, 0, true, 0.8f, 3f);
        MakePhotoViewer("PhotoViewer_Iceland", B2 - 6, -4, 3, 2f, new Sprite[]{ testPhoto, testPhoto, testPhoto });
        MakeMemory("Memory_Iceland", B2 + 8, -5, "七月的冰岛，太阳几乎不落。\n站在黑沙滩上，浪花是冰冷的。\n极光要等到冬天才有。", 0.05f, 0f);
        MakeSign("Sign_Iceland", B2 + 15, -3, "冰岛 → 前方：未来\n按 I 查看收集品", 0f);
        MakeCollectible("极光碎片", B2 - 14, 7, 4, new Color(0.4f, 0.8f, 1f), true);
        MakeCollectible("冰川石", B2 + 14, -5, 5, new Color(0.7f, 0.85f, 0.95f), false);

        // ━━━━━━━━━━ Block 3（x≈108）: 2024-2025 · 未来 — 梦幻·赛博 ━━━━━━━━━━

        MakeAreaTitle("AreaTitle_Future", B3, "未来", "Future · 2024-2025", new Color(0.9f, 0.7f, 1f), BW, BH);
        MakeParticles("Particles_Cyber_B3", B3, 2, 35, new Color(0.8f, 0.5f, 1f, 0.5f), false, BW, BH);
        MakeWeather("Weather_Sunbeam_B3", B3, 3, 20, new Color(1f, 0.95f, 0.8f, 0.25f), 0.4f, BW, BH);
        MakeGround("Ground_Grass_B3", B3, 0, BW, BH, 3);
        MakeBGMZone("BGM_Future", B3, BW, BH);

        // 主视觉：中央大图（Glitch 动态滤镜 — 会动的！）
        MakeFrame("Frame_Future_Main", B3 - 2, 5, 5.5f, 3.5f, "未知的旅途 · The Unknown", new Color(0.85f, 0.75f, 1f, 0.9f), false, testPhoto);
        // 左侧 VHS 风格（也会动！）
        MakeFrame("Frame_Future_VHS", B3 - 10, 2, 3.5f, 2.5f, "回忆录像带", new Color(0.8f, 0.7f, 0.9f, 0.8f), true, testPhoto);
        // 右侧像素风
        MakeFrame("Frame_Future_Pixel", B3 + 8, 4, 3, 2.5f, "8-bit 旅行", new Color(0.7f, 0.9f, 0.7f, 0.8f), false, testPhoto);
        // 中下小幅（油画风）
        MakeFrame("Frame_Future_Oil", B3 + 4, -2, 3.5f, 2.5f, "下一站", new Color(0.9f, 0.8f, 1f, 0.7f), false, testPhoto);
        // 左下拼贴风小幅
        MakeFrame("Frame_Future_Mosaic", B3 - 6, -3, 2, 2f, "碎片", new Color(0.85f, 0.85f, 0.95f, 0.7f), true, testPhoto);

        AddFrameFilter("Frame_Future_Main", GalleryFilter.ArtisticStyle.Glitch, 0.7f);
        AddFrameFilter("Frame_Future_VHS", GalleryFilter.ArtisticStyle.VHS, 0.75f);
        AddFrameFilter("Frame_Future_Pixel", GalleryFilter.ArtisticStyle.PixelArt, 0.85f);
        AddFrameFilter("Frame_Future_Oil", GalleryFilter.ArtisticStyle.OilPainting, 0.6f);
        AddFrameFilter("Frame_Future_Mosaic", GalleryFilter.ArtisticStyle.Mosaic, 0.7f);
        AddPhotoFrame("Frame_Future_Main", 2, new Color(0.2f, 0.15f, 0.25f), 0.05f);
        AddPhotoFrame("Frame_Future_Pixel", 1, new Color(0.3f, 0.9f, 0.3f), 0.04f);
        AddPhotoFrame("Frame_Future_Oil", 3, new Color(0.9f, 0.88f, 0.82f), 0.06f);

        MakeVideo("Video_Future", B3 + 12, 1, 3.5f, 2.5f, false, false, false, 0, false, 0, 0);
        MakeMemory("Memory_Future", B3 - 12, -1, "未来的旅行，还没有开始。\n但路已经在脚下了。\n每一步都在写新的故事。", 0.06f, 0f);
        MakeMemory("Memory_Dream", B3 + 6, -5, "也许明天，也许明年。\n总会再出发的。", 0.05f, 5f);
        MakeSign("Sign_Future", B3 + 15, -3, "旅途仍在继续...\n按 F 打开手电筒探索暗门", 0f);
        MakeCollectible("时光胶囊", B3 - 14, 7, 6, new Color(0.9f, 0.7f, 1f), false);
        MakeCollectible("未来钥匙", B3 + 14, -6, 7, new Color(1f, 0.9f, 0.5f), true);
        MakePhotoViewer("PhotoViewer_Future", B3 + 10, 6, 2.5f, 2f, new Sprite[]{ testPhoto, testPhoto, testPhoto });

        // ━━━━━━━━━━ 推动方块 + 图片门 ━━━━━━━━━━

        var door0 = Instantiate("GalleryImageDoor", new Vector3(B0 + 8, -6, 0));
        if (door0 != null)
        {
            door0.name = "Door_Kyoto_SeasonChange";
            door0.transform.localScale = new Vector3(1.2f, 1.2f, 1);
            var doorComp = door0.GetComponent<GalleryImageDoor>();
            var frameKinkaku = GameObject.Find("Frame_Kinkakuji");
            var frameBamboo  = GameObject.Find("Frame_Bamboo");
            if (doorComp != null)
            {
                var so = new SerializedObject(doorComp);
                so.FindProperty("doorColor").colorValue = new Color(0.8f, 0.5f, 0.2f);
                so.FindProperty("activatedColor").colorValue = new Color(1f, 0.7f, 0.3f);
                so.FindProperty("changeBackground").boolValue = true;
                so.FindProperty("newBackgroundColor").colorValue = new Color(0.25f, 0.12f, 0.05f);
                var swaps = so.FindProperty("frameSwaps");
                int swapCount = (frameKinkaku != null ? 1 : 0) + (frameBamboo != null ? 1 : 0);
                swaps.arraySize = swapCount;
                int idx = 0;
                if (frameKinkaku != null) { var e = swaps.GetArrayElementAtIndex(idx++); e.FindPropertyRelative("targetFrame").objectReferenceValue = frameKinkaku.GetComponent<GalleryFrame>(); e.FindPropertyRelative("newImage").objectReferenceValue = testPhoto; e.FindPropertyRelative("newCaption").stringValue = "金阁寺 · 秋"; }
                if (frameBamboo != null)  { var e = swaps.GetArrayElementAtIndex(idx++); e.FindPropertyRelative("targetFrame").objectReferenceValue = frameBamboo.GetComponent<GalleryFrame>();  e.FindPropertyRelative("newImage").objectReferenceValue = testPhoto; e.FindPropertyRelative("newCaption").stringValue = "竹林 · 夜"; }
                so.ApplyModifiedProperties();
            }
        }
        var pushBlock0 = Instantiate("GalleryPushBlock", new Vector3(B0 + 2, -6, 0));
        if (pushBlock0 != null) { pushBlock0.name = "PushBlock_Kyoto"; var pb = pushBlock0.GetComponent<GalleryPushBlock>(); if (pb != null) { var so = new SerializedObject(pb); so.FindProperty("blockColor").colorValue = new Color(0.9f, 0.6f, 0.2f); so.FindProperty("pushSpeed").floatValue = 5f; so.FindProperty("maxSlideDistance").floatValue = 10f; so.ApplyModifiedProperties(); } }

        var door2 = Instantiate("GalleryImageDoor", new Vector3(B2 + 8, -6, 0));
        if (door2 != null)
        {
            door2.name = "Door_Iceland_AuroraMode";
            door2.transform.localScale = new Vector3(1.2f, 1.2f, 1);
            var doorComp = door2.GetComponent<GalleryImageDoor>();
            var frameChurch = GameObject.Find("Frame_Church");
            if (doorComp != null)
            {
                var so = new SerializedObject(doorComp);
                so.FindProperty("doorColor").colorValue = new Color(0.3f, 0.5f, 0.8f);
                so.FindProperty("activatedColor").colorValue = new Color(0.4f, 0.9f, 1f);
                so.FindProperty("changeBackground").boolValue = true;
                so.FindProperty("newBackgroundColor").colorValue = new Color(0.02f, 0.06f, 0.2f);
                if (frameChurch != null) { var swaps = so.FindProperty("frameSwaps"); swaps.arraySize = 1; var e = swaps.GetArrayElementAtIndex(0); e.FindPropertyRelative("targetFrame").objectReferenceValue = frameChurch.GetComponent<GalleryFrame>(); e.FindPropertyRelative("newImage").objectReferenceValue = testPhoto; e.FindPropertyRelative("newCaption").stringValue = "草帽山教堂 · 极光"; }
                so.ApplyModifiedProperties();
            }
        }
        var pushBlock2 = Instantiate("GalleryPushBlock", new Vector3(B2 + 2, -6, 0));
        if (pushBlock2 != null) { pushBlock2.name = "PushBlock_Iceland"; var pb = pushBlock2.GetComponent<GalleryPushBlock>(); if (pb != null) { var so = new SerializedObject(pb); so.FindProperty("blockColor").colorValue = new Color(0.3f, 0.6f, 0.9f); so.FindProperty("pushSpeed").floatValue = 5f; so.FindProperty("maxSlideDistance").floatValue = 10f; so.ApplyModifiedProperties(); } }

        // ━━━━━━━━━━ 传送门（首尾互通） ━━━━━━━━━━

        var tpTargetEnd = new GameObject("PortalTarget_End"); tpTargetEnd.transform.position = new Vector3(B3, -4, 0);
        var tpTargetStart = new GameObject("PortalTarget_Start"); tpTargetStart.transform.position = new Vector3(B0, -4, 0);

        var portalStart = Instantiate("GalleryPortal", new Vector3(B0 - 15, -4, 0));
        if (portalStart != null) { portalStart.name = "Portal_ToEnd"; var gp = portalStart.GetComponent<GalleryPortal>(); if (gp != null) { var so = new SerializedObject(gp); so.FindProperty("mode").enumValueIndex = 0; so.FindProperty("targetPoint").objectReferenceValue = tpTargetEnd.transform; so.FindProperty("autoTrigger").boolValue = false; so.FindProperty("portalColor").colorValue = new Color(0.9f, 0.5f, 1f, 0.8f); so.ApplyModifiedProperties(); } }
        var portalEnd = Instantiate("GalleryPortal", new Vector3(B3 + 15, -4, 0));
        if (portalEnd != null) { portalEnd.name = "Portal_ToStart"; var gp = portalEnd.GetComponent<GalleryPortal>(); if (gp != null) { var so = new SerializedObject(gp); so.FindProperty("mode").enumValueIndex = 0; so.FindProperty("targetPoint").objectReferenceValue = tpTargetStart.transform; so.FindProperty("autoTrigger").boolValue = false; so.FindProperty("portalColor").colorValue = new Color(0.5f, 0.9f, 1f, 0.8f); so.ApplyModifiedProperties(); } }

        // ━━━━━━━━━━ 视差天空（4 层，更丰富） ━━━━━━━━━━

        var skyGO = new GameObject("ParallaxSky"); skyGO.transform.position = Vector3.zero;
        var sky = skyGO.AddComponent<GalleryParallaxSky>();
        var skySO = new SerializedObject(sky);
        var skyLayers = skySO.FindProperty("layers");
        skyLayers.arraySize = 4;
        var sl0 = skyLayers.GetArrayElementAtIndex(0); sl0.FindPropertyRelative("color").colorValue = new Color(0.65f, 0.7f, 0.85f, 0.1f); sl0.FindPropertyRelative("parallaxFactor").floatValue = 0.015f; sl0.FindPropertyRelative("driftSpeed").floatValue = 0.06f; sl0.FindPropertyRelative("yOffset").floatValue = 6f; sl0.FindPropertyRelative("scale").vector2Value = new Vector2(30f, 4f); sl0.FindPropertyRelative("sortingOrder").intValue = -99;
        var sl1 = skyLayers.GetArrayElementAtIndex(1); sl1.FindPropertyRelative("color").colorValue = new Color(0.75f, 0.78f, 0.9f, 0.12f); sl1.FindPropertyRelative("parallaxFactor").floatValue = 0.03f; sl1.FindPropertyRelative("driftSpeed").floatValue = 0.1f; sl1.FindPropertyRelative("yOffset").floatValue = 5f; sl1.FindPropertyRelative("scale").vector2Value = new Vector2(20f, 2.5f); sl1.FindPropertyRelative("sortingOrder").intValue = -98;
        var sl2 = skyLayers.GetArrayElementAtIndex(2); sl2.FindPropertyRelative("color").colorValue = new Color(0.85f, 0.87f, 0.95f, 0.14f); sl2.FindPropertyRelative("parallaxFactor").floatValue = 0.05f; sl2.FindPropertyRelative("driftSpeed").floatValue = 0.18f; sl2.FindPropertyRelative("yOffset").floatValue = 3.5f; sl2.FindPropertyRelative("scale").vector2Value = new Vector2(14f, 1.8f); sl2.FindPropertyRelative("sortingOrder").intValue = -97;
        var sl3 = skyLayers.GetArrayElementAtIndex(3); sl3.FindPropertyRelative("color").colorValue = new Color(0.92f, 0.93f, 1f, 0.06f); sl3.FindPropertyRelative("parallaxFactor").floatValue = 0.09f; sl3.FindPropertyRelative("driftSpeed").floatValue = 0.3f; sl3.FindPropertyRelative("yOffset").floatValue = 2f; sl3.FindPropertyRelative("scale").vector2Value = new Vector2(8f, 1.2f); sl3.FindPropertyRelative("sortingOrder").intValue = -96;
        skySO.ApplyModifiedProperties();

        // ━━━━━━━━━━ 地面过渡带 ━━━━━━━━━━

        MakeGroundTransition(BW * 0.5f, new Color(0.45f, 0.35f, 0.2f), new Color(0.35f, 0.35f, 0.4f), 8f, 4f);
        MakeGroundTransition(BW * 1.5f, new Color(0.35f, 0.35f, 0.4f), new Color(0.5f, 0.5f, 0.45f), 8f, 4f);
        MakeGroundTransition(BW * 2.5f, new Color(0.5f, 0.5f, 0.45f), new Color(0.25f, 0.35f, 0.2f), 8f, 4f);

        // ━━━━━━━━━━ 拼图小游戏（Block 1） ━━━━━━━━━━

        var jigsawGO = new GameObject("Jigsaw_Shanghai"); jigsawGO.transform.position = new Vector3(B1 - 14, -3, 0);
        var jigsaw = jigsawGO.AddComponent<GalleryJigsaw>();
        var jigSO = new SerializedObject(jigsaw);
        jigSO.FindProperty("columns").intValue = 3; jigSO.FindProperty("rows").intValue = 3;
        jigSO.FindProperty("pieceWorldSize").floatValue = 0.7f; jigSO.FindProperty("interactRange").floatValue = 3f;
        if (testPhoto != null) jigSO.FindProperty("fullImage").objectReferenceValue = testPhoto;
        jigSO.ApplyModifiedProperties();

        // ━━━━━━━━━━ 隐藏房间/暗门 ━━━━━━━━━━

        var hiddenRoomGO = new GameObject("HiddenRoom_Iceland"); hiddenRoomGO.transform.position = new Vector3(B2 - 14, 3, 0); hiddenRoomGO.SetActive(false);
        var hiddenFrame = new GameObject("HiddenFrame_Aurora"); hiddenFrame.transform.SetParent(hiddenRoomGO.transform); hiddenFrame.transform.localPosition = Vector3.zero;
        var hfSR = hiddenFrame.AddComponent<SpriteRenderer>(); hfSR.sprite = testPhoto; hfSR.sortingOrder = 5; hiddenFrame.transform.localScale = new Vector3(3f, 2f, 1f);
        var hiddenMemory = new GameObject("HiddenMemory"); hiddenMemory.transform.SetParent(hiddenRoomGO.transform); hiddenMemory.transform.localPosition = new Vector3(0, -2f, 0);
        var hmTM = hiddenMemory.AddComponent<TextMesh>(); hmTM.text = "秘密：冰川深处的极光"; hmTM.characterSize = 0.06f; hmTM.fontSize = 80; hmTM.anchor = TextAnchor.MiddleCenter; hmTM.color = new Color(0.5f, 0.9f, 1f); hiddenMemory.GetComponent<MeshRenderer>().sortingOrder = 6;

        var secretDoorGO = new GameObject("SecretDoor_Iceland"); secretDoorGO.transform.position = new Vector3(B2 - 14, 3, 0);
        var secretDoor = secretDoorGO.AddComponent<GallerySecretDoor>(); var sdSO = new SerializedObject(secretDoor);
        sdSO.FindProperty("method").enumValueIndex = 0; sdSO.FindProperty("wallColor").colorValue = new Color(0.2f, 0.25f, 0.35f); sdSO.FindProperty("doorSize").vector2Value = new Vector2(3.5f, 3f); sdSO.FindProperty("openDuration").floatValue = 1.2f; sdSO.FindProperty("hiddenRoom").objectReferenceValue = hiddenRoomGO; sdSO.ApplyModifiedProperties();

        var hiddenRoom2 = new GameObject("HiddenRoom_Future"); hiddenRoom2.transform.position = new Vector3(B3 - 14, 3, 0); hiddenRoom2.SetActive(false);
        var hr2Frame = new GameObject("HiddenFrame_Future"); hr2Frame.transform.SetParent(hiddenRoom2.transform); hr2Frame.transform.localPosition = Vector3.zero;
        var hr2SR = hr2Frame.AddComponent<SpriteRenderer>(); hr2SR.sprite = testPhoto; hr2SR.sortingOrder = 5; hr2Frame.transform.localScale = new Vector3(2.5f, 2f, 1f);
        var secretDoor2GO = new GameObject("SecretDoor_Future"); secretDoor2GO.transform.position = new Vector3(B3 - 14, 3, 0);
        var secretDoor2 = secretDoor2GO.AddComponent<GallerySecretDoor>(); var sd2SO = new SerializedObject(secretDoor2);
        sd2SO.FindProperty("method").enumValueIndex = 1; sd2SO.FindProperty("wallColor").colorValue = new Color(0.15f, 0.12f, 0.2f); sd2SO.FindProperty("doorSize").vector2Value = new Vector2(2.5f, 2.5f); sd2SO.FindProperty("openDuration").floatValue = 0.8f; sd2SO.FindProperty("hiddenRoom").objectReferenceValue = hiddenRoom2; sd2SO.ApplyModifiedProperties();

        // ━━━━━━━━━━ NPC 对话（4 个，每区块 1 个） ━━━━━━━━━━

        MakeNPC("NPC_KyotoElder", B0 + 12, 2, new Color(0.6f, 0.4f, 0.3f), true, 12f, new string[]{"欢迎来到京都。", "这里有 1600 多座寺庙呢。", "秋天来最好，满城红叶。"}, new float[]{2.5f, 3f, 2.5f});
        MakeNPC("NPC_Photographer", B1 + 2, -4, new Color(0.3f, 0.5f, 0.7f), false, 18f, new string[]{"嘿，拍到好照片了吗？", "按 Tab 可以换交通工具哦~", "试试旁边的拼图游戏！"}, new float[]{2.5f, 3f, 2.5f});
        MakeNPC("NPC_IcelandGuide", B2 + 6, -4, new Color(0.4f, 0.6f, 0.8f), true, 14f, new string[]{"小心脚下！冰面很滑。", "推一下那边的蓝色方块。", "也许会有什么惊喜..."}, new float[]{2.5f, 3f, 2.5f});
        MakeNPC("NPC_TimeKeeper", B3 + 2, -4, new Color(0.6f, 0.45f, 0.7f), true, 10f, new string[]{"这里是时间线的尽头。", "但每段旅途都没有真正结束。", "用手电筒照照墙壁试试？"}, new float[]{3f, 3.5f, 3f});

        // ━━━━━━━━━━ 推块→暗门的推块 ━━━━━━━━━━

        var pushBlockSecret = new GameObject("PushBlock_Secret"); pushBlockSecret.transform.position = new Vector3(B2 - 8, 3, 0);
        pushBlockSecret.AddComponent<SpriteRenderer>(); pushBlockSecret.AddComponent<BoxCollider2D>(); pushBlockSecret.AddComponent<Rigidbody2D>();
        var pbSecret = pushBlockSecret.AddComponent<GalleryPushBlock>(); var pbSecSO = new SerializedObject(pbSecret);
        pbSecSO.FindProperty("blockColor").colorValue = new Color(0.3f, 0.45f, 0.7f); pbSecSO.FindProperty("pushSpeed").floatValue = 4f; pbSecSO.FindProperty("maxSlideDistance").floatValue = 8f; pbSecSO.FindProperty("size").floatValue = 0.5f; pbSecSO.ApplyModifiedProperties();

        // ━━━━━━━━━━ 时间轴（2018-2025） ━━━━━━━━━━

        var timeline = Instantiate("GalleryTimeline", Vector3.zero);
        if (timeline != null) { timeline.name = "Timeline_2018_2025"; var tl = timeline.GetComponent<GalleryTimeline>(); if (tl != null) { var so = new SerializedObject(tl); var pts = so.FindProperty("points");
            Color[] yc = { new Color(0.95f,0.6f,0.6f), new Color(0.95f,0.75f,0.5f), new Color(0.95f,0.9f,0.5f), new Color(0.6f,0.9f,0.55f), new Color(0.5f,0.85f,0.8f), new Color(0.55f,0.7f,0.95f), new Color(0.7f,0.6f,0.95f), new Color(0.9f,0.6f,0.85f) };
            pts.arraySize = 8; for (int i = 0; i < 8; i++) { float nodeX = B0 - 12f + i * (totalW / 7f); var e = pts.GetArrayElementAtIndex(i); e.FindPropertyRelative("position").vector2Value = new Vector2(nodeX, -8f); e.FindPropertyRelative("dateText").stringValue = (2018 + i).ToString(); e.FindPropertyRelative("color").colorValue = yc[i]; }
            so.ApplyModifiedProperties(); EditorUtility.SetDirty(tl); } }

        // ━━━━━━━━━━ 地面纹理（GalleryGround + 程序化遮罩） ━━━━━━━━━━

        CreateDemoGround(B0, B1, B2, B3, BW, BH, totalW);

        // ━━━━━━━━━━ 小路 ━━━━━━━━━━

        CreateDemoPaths(B0, B1, B2, B3, BW);

        // ━━━━━━━━━━ 边界墙 ━━━━━━━━━━

        float wallCenterX = (B0 + B3) * 0.5f; float wallWidth = totalW + 4f;
        CreateWall("Wall_Top",    new Vector3(wallCenterX,  9.5f, 0), new Vector3(wallWidth, 1, 1));
        CreateWall("Wall_Bottom", new Vector3(wallCenterX, -9.5f, 0), new Vector3(wallWidth, 1, 1));
        CreateWall("Wall_Left",   new Vector3(B0 - BW * 0.5f - 0.5f, 0, 0), new Vector3(1, BH + 1, 1));
        CreateWall("Wall_Right",  new Vector3(B3 + BW * 0.5f + 0.5f, 0, 0), new Vector3(1, BH + 1, 1));

        // ━━━━━━━━━━ 整理 Hierarchy ━━━━━━━━━━

        OrganizeHierarchy(B0, B1, B2, B3, BW);

        // ━━━━━━━━━━ 保存 ━━━━━━━━━━

        EditorSceneManager.MarkSceneDirty(scene);
        string folder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Scenes");
        string path = "Assets/Scenes/gallery.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log(
            "═══ Gallery 示范场景已创建（4屏宽·全功能·高完成度） ═══\n" +
            "路径: " + path + "\n\n" +
            "【Block 0 · 京都】5 张照片(浮世绘/水彩/炭笔) · Polaroid+白框 · 视差\n" +
            "【Block 1 · 上海】5 张照片(漫画/波普) · Shadow框 · 拼图 · 视频×2\n" +
            "【Block 2 · 冰岛】5 张照片(印象派/点彩/炭笔) · 暗门(推块) · 视频×2\n" +
            "【Block 3 · 未来】5 张照片(Glitch/VHS/像素/油画/马赛克) · 暗门(手电筒)\n\n" +
            "每区块：照片×5 + NPC×1 + 粒子+天气+地面 + 收集品×2\n" +
            "全局：视差天空4层 · 地面过渡带×3 · 时间轴 · 传送门×2");
    }

    // ── 场景生成辅助方法 ──

    private static void MakeAreaTitle(string name, float cx, string title, string subtitle, Color color, float w, float h)
    {
        var go = Instantiate("GalleryAreaTitle", new Vector3(cx, 0, 0));
        if (go == null) return;
        go.name = name;
        SetBoxSize(go, w, h);
        var at = go.GetComponent<GalleryAreaTitle>();
        if (at == null) return;
        var so = new SerializedObject(at);
        so.FindProperty("title").stringValue = title;
        so.FindProperty("subtitle").stringValue = subtitle;
        so.FindProperty("textColor").colorValue = color;
        so.ApplyModifiedProperties();
    }

    private static void MakeParticles(string name, float cx, int styleIdx, int count, Color color, bool triggerEnter, float w, float h)
    {
        var go = Instantiate("GalleryAreaParticles", new Vector3(cx, 0, 0));
        if (go == null) return;
        go.name = name;
        SetBoxSize(go, w, h);
        var ap = go.GetComponent<GalleryAreaParticles>();
        if (ap == null) return;
        var so = new SerializedObject(ap);
        so.FindProperty("style").enumValueIndex = styleIdx;
        so.FindProperty("particleCount").intValue = count;
        so.FindProperty("particleColor").colorValue = color;
        so.FindProperty("activateOnEnter").boolValue = triggerEnter;
        so.ApplyModifiedProperties();
    }

    private static void MakeWeather(string name, float cx, int typeIdx, int count, Color color, float intensity, float w, float h)
    {
        var go = Instantiate("GalleryWeather", new Vector3(cx, 0, 0));
        if (go == null) return;
        go.name = name;
        SetBoxSize(go, w, h);
        var wc = go.GetComponent<GalleryWeather>();
        if (wc == null) return;
        var so = new SerializedObject(wc);
        so.FindProperty("weatherType").enumValueIndex = typeIdx;
        so.FindProperty("particleCount").intValue = count;
        so.FindProperty("particleColor").colorValue = color;
        so.FindProperty("intensity").floatValue = intensity;
        so.ApplyModifiedProperties();
    }

    private static void MakeFrame(string name, float x, float y, float w, float h, string caption, Color captionColor, bool fadeIn,
        Sprite placeholder = null)
    {
        var go = Instantiate("GalleryFrame", new Vector3(x, y, 0));
        if (go == null) return;
        go.name = name;
        go.transform.localScale = new Vector3(w, h, 1);
        var gf = go.GetComponent<GalleryFrame>();
        if (gf == null) return;
        var so = new SerializedObject(gf);
        so.FindProperty("caption").stringValue = caption;
        so.FindProperty("captionColor").colorValue = captionColor;
        if (fadeIn)
        {
            so.FindProperty("fadeInOnApproach").boolValue = true;
            so.FindProperty("fadeDistance").floatValue = 6f;
            so.FindProperty("fadeSpeed").floatValue = 1.5f;
        }
        if (placeholder != null)
            so.FindProperty("image").objectReferenceValue = placeholder;
        so.ApplyModifiedProperties();
    }

    private static void MakeVideo(string name, float x, float y, float w, float h,
        bool auto, bool loop, bool fadeIn, float fadeDist,
        bool audio, float vol, float audioRange)
    {
        var go = Instantiate("GalleryVideo", new Vector3(x, y, 0));
        if (go == null) return;
        go.name = name;
        go.transform.localScale = new Vector3(w, h, 1);
        var gv = go.GetComponent<GalleryVideo>();
        if (gv == null) return;
        var so = new SerializedObject(gv);
        so.FindProperty("autoPlay").boolValue = auto;
        so.FindProperty("loop").boolValue = loop;
        so.FindProperty("fadeInOnApproach").boolValue = fadeIn;
        if (fadeIn) so.FindProperty("fadeDistance").floatValue = fadeDist;
        so.FindProperty("enableAudio").boolValue = audio;
        if (audio)
        {
            so.FindProperty("maxVolume").floatValue = vol;
            so.FindProperty("audioRange").floatValue = audioRange;
        }
        so.ApplyModifiedProperties();
    }

    private static void MakePhotoViewer(string name, float x, float y, float w, float h, Sprite[] photos = null)
    {
        var go = Instantiate("GalleryPhotoViewer", new Vector3(x, y, 0));
        if (go == null) return;
        go.name = name;
        go.transform.localScale = new Vector3(w, h, 1);
        if (photos != null && photos.Length > 0)
        {
            var pv = go.GetComponent<GalleryPhotoViewer>();
            if (pv != null)
            {
                var so = new SerializedObject(pv);
                var prop = so.FindProperty("photos");
                prop.arraySize = photos.Length;
                for (int i = 0; i < photos.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = photos[i];
                so.ApplyModifiedProperties();
            }
        }
    }

    private static void MakeMemory(string name, float x, float y, string text, float speed, float autoClose)
    {
        var go = Instantiate("GalleryMemory", new Vector3(x, y, 0));
        if (go == null) return;
        go.name = name;
        var gm = go.GetComponent<GalleryMemory>();
        if (gm == null) return;
        var so = new SerializedObject(gm);
        so.FindProperty("memoryText").stringValue = text;
        so.FindProperty("typeSpeed").floatValue = speed;
        so.FindProperty("autoCloseDelay").floatValue = autoClose;
        so.ApplyModifiedProperties();
    }

    private static void MakeSign(string name, float x, float y, string text, float duration)
    {
        var go = Instantiate("GallerySign", new Vector3(x, y, 0));
        if (go == null) return;
        go.name = name;
        var gs = go.GetComponent<GallerySign>();
        if (gs == null) return;
        var so = new SerializedObject(gs);
        so.FindProperty("text").stringValue = text;
        so.FindProperty("displayDuration").floatValue = duration;
        so.ApplyModifiedProperties();
    }

    private static void MakeGround(string name, float cx, float cy, float w, float h, int materialIdx)
    {
        var go = Instantiate("GalleryGroundType", new Vector3(cx, cy, 0));
        if (go == null) return;
        go.name = name;
        SetBoxSize(go, w, h);
        var gt = go.GetComponent<GalleryGroundType>();
        if (gt == null) return;
        var so = new SerializedObject(gt);
        so.FindProperty("material").enumValueIndex = materialIdx;
        so.ApplyModifiedProperties();
    }

    private static void MakeBGMZone(string name, float cx, float w, float h)
    {
        var go = Instantiate("GalleryBGMZone", new Vector3(cx, 0, 0));
        if (go == null) return;
        go.name = name;
        SetBoxSize(go, w, h);
    }

    private static void MakeParallax(string name, float x, float y, string caption, float bgFactor, float fgFactor)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(x, y, 0);
        go.AddComponent<GalleryParallaxFrame>();
        var pf = go.GetComponent<GalleryParallaxFrame>();
        if (pf == null) return;
        var testSprite = LoadTestPhoto();
        var so = new SerializedObject(pf);
        so.FindProperty("bgParallaxFactor").floatValue = bgFactor;
        so.FindProperty("fgParallaxFactor").floatValue = fgFactor;
        so.FindProperty("maxOffset").floatValue = 0.4f;
        so.FindProperty("caption").stringValue = caption;
        if (testSprite != null)
        {
            so.FindProperty("backgroundLayer").objectReferenceValue = testSprite;
            so.FindProperty("foregroundLayer").objectReferenceValue = testSprite;
        }
        so.ApplyModifiedProperties();
    }

    private static void MakeCollectible(string displayName, float x, float y, int idx, Color color, bool hidden)
    {
        var go = Instantiate("GalleryCollectible", new Vector3(x, y, 0));
        if (go == null)
        {
            go = new GameObject("Collect_" + displayName);
            go.transform.position = new Vector3(x, y, 0);
            go.AddComponent<GalleryCollectible>();
        }
        go.name = "Collect_" + displayName;
        var gc = go.GetComponent<GalleryCollectible>();
        if (gc == null) return;
        var so = new SerializedObject(gc);
        so.FindProperty("collectibleID").stringValue = "collect_demo_" + idx;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("itemColor").colorValue = color;
        so.FindProperty("hidden").boolValue = hidden;
        so.FindProperty("pickupMessage").stringValue = "获得：" + displayName;
        so.FindProperty("size").floatValue = hidden ? 0.2f : 0.3f;
        so.ApplyModifiedProperties();
    }

    private static void AddFrameFilter(string frameName, GalleryFilter.ArtisticStyle style, float intensity)
    {
        var go = GameObject.Find(frameName);
        if (go == null) return;
        var filter = go.AddComponent<GalleryFilter>();
        var so = new SerializedObject(filter);
        so.FindProperty("artisticStyle").enumValueIndex = (int)style;
        so.FindProperty("styleIntensity").floatValue = intensity;
        so.ApplyModifiedProperties();
    }

    private static void AddPhotoFrame(string frameName, int styleIdx, Color color, float thickness)
    {
        var go = GameObject.Find(frameName);
        if (go == null) return;
        var pf = go.AddComponent<GalleryPhotoFrame>();
        var so = new SerializedObject(pf);
        so.FindProperty("style").enumValueIndex = styleIdx;
        so.FindProperty("frameColor").colorValue = color;
        so.FindProperty("borderThickness").floatValue = thickness;
        so.ApplyModifiedProperties();
    }

    private static void MakeGroundTransition(float x, Color left, Color right, float width, float height)
    {
        var go = new GameObject("GroundTransition_" + (int)x);
        go.transform.position = new Vector3(x, -7f, 0);
        var gt = go.AddComponent<GalleryGroundTransition>();
        var so = new SerializedObject(gt);
        so.FindProperty("transitionWidth").floatValue = width;
        so.FindProperty("leftColor").colorValue = left;
        so.FindProperty("rightColor").colorValue = right;
        so.FindProperty("height").floatValue = height;
        so.ApplyModifiedProperties();
    }

    private static void CreateDemoGround(float B0, float B1, float B2, float B3, float BW, float BH, float totalW)
    {
        float centerX = (B0 + B3) * 0.5f;
        float gw = totalW + 4f;
        float gh = BH + 2f;

        var groundGO = new GameObject("GalleryGround");
        groundGO.transform.position = new Vector3(centerX, 0, 0);
        var ground = groundGO.AddComponent<GalleryGround>();

        int res = 512;
        string dir = "Assets/Gallery/GroundTextures";
        if (!AssetDatabase.IsValidFolder("Assets/Gallery"))
            AssetDatabase.CreateFolder("Assets", "Gallery");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Gallery", "GroundTextures");

        Color[] blockColors = {
            new Color(0.45f, 0.32f, 0.2f),
            new Color(0.3f, 0.35f, 0.32f),
            new Color(0.42f, 0.45f, 0.5f),
            new Color(0.25f, 0.38f, 0.22f)
        };
        Color baseCol = new Color(0.2f, 0.18f, 0.16f);

        float[] blockNorms = {
            (B0 - centerX + gw * 0.5f) / gw,
            (B1 - centerX + gw * 0.5f) / gw,
            (B2 - centerX + gw * 0.5f) / gw,
            (B3 - centerX + gw * 0.5f) / gw
        };

        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int py = 0; py < res; py++)
        {
            for (int px = 0; px < res; px++)
            {
                float u = (float)px / res;
                float v = (float)py / res;
                float noise = Mathf.PerlinNoise(u * 8f, v * 8f) * 0.08f;

                float bestW = 0f;
                Color col = baseCol;
                for (int i = 0; i < 4; i++)
                {
                    float d = Mathf.Abs(u - blockNorms[i]) * 4f;
                    float nOff = Mathf.PerlinNoise(u * 5f + i * 30f, v * 5f + i * 30f) * 0.3f;
                    float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1.2f - d + nOff));
                    if (w > bestW) { bestW = w; col = Color.Lerp(baseCol, blockColors[i], w); }
                }
                col.r += noise - 0.04f;
                col.g += noise - 0.04f;
                col.b += noise - 0.04f;
                col.a = 1f;
                tex.SetPixel(px, py, col);
            }
        }
        tex.Apply();

        string texPath = AssetDatabase.GenerateUniqueAssetPath(dir + "/DemoGround.png");
        System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath);

        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = res;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        var so = new SerializedObject(ground);
        so.FindProperty("groundWidth").floatValue = gw;
        so.FindProperty("groundHeight").floatValue = gh;
        so.FindProperty("groundTexture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        so.FindProperty("sortingOrder").intValue = -10;
        so.ApplyModifiedProperties();
    }

    private static void OrganizeHierarchy(float B0, float B1, float B2, float B3, float BW)
    {
        string[] blockLabels = { "Block 0 · 京都", "Block 1 · 上海", "Block 2 · 冰岛", "Block 3 · 未来" };
        float[] blockCenters = { B0, B1, B2, B3 };

        var globalGroup = new GameObject("── 全局 ──");
        var envGroup = new GameObject("── 环境 · 地面/天空/小路 ──");
        var wallGroup = new GameObject("── 边界墙 ──");

        var blockRoots = new GameObject[4];
        var subMedia = new GameObject[4];
        var subFx = new GameObject[4];
        var subInteract = new GameObject[4];
        var subEnv = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            blockRoots[i] = new GameObject($"── {blockLabels[i]} ──");
            subMedia[i] = new GameObject("图片·视频");
            subMedia[i].transform.SetParent(blockRoots[i].transform);
            subFx[i] = new GameObject("特效·粒子");
            subFx[i].transform.SetParent(blockRoots[i].transform);
            subInteract[i] = new GameObject("交互·NPC");
            subInteract[i].transform.SetParent(blockRoots[i].transform);
            subEnv[i] = new GameObject("环境·音乐");
            subEnv[i].transform.SetParent(blockRoots[i].transform);
        }

        var groupObjects = new System.Collections.Generic.HashSet<GameObject> {
            globalGroup, envGroup, wallGroup
        };
        for (int i = 0; i < 4; i++)
        {
            groupObjects.Add(blockRoots[i]);
            groupObjects.Add(subMedia[i]);
            groupObjects.Add(subFx[i]);
            groupObjects.Add(subInteract[i]);
            groupObjects.Add(subEnv[i]);
        }

        string[] globalNames = { "GalleryManager", "GalleryPlayer", "GalleryBackground", "Main Camera",
            "Directional Light", "Follower_Companion" };
        string[] globalEnvNames = { "ParallaxSky", "GalleryGround", "Timeline_", "GroundTransition_", "Path_" };
        string[] mediaKeys = { "Frame_", "Video_", "PhotoViewer_", "Parallax_", "Slideshow_" };
        string[] fxKeys = { "Particles_", "Weather_" };
        string[] interactKeys = { "NPC_", "Memory_", "Sign_", "PushBlock", "Door_", "Jigsaw",
            "SecretDoor", "HiddenRoom", "Portal", "Collectible_" };
        string[] blockEnvKeys = { "AreaTitle_", "BGM_", "Ground_" };

        var allRoot = new System.Collections.Generic.List<GameObject>();
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.transform.parent == null && !groupObjects.Contains(go))
                allRoot.Add(go);
        }

        foreach (var go in allRoot)
        {
            string n = go.name;

            if (MatchAny(n, globalNames)) { go.transform.SetParent(globalGroup.transform); continue; }
            if (n.StartsWith("Wall_")) { go.transform.SetParent(wallGroup.transform); continue; }
            if (MatchAny(n, globalEnvNames)) { go.transform.SetParent(envGroup.transform); continue; }

            int block = FindBlock(go.transform.position.x, blockCenters);

            if (MatchAny(n, mediaKeys)) { go.transform.SetParent(subMedia[block].transform); continue; }
            if (MatchAny(n, fxKeys)) { go.transform.SetParent(subFx[block].transform); continue; }
            if (MatchAny(n, interactKeys)) { go.transform.SetParent(subInteract[block].transform); continue; }
            if (MatchAny(n, blockEnvKeys)) { go.transform.SetParent(subEnv[block].transform); continue; }

            bool hasCollectible = go.GetComponent<GalleryCollectible>() != null;
            if (hasCollectible) { go.transform.SetParent(subInteract[block].transform); continue; }

            go.transform.SetParent(subEnv[block].transform);
        }
    }

    private static bool MatchAny(string name, string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
            if (name.Contains(keys[i])) return true;
        return false;
    }

    private static int FindBlock(float x, float[] centers)
    {
        int best = 0; float bestD = float.MaxValue;
        for (int i = 0; i < centers.Length; i++)
        {
            float d = Mathf.Abs(x - centers[i]);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private static void CreateDemoPaths(float B0, float B1, float B2, float B3, float BW)
    {
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/PathMaterials/StonePath.mat");
        if (stoneMat == null)
        {
            PathTextureGenerator.GenerateStonePath();
            stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/PathMaterials/StonePath.mat");
        }

        MakePath("Path_Main", Vector3.zero, new Vector2[]
        {
            new Vector2(B0 - BW*0.4f, 0f),
            new Vector2(B0 - 5f, 0.3f),
            new Vector2(B0, 0f),
            new Vector2(B0 + 8f, -0.5f),
            new Vector2(B1 - 8f, 0.2f),
            new Vector2(B1, 0f),
            new Vector2(B1 + 8f, -0.3f),
            new Vector2(B2 - 8f, 0.4f),
            new Vector2(B2, 0f),
            new Vector2(B2 + 8f, -0.2f),
            new Vector2(B3 - 8f, 0.3f),
            new Vector2(B3, 0f),
            new Vector2(B3 + BW*0.4f, 0.2f),
        }, 0.35f, new Color(0.93f, 0.91f, 0.87f), -4, stoneMat);

        MakePath("Path_B0_Branch", Vector3.zero, new Vector2[]
        {
            new Vector2(B0 - 2f, 0f),
            new Vector2(B0 - 3f, 2f),
            new Vector2(B0 - 1f, 4.5f),
            new Vector2(B0 + 2f, 6f),
        }, 0.18f, new Color(0.88f, 0.86f, 0.82f), -4);

        MakePath("Path_B0_South", Vector3.zero, new Vector2[]
        {
            new Vector2(B0 + 3f, 0f),
            new Vector2(B0 + 4f, -2f),
            new Vector2(B0 + 2f, -4.5f),
            new Vector2(B0 - 1f, -6f),
        }, 0.18f, new Color(0.88f, 0.86f, 0.82f), -4);

        MakePath("Path_B1_Upper", Vector3.zero, new Vector2[]
        {
            new Vector2(B1, 0f),
            new Vector2(B1 + 3f, 2.5f),
            new Vector2(B1 + 6f, 4f),
            new Vector2(B1 + 10f, 3.5f),
        }, 0.2f, new Color(0.9f, 0.88f, 0.84f), -4);

        MakePath("Path_B2_Loop", Vector3.zero, new Vector2[]
        {
            new Vector2(B2 - 4f, 0f),
            new Vector2(B2 - 6f, 3f),
            new Vector2(B2 - 2f, 5.5f),
            new Vector2(B2 + 3f, 5f),
            new Vector2(B2 + 5f, 2.5f),
            new Vector2(B2 + 3f, 0f),
        }, 0.22f, new Color(0.9f, 0.9f, 0.88f), -4);

        MakePath("Path_B3_Branch", Vector3.zero, new Vector2[]
        {
            new Vector2(B3 - 2f, 0f),
            new Vector2(B3 - 4f, -2f),
            new Vector2(B3 - 2f, -5f),
            new Vector2(B3 + 1f, -6.5f),
        }, 0.18f, new Color(0.85f, 0.84f, 0.8f), -4);
    }

    private static void MakePath(string name, Vector3 pos, Vector2[] pts, float width, Color color, int sortOrder, Material mat = null)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var path = go.AddComponent<GalleryPath>();

        var so = new SerializedObject(path);
        so.FindProperty("pathColor").colorValue = color;
        so.FindProperty("pathWidth").floatValue = width;
        so.FindProperty("smoothSegments").intValue = 10;
        so.FindProperty("sortingOrder").intValue = sortOrder;
        so.FindProperty("edgeSoftness").floatValue = mat != null ? 0f : 0.05f;
        if (mat != null)
        {
            so.FindProperty("customMaterial").objectReferenceValue = mat;
            so.FindProperty("textureMode").enumValueIndex = (int)LineTextureMode.Tile;
        }

        var ptsProp = so.FindProperty("points");
        ptsProp.arraySize = pts.Length;
        for (int i = 0; i < pts.Length; i++)
            ptsProp.GetArrayElementAtIndex(i).vector2Value = pts[i];

        so.ApplyModifiedProperties();
    }

    private static Sprite LoadNPCSprite()
    {
        var settings = GetSettings();
        if (settings != null && settings.npcSprite != null)
            return settings.npcSprite;
        return ImportSpriteAt("Assets/Photo/test2.png", 1f);
    }

    private static void MakeNPC(string name, float x, float y, Color color, bool auto, float speed, string[] texts, float[] durations)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(x, y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        var npcSprite = LoadNPCSprite();
        sr.sprite = npcSprite != null ? npcSprite : RuntimeSprite.Get();
        sr.color = Color.white;
        sr.sortingOrder = 9;
        go.transform.localScale = Vector3.one * 0.6f;
        var dlg = go.AddComponent<GalleryNPCDialogue>();
        var so = new SerializedObject(dlg);
        so.FindProperty("autoTrigger").boolValue = auto;
        so.FindProperty("triggerDistance").floatValue = 2.5f;
        so.FindProperty("typeSpeed").floatValue = speed;
        var lines = so.FindProperty("lines");
        lines.arraySize = texts.Length;
        for (int i = 0; i < texts.Length; i++)
        {
            lines.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue = texts[i];
            lines.GetArrayElementAtIndex(i).FindPropertyRelative("duration").floatValue = durations[i];
        }
        so.ApplyModifiedProperties();
    }

    private static void SetBoxSize(GameObject go, float w, float h)
    {
        var col = go.GetComponent<BoxCollider2D>();
        if (col != null) col.size = new Vector2(w, h);
    }

    // ────── helpers ──────

    private static void CreatePrefabIfNeeded(string name, System.Action<GameObject> setup)
    {
        string path = GalleryPrefabFolder + "/" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var go = new GameObject(name);
        setup(go);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void EnsureGallerySettings()
    {
        string resFolder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resFolder))
            AssetDatabase.CreateFolder("Assets", "Resources");

        string path = resFolder + "/GallerySettings.asset";
        if (AssetDatabase.LoadAssetAtPath<GallerySettings>(path) != null) return;

        var asset = ScriptableObject.CreateInstance<GallerySettings>();
        AssetDatabase.CreateAsset(asset, path);
    }

    private static GameObject Instantiate(string name, Vector3 pos)
    {
        string prefabPath = GalleryPrefabFolder + "/" + name + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            return inst;
        }
        return null;
    }

    private static void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = Instantiate("GalleryWall", pos);
        if (wall == null)
        {
            wall = new GameObject(name);
            wall.AddComponent<GalleryWall>();
            wall.transform.position = pos;
        }
        wall.name = name;
        wall.transform.localScale = scale;
    }

    // ────── Gallery: Filter Preview Scene ──────

    [MenuItem("Tools/Gallery/创建滤镜预览场景")]
    public static void CreateFilterPreviewScene()
    {
        EnsureGalleryFolder();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 12;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            cam.transform.position = new Vector3(14f, -6f, -10f);
        }

        var sampleSprite = LoadFilterPreviewPhoto();
        if (sampleSprite == null)
        {
            Debug.LogError("请在 GallerySettings 的 filterPreviewImage 中拖入图片，或放一张 Assets/Photo/cat.png！");
            return;
        }

        string[] styleNames = System.Enum.GetNames(typeof(GalleryFilter.ArtisticStyle));
        string[] colorNames = System.Enum.GetNames(typeof(GalleryFilter.ColorFilter));

        float spriteW = sampleSprite.bounds.size.x;
        float spriteH = sampleSprite.bounds.size.y;

        float targetW = 3.8f;
        float targetH = 2.85f;
        float cardScaleX = targetW / Mathf.Max(spriteW, 0.01f);
        float cardScaleY = targetH / Mathf.Max(spriteH, 0.01f);

        int cols = 7;
        float spacingX = 4.5f;
        float labelOffset = 0.35f;
        float spacingY = targetH + labelOffset + 1.2f;
        float cardW = cardScaleX;
        float cardH = cardScaleY;

        // ── Section: Artistic Styles ──
        var titleArt = new GameObject("Title_Artistic");
        titleArt.transform.position = new Vector3(-1f, 2f, 0);
        var tmArt = titleArt.AddComponent<TextMesh>();
        tmArt.text = "== 风格化滤镜 ==  （点击查看大图）";
        tmArt.characterSize = 0.15f;
        tmArt.fontSize = 80;
        tmArt.anchor = TextAnchor.MiddleLeft;
        tmArt.color = new Color(1f, 0.9f, 0.6f);
        titleArt.GetComponent<MeshRenderer>().sortingOrder = 20;

        for (int i = 0; i < styleNames.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = col * spacingX;
            float y = -row * spacingY;
            string styleName = styleNames[i];

            var go = new GameObject("Filter_" + styleName);
            go.transform.position = new Vector3(x, y, 0);
            go.transform.localScale = new Vector3(cardW, cardH, 1);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sampleSprite;
            sr.color = Color.white;
            sr.sortingOrder = 1;

            var viewer = go.AddComponent<FilterPreviewViewer>();
            if (i > 0)
            {
                var filter = go.AddComponent<GalleryFilter>();
                var so = new SerializedObject(filter);
                so.FindProperty("artisticStyle").enumValueIndex = i;
                so.FindProperty("styleIntensity").floatValue = 0.85f;
                so.ApplyModifiedProperties();

                var vso = new SerializedObject(viewer);
                vso.FindProperty("artisticStyle").enumValueIndex = i;
                vso.FindProperty("styleIntensity").floatValue = 0.85f;
                vso.ApplyModifiedProperties();
            }

            var labelGO = new GameObject("Label");
            labelGO.transform.position = new Vector3(x, y - targetH * 0.5f - labelOffset, 0);
            var tm = labelGO.AddComponent<TextMesh>();
            tm.text = i == 0 ? "Original" : styleName;
            tm.characterSize = 0.08f;
            tm.fontSize = 80;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            labelGO.GetComponent<MeshRenderer>().sortingOrder = 10;
        }

        // ── Section: Color Filters ──
        int artRows = (styleNames.Length + cols - 1) / cols;
        float colorStartY = -(artRows) * spacingY - 2f;

        var titleColor = new GameObject("Title_Color");
        titleColor.transform.position = new Vector3(-1f, colorStartY + 2f, 0);
        var tmColor = titleColor.AddComponent<TextMesh>();
        tmColor.text = "== 颜色滤镜 ==";
        tmColor.characterSize = 0.15f;
        tmColor.fontSize = 80;
        tmColor.anchor = TextAnchor.MiddleLeft;
        tmColor.color = new Color(0.7f, 0.9f, 1f);
        titleColor.GetComponent<MeshRenderer>().sortingOrder = 20;

        for (int i = 0; i < colorNames.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = col * spacingX;
            float y = colorStartY - row * spacingY;
            string filterName = colorNames[i];

            var go = new GameObject("Color_" + filterName);
            go.transform.position = new Vector3(x, y, 0);
            go.transform.localScale = new Vector3(cardW, cardH, 1);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sampleSprite;
            sr.color = Color.white;
            sr.sortingOrder = 1;

            var viewer = go.AddComponent<FilterPreviewViewer>();
            if (i > 0)
            {
                var filter = go.AddComponent<GalleryFilter>();
                var so = new SerializedObject(filter);
                so.FindProperty("colorFilter").enumValueIndex = i;
                so.FindProperty("colorIntensity").floatValue = 0.75f;
                so.FindProperty("vignette").boolValue = true;
                so.FindProperty("vignetteStrength").floatValue = 0.25f;
                so.ApplyModifiedProperties();

                var vso = new SerializedObject(viewer);
                vso.FindProperty("colorFilter").enumValueIndex = i;
                vso.FindProperty("colorIntensity").floatValue = 0.75f;
                vso.FindProperty("useVignette").boolValue = true;
                vso.FindProperty("vignetteStrength").floatValue = 0.25f;
                vso.ApplyModifiedProperties();
            }

            var labelGO = new GameObject("Label");
            labelGO.transform.position = new Vector3(x, y - targetH * 0.5f - labelOffset, 0);
            var tm = labelGO.AddComponent<TextMesh>();
            tm.text = i == 0 ? "Original" : filterName;
            tm.characterSize = 0.08f;
            tm.fontSize = 80;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            labelGO.GetComponent<MeshRenderer>().sortingOrder = 10;
        }

        EditorSceneManager.MarkSceneDirty(scene);

        string folder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        string path = "Assets/Scenes/filter_preview.unity";
        EditorSceneManager.SaveScene(scene, path);

        int totalFilters = styleNames.Length + colorNames.Length - 2;
        Debug.Log(
            $"═══ 滤镜预览场景已创建 ═══\n" +
            $"路径: {path}\n" +
            $"风格化滤镜: {styleNames.Length - 1} 种\n" +
            $"颜色滤镜: {colorNames.Length - 1} 种\n" +
            $"共 {totalFilters} 张对比卡片 + 2 张原图\n\n" +
            "点 Play 即可实时预览所有滤镜效果！\n" +
            "在 Scene 视图中滚动浏览全部卡片。");
    }

    // ────── Level: CursorSettings（保留） ──────

    [MenuItem("Tools/关卡/创建 CursorSettings 配置")]
    public static void CreateCursorSettings()
    {
        const string path = "Assets/Resources/CursorSettings.asset";
        var existing = AssetDatabase.LoadAssetAtPath<CursorSettings>(path);
        if (existing != null)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = existing;
            Debug.Log("[EditorTools] CursorSettings 已存在，已选中");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var asset = ScriptableObject.CreateInstance<CursorSettings>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        Debug.Log("[EditorTools] 已创建 CursorSettings: " + path);
    }

    [MenuItem("Tools/Gallery/设置 Build Settings（Gallery 为启动场景）")]
    public static void SetupBuildSettings()
    {
        string galleryPath = "Assets/Scenes/gallery.unity";
        if (!System.IO.File.Exists(galleryPath))
        {
            Debug.LogError("找不到 gallery 场景: " + galleryPath);
            return;
        }

        var scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(galleryPath, true));

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path == galleryPath) continue;
            scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[EditorTools] Build Settings 已更新，gallery 设为启动场景 (index 0)");

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(galleryPath);
        EditorSceneManager.OpenScene(galleryPath);
        EnsureRuntimeBootstrap();
    }

    private static void EnsureRuntimeBootstrap()
    {
        var existing = Object.FindObjectOfType<RuntimeGalleryBootstrap>();
        if (existing != null)
        {
            Debug.Log("[EditorTools] RuntimeGalleryBootstrap 已存在");
            return;
        }

        var go = new GameObject("RuntimeGalleryBootstrap");
        go.AddComponent<RuntimeGalleryBootstrap>();
        Debug.Log("[EditorTools] 已添加 RuntimeGalleryBootstrap 到场景");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Gallery/创建 Gallery 编辑器场景")]
    public static void CreateGalleryEditorScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 10;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        camGO.transform.position = new Vector3(0, 0, -10);
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        string playerPrefabPath = "Assets/Prefabs/Gallery/GalleryPlayer.prefab";
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (playerPrefab != null)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.zero;
        }
        else
        {
            var playerGO = new GameObject("GalleryPlayer");
            playerGO.transform.position = Vector3.zero;
            var sr = playerGO.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprite.Get();
            sr.color = Color.cyan;
            sr.sortingOrder = 10;
            playerGO.transform.localScale = Vector3.one * 0.6f;
            var rb = playerGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            playerGO.AddComponent<BoxCollider2D>();
            playerGO.AddComponent<GalleryPlayer>();
        }

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var bootstrapGO = new GameObject("RuntimeGalleryBootstrap");
        var bootstrap = bootstrapGO.AddComponent<RuntimeGalleryBootstrap>();
        var so = new SerializedObject(bootstrap);
        so.FindProperty("enableEditor").boolValue = true;
        so.FindProperty("autoLoadScene").stringValue = "";
        so.ApplyModifiedPropertiesWithoutUndo();

        string savePath = "Assets/Scenes/gallery_editor.unity";
        if (!System.IO.Directory.Exists("Assets/Scenes"))
            System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, savePath);
        Debug.Log("[EditorTools] Gallery 编辑器场景已创建: " + savePath +
            "\n双击打开场景，点 Play，按 Tab 即可使用运行时编辑器");
    }
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(RuntimeGalleryBootstrap))]
public class GalleryScenePreview : Editor
{
    private string selectedScene;
    private string[] sceneNames;
    private int selectedIndex;
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool previewActive;
    private GallerySpawnPoint spawnPoint;

    private void OnEnable()
    {
        RefreshSceneList();
        var bootstrap = (RuntimeGalleryBootstrap)target;
        var so = new SerializedObject(bootstrap);
        var prop = so.FindProperty("autoLoadScene");
        if (prop != null && !string.IsNullOrEmpty(prop.stringValue))
        {
            selectedScene = prop.stringValue;
            selectedIndex = System.Array.IndexOf(sceneNames, selectedScene);
            if (selectedIndex < 0) selectedIndex = 0;
        }
    }

    private void OnDisable()
    {
        ClearPreview();
    }

    private void RefreshSceneList()
    {
        string root = Path.Combine(Application.persistentDataPath, "Gallery", "scenes");
        if (!Directory.Exists(root))
        {
            sceneNames = new string[0];
            return;
        }
        var dirs = Directory.GetDirectories(root);
        var names = new List<string>();
        foreach (var d in dirs)
        {
            if (File.Exists(Path.Combine(d, "scene.json")))
                names.Add(Path.GetFileName(d));
        }
        sceneNames = names.ToArray();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("场景预览", EditorStyles.boldLabel);

        if (sceneNames == null || sceneNames.Length == 0)
        {
            EditorGUILayout.HelpBox("未找到任何场景数据。请先在运行时创建场景。", MessageType.Info);
            if (GUILayout.Button("刷新场景列表"))
                RefreshSceneList();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        int newIndex = EditorGUILayout.Popup("预览场景", selectedIndex, sceneNames);
        if (newIndex != selectedIndex)
        {
            selectedIndex = newIndex;
            selectedScene = sceneNames[selectedIndex];
            if (previewActive) ShowPreview();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (!previewActive)
        {
            if (GUILayout.Button("显示预览"))
                ShowPreview();
        }
        else
        {
            if (GUILayout.Button("刷新预览"))
                ShowPreview();
            if (GUILayout.Button("隐藏预览"))
                ClearPreview();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("刷新场景列表"))
            RefreshSceneList();

        if (previewActive)
        {
            EditorGUILayout.HelpBox(
                "预览中: " + selectedScene + " (" + previewObjects.Count + " 个元素)\n" +
                "预览对象在 Play 时自动清除。",
                MessageType.None);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("玩家出生点", EditorStyles.boldLabel);

        if (spawnPoint == null)
            spawnPoint = FindObjectOfType<GallerySpawnPoint>();

        if (spawnPoint == null)
        {
            if (GUILayout.Button("创建出生点标记"))
            {
                var go = new GameObject("PlayerSpawnPoint");
                spawnPoint = go.AddComponent<GallerySpawnPoint>();
                if (sceneNames != null && selectedIndex >= 0 && selectedIndex < sceneNames.Length)
                    spawnPoint.sceneName = sceneNames[selectedIndex];
                Selection.activeGameObject = go;
                Undo.RegisterCreatedObjectUndo(go, "Create Spawn Point");
            }
            EditorGUILayout.HelpBox("在场景中添加出生点标记，可以直接拖拽设置 Player 出生位置。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.ObjectField("出生点对象", spawnPoint, typeof(GallerySpawnPoint), true);
            Vector3 pos = spawnPoint.transform.position;
            EditorGUILayout.LabelField("位置", "(" + pos.x.ToString("F2") + ", " + pos.y.ToString("F2") + ")");

            string targetScene = spawnPoint.sceneName;
            if (string.IsNullOrEmpty(targetScene) && sceneNames != null && selectedIndex >= 0 && selectedIndex < sceneNames.Length)
                targetScene = sceneNames[selectedIndex];

            if (!string.IsNullOrEmpty(targetScene))
            {
                if (GUILayout.Button("保存出生点到场景: " + targetScene))
                    SaveSpawnPosition(targetScene, pos);
                if (GUILayout.Button("从场景数据加载出生点"))
                    LoadSpawnPosition(targetScene);
            }
            else
            {
                EditorGUILayout.HelpBox("请先选择一个场景或在 SpawnPoint 上指定 sceneName。", MessageType.Warning);
            }
        }
    }

    private void ShowPreview()
    {
        ClearPreview();
        if (selectedIndex < 0 || selectedIndex >= sceneNames.Length) return;
        selectedScene = sceneNames[selectedIndex];

        string jsonPath = Path.Combine(Application.persistentDataPath, "Gallery", "scenes", selectedScene, "scene.json");
        if (!File.Exists(jsonPath))
        {
            Debug.LogWarning("[Preview] scene.json not found: " + jsonPath);
            return;
        }

        string json = File.ReadAllText(jsonPath);
        SceneData data = SceneData.FromJson(json);
        if (data == null) return;

        string mediaRoot = Path.Combine(Application.persistentDataPath, "Gallery", "scenes", selectedScene);

        var parent = new GameObject("[ScenePreview] " + selectedScene);
        parent.tag = "EditorOnly";
        parent.hideFlags = HideFlags.DontSave;
        previewObjects.Add(parent);

        if (data.settings != null)
        {
            ApplyPlayerPosition(data.settings);
        }

        foreach (var elem in data.elements)
        {
            if (!elem.enabled) continue;
            var go = CreatePreviewElement(elem, mediaRoot);
            if (go != null)
            {
                go.transform.SetParent(parent.transform);
                go.hideFlags = HideFlags.DontSave;
                go.tag = "EditorOnly";
            }
        }

        previewActive = true;
        SceneView.RepaintAll();
    }

    private void ApplyPlayerPosition(SceneSettingsData s)
    {
        var bootstrap = (RuntimeGalleryBootstrap)target;
        var player = bootstrap.GetComponentInChildren<GalleryPlayer>();
        if (player == null)
            player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
        {
            player.transform.position = new Vector3(s.playerStartX, s.playerStartY, 0);
            EditorUtility.SetDirty(player);
        }
    }

    private GameObject CreatePreviewElement(ElementData elem, string mediaRoot)
    {
        string label = elem.type + "_" + elem.id;
        var go = new GameObject(label);
        go.transform.position = new Vector3(elem.x, elem.y, 0);
        go.transform.localScale = new Vector3(elem.scaleX, elem.scaleY, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, elem.rotation);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = elem.sortingOrder;

        Sprite sprite = null;
        if (!string.IsNullOrEmpty(elem.mediaFile))
            sprite = LoadSpriteFromDisk(Path.Combine(mediaRoot, elem.mediaFile));

        if (sprite != null)
        {
            sr.sprite = sprite;
            sr.color = Color.white;
            FitPreviewScale(go, sprite, elem);
        }
        else
        {
            sr.color = GetTypeColor(elem.type);
            float w = Mathf.Max(Mathf.Abs(elem.scaleX), 1f);
            float h = Mathf.Max(Mathf.Abs(elem.scaleY), 1f);
            go.transform.localScale = new Vector3(w, h, 1f);
        }

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        labelGO.transform.localPosition = Vector3.zero;
        Vector3 ps = go.transform.localScale;
        labelGO.transform.localScale = new Vector3(
            ps.x != 0 ? 1f / ps.x : 1f,
            ps.y != 0 ? 1f / ps.y : 1f, 1f);
        var tm = labelGO.AddComponent<TextMesh>();
        tm.text = !string.IsNullOrEmpty(elem.caption) ? elem.caption : elem.id;
        tm.characterSize = 0.05f;
        tm.fontSize = 60;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 1f, 1f, 0.6f);
        labelGO.GetComponent<MeshRenderer>().sortingOrder = elem.sortingOrder + 1;
        labelGO.hideFlags = HideFlags.DontSave;
        labelGO.tag = "EditorOnly";

        return go;
    }

    private void FitPreviewScale(GameObject go, Sprite sprite, ElementData elem)
    {
        float spriteW = sprite.bounds.size.x;
        float spriteH = sprite.bounds.size.y;
        if (spriteW <= 0 || spriteH <= 0) return;

        float frameW = Mathf.Abs(elem.scaleX);
        float frameH = Mathf.Abs(elem.scaleY);
        if (frameW <= 0) frameW = 1f;
        if (frameH <= 0) frameH = 1f;

        float oldVisualArea = frameW * frameH;
        float uniformScale = Mathf.Sqrt(oldVisualArea / (spriteW * spriteH));
        go.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
    }

    private Color GetTypeColor(string type)
    {
        switch (type)
        {
            case "photo": return new Color(0.3f, 0.5f, 0.8f, 0.5f);
            case "video": return new Color(0.8f, 0.3f, 0.3f, 0.5f);
            case "npc_dialogue": return new Color(0.3f, 0.8f, 0.4f, 0.5f);
            case "npc_follower": return new Color(0.8f, 0.7f, 0.2f, 0.5f);
            case "weather": return new Color(0.6f, 0.6f, 0.8f, 0.3f);
            default: return new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    private Sprite LoadSpriteFromDisk(string fullPath)
    {
        if (!File.Exists(fullPath)) return null;
        try
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2);
            tex.hideFlags = HideFlags.DontSave;
            if (!tex.LoadImage(bytes)) return null;
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                Vector2.one * 0.5f, 100f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
        catch
        {
            return null;
        }
    }

    private void ClearPreview()
    {
        for (int i = previewObjects.Count - 1; i >= 0; i--)
        {
            if (previewObjects[i] != null)
                DestroyImmediate(previewObjects[i]);
        }
        previewObjects.Clear();
        previewActive = false;
    }

    private void SaveSpawnPosition(string scene, Vector3 pos)
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "Gallery", "scenes", scene, "scene.json");
        if (!File.Exists(jsonPath))
        {
            Debug.LogWarning("[Preview] scene.json not found: " + jsonPath);
            return;
        }
        string json = File.ReadAllText(jsonPath);
        SceneData data = SceneData.FromJson(json);
        if (data == null) return;

        if (data.settings == null) data.settings = new SceneSettingsData();
        data.settings.playerStartX = pos.x;
        data.settings.playerStartY = pos.y;
        File.WriteAllText(jsonPath, data.ToJson());
        Debug.Log("[Preview] Spawn position saved: (" + pos.x.ToString("F2") + ", " + pos.y.ToString("F2") + ") -> " + scene);
    }

    private void LoadSpawnPosition(string scene)
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "Gallery", "scenes", scene, "scene.json");
        if (!File.Exists(jsonPath))
        {
            Debug.LogWarning("[Preview] scene.json not found: " + jsonPath);
            return;
        }
        string json = File.ReadAllText(jsonPath);
        SceneData data = SceneData.FromJson(json);
        if (data == null || data.settings == null) return;

        if (spawnPoint != null)
        {
            Undo.RecordObject(spawnPoint.transform, "Load Spawn Position");
            spawnPoint.transform.position = new Vector3(data.settings.playerStartX, data.settings.playerStartY, 0);
        }

        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Load Player Position");
            player.transform.position = new Vector3(data.settings.playerStartX, data.settings.playerStartY, 0);
        }
    }
}

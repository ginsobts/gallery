using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GallerySpawnPoint))]
public class GallerySpawnPointEditor : Editor
{
    private void OnSceneGUI()
    {
        var sp = (GallerySpawnPoint)target;
        EditorGUI.BeginChangeCheck();
        Vector3 newPos = Handles.PositionHandle(sp.transform.position, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(sp.transform, "Move Spawn Point");
            sp.transform.position = new Vector3(newPos.x, newPos.y, 0);
        }

        Handles.color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
        Handles.DrawWireDisc(sp.transform.position, Vector3.forward, 0.5f);

        var style = new GUIStyle();
        style.normal.textColor = new Color(0.2f, 0.9f, 0.4f);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        Handles.Label(sp.transform.position + Vector3.up * 1.0f, "Player Spawn", style);

        var subStyle = new GUIStyle();
        subStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f);
        subStyle.fontSize = 10;
        subStyle.alignment = TextAnchor.MiddleCenter;
        string posLabel = "(" + sp.transform.position.x.ToString("F1") + ", " + sp.transform.position.y.ToString("F1") + ")";
        Handles.Label(sp.transform.position + Vector3.down * 0.7f, posLabel, subStyle);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var sp = (GallerySpawnPoint)target;
        EditorGUILayout.Space(5);
        Vector2 pos = new Vector2(sp.transform.position.x, sp.transform.position.y);
        Vector2 newPos = EditorGUILayout.Vector2Field("出生位置", pos);
        if (newPos != pos)
        {
            Undo.RecordObject(sp.transform, "Set Spawn Position");
            sp.transform.position = new Vector3(newPos.x, newPos.y, 0);
        }

        EditorGUILayout.Space(5);
        string targetScene = sp.sceneName;

        if (!string.IsNullOrEmpty(targetScene))
        {
            if (GUILayout.Button("保存到场景: " + targetScene))
            {
                string jsonPath = System.IO.Path.Combine(Application.persistentDataPath, "Gallery", "scenes", targetScene, "scene.json");
                if (System.IO.File.Exists(jsonPath))
                {
                    string json = System.IO.File.ReadAllText(jsonPath);
                    SceneData data = SceneData.FromJson(json);
                    if (data != null)
                    {
                        if (data.settings == null) data.settings = new SceneSettingsData();
                        data.settings.playerStartX = sp.transform.position.x;
                        data.settings.playerStartY = sp.transform.position.y;
                        System.IO.File.WriteAllText(jsonPath, data.ToJson());
                        Debug.Log("[SpawnPoint] Saved spawn (" + sp.transform.position.x.ToString("F2") + ", " + sp.transform.position.y.ToString("F2") + ") to " + targetScene);
                    }
                }
                else
                {
                    Debug.LogWarning("[SpawnPoint] scene.json not found: " + jsonPath);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("请指定 sceneName 以启用保存功能。", MessageType.Info);
        }
    }
}

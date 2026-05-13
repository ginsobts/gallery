using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GalleryPath))]
public class GalleryPathEditor : Editor
{
    private GalleryPath path;

    private void OnEnable()
    {
        path = (GalleryPath)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加路径点"))
        {
            Undo.RecordObject(path, "Add Path Point");
            Vector2 last = path.points.Count > 0 ? path.points[path.points.Count - 1] : Vector2.zero;
            path.points.Add(last + Vector2.right * 2f);
            path.Rebuild();
            EditorUtility.SetDirty(path);
        }
        if (GUILayout.Button("删除最后一个点") && path.points.Count > 2)
        {
            Undo.RecordObject(path, "Remove Path Point");
            path.points.RemoveAt(path.points.Count - 1);
            path.Rebuild();
            EditorUtility.SetDirty(path);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("重建路径"))
            path.Rebuild();
    }

    private void OnSceneGUI()
    {
        if (path.points == null) return;

        for (int i = 0; i < path.points.Count; i++)
        {
            Vector3 wp = path.transform.TransformPoint(path.points[i]);
            float handleSize = HandleUtility.GetHandleSize(wp) * 0.08f;

            EditorGUI.BeginChangeCheck();
            Vector3 newWp = Handles.FreeMoveHandle(wp, handleSize, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Path Point");
                path.points[i] = path.transform.InverseTransformPoint(newWp);
                path.Rebuild();
                EditorUtility.SetDirty(path);
            }

            Handles.Label(wp + Vector3.up * 0.25f, i.ToString(), EditorStyles.boldLabel);
        }
    }
}

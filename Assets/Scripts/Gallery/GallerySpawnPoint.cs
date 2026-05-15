using UnityEngine;

public class GallerySpawnPoint : MonoBehaviour
{
    [Tooltip("关联的场景名（留空则使用 Bootstrap 的 autoLoadScene）")]
    public string sceneName = "";

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.25f);
        Gizmos.DrawSphere(transform.position, 0.5f);

#if UNITY_EDITOR
        var style = new GUIStyle();
        style.normal.textColor = new Color(0.2f, 0.9f, 0.4f);
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.8f,
            "Player Spawn", style);
#endif
    }
}

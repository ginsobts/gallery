using UnityEngine;
using UnityEngine.EventSystems;

public class RuntimeGalleryBootstrap : MonoBehaviour
{
    [Header("启动设置")]
    [Tooltip("启动时自动加载的场景名（留空则显示场景列表）")]
    [SerializeField] private string autoLoadScene = "";
    [Tooltip("是否启用运行时编辑器（Tab键切换）")]
    [SerializeField] private bool enableEditor = true;

    private void Awake()
    {
        EnsureEventSystem();
    }

    private void Start()
    {
        var builderGO = new GameObject("RuntimeSceneBuilder");
        builderGO.AddComponent<RuntimeSceneBuilder>();

        if (enableEditor)
        {
            var editorGO = new GameObject("RuntimeEditor");
            editorGO.AddComponent<RuntimeEditor>();
            editorGO.AddComponent<RuntimeMediaBrowser>();
            editorGO.AddComponent<RuntimeSceneBrowser>();
        }

        string sceneToLoad = autoLoadScene;
        if (string.IsNullOrEmpty(sceneToLoad))
            sceneToLoad = PlayerPrefs.GetString("Gallery_LastScene", "");

        if (!string.IsNullOrEmpty(sceneToLoad) &&
            System.IO.File.Exists(SceneDataHelper.GetSceneJsonPath(sceneToLoad)))
        {
            RuntimeSceneBuilder.Instance.LoadScene(sceneToLoad);
        }
        else
        {
            string[] scenes = SceneDataHelper.ListScenes();
            if (scenes.Length > 0)
            {
                RuntimeSceneBuilder.Instance.LoadScene(scenes[0]);
            }
            else if (enableEditor && RuntimeSceneBrowser.Instance != null)
            {
                RuntimeSceneBrowser.Instance.Open();
            }
        }
    }

    private void EnsureEventSystem()
    {
        var existing = FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            if (existing.GetComponent<StandaloneInputModule>() == null
                && existing.GetComponent<BaseInputModule>() == null)
            {
                existing.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[Bootstrap] Added StandaloneInputModule to existing EventSystem");
            }
            Debug.Log("[Bootstrap] EventSystem already exists: " + existing.gameObject.name);
            return;
        }

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        Debug.Log("[Bootstrap] Created new EventSystem");
    }
}

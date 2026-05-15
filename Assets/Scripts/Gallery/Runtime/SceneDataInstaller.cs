using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class SceneDataInstaller : MonoBehaviour
{
    private const string VERSION_KEY = "Gallery_DataVersion";

    public static bool NeedsInstall()
    {
        string versionPath = Path.Combine(Application.streamingAssetsPath, "Gallery", "version.txt");
        if (!File.Exists(versionPath)) return false;

        string bundledVersion = File.ReadAllText(versionPath).Trim();
        string installedVersion = PlayerPrefs.GetString(VERSION_KEY, "");
        return bundledVersion != installedVersion;
    }

    public static IEnumerator InstallDefaultScenes(System.Action onComplete = null)
    {
        string targetRoot = SceneDataHelper.GetScenesRootPath();

        string manifestPath = Path.Combine(Application.streamingAssetsPath, "Gallery", "manifest.txt");
        string versionPath = Path.Combine(Application.streamingAssetsPath, "Gallery", "version.txt");
        string manifestContent = null;
        string versionContent = null;

        yield return ReadStreamingFile(manifestPath, result => manifestContent = result);
        yield return ReadStreamingFile(versionPath, result => versionContent = result);

        if (string.IsNullOrEmpty(manifestContent))
        {
            Debug.LogWarning("[SceneDataInstaller] No manifest.txt found in StreamingAssets/Gallery/");
            onComplete?.Invoke();
            yield break;
        }

        string[] files = manifestContent.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        int copied = 0;

        foreach (string relPath in files)
        {
            string trimmed = relPath.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            string srcFile = Path.Combine(Application.streamingAssetsPath, "Gallery", "scenes", trimmed);
            string dstFile = Path.Combine(targetRoot, trimmed);

            string dstDir = Path.GetDirectoryName(dstFile);
            if (!Directory.Exists(dstDir)) Directory.CreateDirectory(dstDir);

            bool isBinary = !trimmed.EndsWith(".json") && !trimmed.EndsWith(".txt");

            if (isBinary)
            {
                byte[] bytes = null;
                yield return ReadStreamingFileBytes(srcFile, result => bytes = result);
                if (bytes != null)
                {
                    File.WriteAllBytes(dstFile, bytes);
                    copied++;
                }
            }
            else
            {
                string fileContent = null;
                yield return ReadStreamingFile(srcFile, result => fileContent = result);
                if (fileContent != null)
                {
                    File.WriteAllText(dstFile, fileContent);
                    copied++;
                }
            }
        }

        Debug.Log($"[SceneDataInstaller] Installed {copied} files from StreamingAssets");

        if (!string.IsNullOrEmpty(versionContent))
        {
            PlayerPrefs.SetString(VERSION_KEY, versionContent.Trim());
            PlayerPrefs.Save();
        }

        onComplete?.Invoke();
    }

    private static IEnumerator ReadStreamingFile(string path, System.Action<string> callback)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var req = UnityWebRequest.Get(path))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                callback(req.downloadHandler.text);
            else
                callback(null);
        }
#else
        if (File.Exists(path))
            callback(File.ReadAllText(path));
        else
            callback(null);
        yield break;
#endif
    }

    private static IEnumerator ReadStreamingFileBytes(string path, System.Action<byte[]> callback)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var req = UnityWebRequest.Get(path))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
                callback(req.downloadHandler.data);
            else
                callback(null);
        }
#else
        if (File.Exists(path))
            callback(File.ReadAllBytes(path));
        else
            callback(null);
        yield break;
#endif
    }
}

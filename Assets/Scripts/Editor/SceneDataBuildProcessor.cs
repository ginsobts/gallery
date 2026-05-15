using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SceneDataBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        ExportSceneData();
    }

    [MenuItem("Gallery/导出场景数据到 StreamingAssets")]
    public static void ExportSceneData()
    {
        string sourceRoot = Path.Combine(Application.persistentDataPath, "Gallery", "scenes");
        string galleryRoot = Path.Combine(Application.streamingAssetsPath, "Gallery");
        string targetRoot = Path.Combine(galleryRoot, "scenes");
        string manifestPath = Path.Combine(galleryRoot, "manifest.txt");
        string versionPath = Path.Combine(galleryRoot, "version.txt");

        if (!Directory.Exists(sourceRoot))
        {
            Debug.LogWarning("[SceneDataBuild] No scene data found in persistentDataPath. Nothing to export.");
            return;
        }

        if (!Directory.Exists(galleryRoot))
            Directory.CreateDirectory(galleryRoot);

        if (Directory.Exists(targetRoot))
            Directory.Delete(targetRoot, true);
        Directory.CreateDirectory(targetRoot);

        var manifestLines = new List<string>();
        string[] sceneDirs = Directory.GetDirectories(sourceRoot);

        int fileCount = 0;
        foreach (string sceneDir in sceneDirs)
        {
            string sceneName = Path.GetFileName(sceneDir);
            string sceneJsonPath = Path.Combine(sceneDir, "scene.json");
            if (!File.Exists(sceneJsonPath)) continue;

            CopyDirectoryRecursive(sceneDir, Path.Combine(targetRoot, sceneName), sceneName, manifestLines);
            fileCount++;
        }

        File.WriteAllText(manifestPath, string.Join("\n", manifestLines));

        string version = System.DateTime.Now.ToString("yyyyMMddHHmmss");
        File.WriteAllText(versionPath, version);

        AssetDatabase.Refresh();
        Debug.Log($"[SceneDataBuild] Exported {fileCount} scene(s), {manifestLines.Count} files (version: {version})");
    }

    private static void CopyDirectoryRecursive(string srcDir, string dstDir, string relativePath, List<string> manifest)
    {
        if (!Directory.Exists(dstDir))
            Directory.CreateDirectory(dstDir);

        foreach (string file in Directory.GetFiles(srcDir))
        {
            string fileName = Path.GetFileName(file);
            string dstFile = Path.Combine(dstDir, fileName);
            File.Copy(file, dstFile, true);

            string relFile = relativePath + "/" + fileName;
            manifest.Add(relFile);
        }

        foreach (string subDir in Directory.GetDirectories(srcDir))
        {
            string dirName = Path.GetFileName(subDir);
            string subRelPath = relativePath + "/" + dirName;
            CopyDirectoryRecursive(subDir, Path.Combine(dstDir, dirName), subRelPath, manifest);
        }
    }

    [MenuItem("Gallery/清除 StreamingAssets 场景数据")]
    public static void ClearStreamingAssets()
    {
        string targetRoot = Path.Combine(Application.streamingAssetsPath, "Gallery");
        if (Directory.Exists(targetRoot))
        {
            Directory.Delete(targetRoot, true);
            AssetDatabase.Refresh();
            Debug.Log("[SceneDataBuild] Cleared StreamingAssets/Gallery/");
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class RuntimeAssetLoader : MonoBehaviour
{
    private static RuntimeAssetLoader _instance;
    public static RuntimeAssetLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("RuntimeAssetLoader");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<RuntimeAssetLoader>();
            }
            return _instance;
        }
    }

    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();

    public Sprite LoadSprite(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return null;
        if (!File.Exists(absolutePath)) { Debug.LogWarning($"File not found: {absolutePath}"); return null; }

        if (spriteCache.TryGetValue(absolutePath, out var cached)) return cached;

        Texture2D tex = LoadTexture(absolutePath);
        if (tex == null) return null;

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            Vector2.one * 0.5f,
            100f);
        sprite.name = Path.GetFileNameWithoutExtension(absolutePath);
        spriteCache[absolutePath] = sprite;
        return sprite;
    }

    public Texture2D LoadTexture(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return null;
        if (!File.Exists(absolutePath)) return null;

        if (textureCache.TryGetValue(absolutePath, out var cached)) return cached;

        byte[] bytes = File.ReadAllBytes(absolutePath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        if (!tex.LoadImage(bytes, false))
        {
            Destroy(tex);
            return null;
        }
        tex.name = Path.GetFileNameWithoutExtension(absolutePath);
        textureCache[absolutePath] = tex;
        return tex;
    }

    public Sprite LoadSpriteFromScene(string sceneName, string mediaFile)
    {
        if (string.IsNullOrEmpty(mediaFile)) return null;
        string fullPath = Path.Combine(SceneDataHelper.GetScenePath(sceneName), mediaFile);
        return LoadSprite(fullPath);
    }

    public Sprite[] LoadSpriteArray(string sceneName, string[] mediaFiles)
    {
        if (mediaFiles == null || mediaFiles.Length == 0) return new Sprite[0];
        Sprite[] sprites = new Sprite[mediaFiles.Length];
        for (int i = 0; i < mediaFiles.Length; i++)
            sprites[i] = LoadSpriteFromScene(sceneName, mediaFiles[i]);
        return sprites;
    }

    public string GetVideoUrl(string sceneName, string mediaFile)
    {
        if (string.IsNullOrEmpty(mediaFile)) return "";
        string fullPath = Path.Combine(SceneDataHelper.GetScenePath(sceneName), mediaFile);
        if (!File.Exists(fullPath)) return "";
        return "file:///" + fullPath.Replace('\\', '/');
    }

    public void LoadAudioAsync(string absolutePath, Action<AudioClip> callback)
    {
        if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
        {
            callback?.Invoke(null);
            return;
        }

        if (audioCache.TryGetValue(absolutePath, out var cached))
        {
            callback?.Invoke(cached);
            return;
        }

        StartCoroutine(LoadAudioCoroutine(absolutePath, callback));
    }

    public void LoadAudioFromScene(string sceneName, string mediaFile, Action<AudioClip> callback)
    {
        if (string.IsNullOrEmpty(mediaFile)) { callback?.Invoke(null); return; }
        string fullPath = Path.Combine(SceneDataHelper.GetScenePath(sceneName), mediaFile);
        LoadAudioAsync(fullPath, callback);
    }

    private IEnumerator LoadAudioCoroutine(string absolutePath, Action<AudioClip> callback)
    {
        string url = "file:///" + absolutePath.Replace('\\', '/');
        AudioType audioType = GetAudioType(absolutePath);

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                clip.name = Path.GetFileNameWithoutExtension(absolutePath);
                audioCache[absolutePath] = clip;
                callback?.Invoke(clip);
            }
            else
            {
                Debug.LogWarning($"Failed to load audio: {absolutePath} - {req.error}");
                callback?.Invoke(null);
            }
        }
    }

    private AudioType GetAudioType(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        switch (ext)
        {
            case ".ogg": return AudioType.OGGVORBIS;
            case ".wav": return AudioType.WAV;
            case ".mp3": return AudioType.MPEG;
            default: return AudioType.UNKNOWN;
        }
    }

    public string CopyMediaToScene(string sourcePath, string sceneName)
    {
        if (!File.Exists(sourcePath)) return "";
        string mediaDir = SceneDataHelper.GetMediaPath(sceneName);
        string fileName = Path.GetFileName(sourcePath);
        string destPath = Path.Combine(mediaDir, fileName);

        int counter = 1;
        while (File.Exists(destPath))
        {
            string nameWithout = Path.GetFileNameWithoutExtension(sourcePath);
            string ext = Path.GetExtension(sourcePath);
            destPath = Path.Combine(mediaDir, $"{nameWithout}_{counter}{ext}");
            counter++;
        }

        File.Copy(sourcePath, destPath);
        return "media/" + Path.GetFileName(destPath);
    }

    public void InvalidateCache(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return;
        if (textureCache.TryGetValue(absolutePath, out var tex))
        {
            if (tex != null) Destroy(tex);
            textureCache.Remove(absolutePath);
        }
        if (spriteCache.TryGetValue(absolutePath, out var spr))
        {
            if (spr != null) Destroy(spr);
            spriteCache.Remove(absolutePath);
        }
        audioCache.Remove(absolutePath);
    }

    public void ClearCache()
    {
        foreach (var kv in textureCache)
            if (kv.Value != null) Destroy(kv.Value);
        foreach (var kv in spriteCache)
            if (kv.Value != null) Destroy(kv.Value);
        textureCache.Clear();
        spriteCache.Clear();
        audioCache.Clear();
    }

    private void OnDestroy()
    {
        ClearCache();
    }
}

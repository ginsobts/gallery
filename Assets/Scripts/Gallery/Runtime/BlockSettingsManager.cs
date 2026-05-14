using UnityEngine;

public class BlockSettingsManager : MonoBehaviour
{
    private SceneSettingsData settings;
    private string sceneName;
    private GalleryCamera galleryCam;
    private int lastBlock = -1;

    private SpriteRenderer bgA, bgB;
    private bool bgUsingA = true;
    private float bgFadeT = 1f;

    private BlockSettingsData currentBlockData;
    private BlockSettingsData targetBlockData;
    private float transitionT = 1f;

    private AudioSource bgmA, bgmB;
    private bool bgmUsingA = true;
    private float bgmFadeT = 1f;
    private float bgmTargetVol;

    private GameObject weatherGO;
    private GalleryWeather weatherComp;

    private SpriteRenderer filterOverlay;
    private Material filterColorMat;
    private Material filterArtMat;

    private Camera cachedCam;
    private Transform cachedCamTransform;
    private GalleryBackground cachedGalleryBG;
    private Vector3 lastFollowCamPos;
    private float lastFollowOrtho;
    private float lastFollowAspect;

    public void Init(SceneSettingsData s, string scene)
    {
        settings = s;
        sceneName = scene;
        galleryCam = GetComponent<GalleryCamera>();
        cachedCam = Camera.main;
        if (cachedCam != null) cachedCamTransform = cachedCam.transform;
        cachedGalleryBG = FindObjectOfType<GalleryBackground>();

        SetupBackgrounds();
        SetupBGMSources();

        int startBlock = galleryCam != null ? galleryCam.CurrentBlock : 0;
        currentBlockData = settings.GetBlockSettings(startBlock);
        targetBlockData = currentBlockData;
        lastBlock = startBlock;

        ApplyImmediate(currentBlockData);
    }

    private void SetupBackgrounds()
    {
        if (bgA != null) Destroy(bgA.gameObject);
        if (bgB != null) Destroy(bgB.gameObject);

        bgA = CreateBGRenderer("BlockBG_A");
        bgB = CreateBGRenderer("BlockBG_B");
        bgB.color = new Color(1, 1, 1, 0);
    }

    private SpriteRenderer CreateBGRenderer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -999;
        return sr;
    }

    private void SetupBGMSources()
    {
        var go = new GameObject("BlockBGM");
        go.transform.SetParent(transform);
        bgmA = go.AddComponent<AudioSource>();
        bgmA.loop = true;
        bgmA.playOnAwake = false;
        bgmA.spatialBlend = 0f;
        bgmB = go.AddComponent<AudioSource>();
        bgmB.loop = true;
        bgmB.playOnAwake = false;
        bgmB.spatialBlend = 0f;
    }

    private void ApplyImmediate(BlockSettingsData bd)
    {
        ApplyLighting(bd);
        ApplyWeather(bd);
        ApplyFilter(bd);
        ApplyBGImmediate(bgUsingA ? bgA : bgB, bd);
        LoadAndPlayBGM(bgmUsingA ? bgmA : bgmB, bd);
    }

    private void ApplyLighting(BlockSettingsData bd)
    {
        RenderSettings.ambientLight = SceneDataHelper.ToColor(bd.ambientColor) * bd.ambientBrightness;
        if (cachedGalleryBG == null) cachedGalleryBG = FindObjectOfType<GalleryBackground>();
        if (cachedGalleryBG != null) cachedGalleryBG.SetDefaultColor(SceneDataHelper.ToColor(bd.bgColor));
    }

    private void ApplyWeather(BlockSettingsData bd)
    {
        if (!bd.weatherEnabled)
        {
            if (weatherGO != null) { Destroy(weatherGO); weatherGO = null; weatherComp = null; }
            return;
        }

        if (weatherGO == null)
        {
            weatherGO = new GameObject("BlockWeather");
            weatherGO.transform.SetParent(transform);
            var col = weatherGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(200f, 100f);
            weatherComp = weatherGO.AddComponent<GalleryWeather>();
        }

        weatherComp.SetWeather(
            (GalleryWeather.WeatherType)bd.weatherType,
            bd.weatherParticles,
            SceneDataHelper.ToColor(bd.weatherColor));
    }

    private Shader cachedArtisticShader;
    private Shader cachedFilterShader;
    private bool shadersResolved;

    private void EnsureShaders()
    {
        if (shadersResolved) return;
        cachedArtisticShader = Shader.Find("Hidden/GalleryArtistic");
        cachedFilterShader = Shader.Find("Hidden/GalleryFilter");
        shadersResolved = true;
    }

    private void ApplyFilter(BlockSettingsData bd)
    {
        EnsureFilterOverlay();
        EnsureShaders();

        if (bd.artisticStyle > 0)
        {
            if (cachedArtisticShader != null)
            {
                if (filterArtMat == null) filterArtMat = new Material(cachedArtisticShader);
                filterArtMat.SetInt("_Style", bd.artisticStyle);
                filterArtMat.SetFloat("_Intensity", bd.artisticIntensity);
                filterArtMat.SetVector("_TexelSize", new Vector4(0.004f, 0.004f, 0, 0));
                filterOverlay.material = filterArtMat;
                filterOverlay.enabled = true;
            }
        }
        else if (bd.colorFilter > 0)
        {
            if (cachedFilterShader != null)
            {
                if (filterColorMat == null) filterColorMat = new Material(cachedFilterShader);
                ApplyColorFilterParams(filterColorMat, bd.colorFilter, bd.colorFilterIntensity);
                filterOverlay.material = filterColorMat;
                filterOverlay.enabled = true;
            }
        }
        else
        {
            filterOverlay.enabled = false;
        }
    }

    private void EnsureFilterOverlay()
    {
        if (filterOverlay != null) return;
        var go = new GameObject("BlockFilterOverlay");
        go.transform.SetParent(transform);
        filterOverlay = go.AddComponent<SpriteRenderer>();
        filterOverlay.sprite = RuntimeSprite.Get();
        filterOverlay.sortingOrder = 9000;
        filterOverlay.enabled = false;
    }

    private void ApplyBGImmediate(SpriteRenderer sr, BlockSettingsData bd)
    {
        if (string.IsNullOrEmpty(bd.backgroundMediaFile))
        {
            sr.sprite = RuntimeSprite.Get();
            sr.color = SceneDataHelper.ToColor(bd.bgColor);
        }
        else
        {
            var sprite = RuntimeAssetLoader.Instance.LoadSpriteFromScene(sceneName, bd.backgroundMediaFile);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = RuntimeSprite.Get();
                sr.color = SceneDataHelper.ToColor(bd.bgColor);
            }
        }
        sr.transform.localScale = new Vector3(bd.backgroundScaleX, bd.backgroundScaleY, 1f);
    }

    private void LoadAndPlayBGM(AudioSource src, BlockSettingsData bd)
    {
        if (string.IsNullOrEmpty(bd.bgmFile))
        {
            src.Stop();
            return;
        }
        RuntimeAssetLoader.Instance.LoadAudioFromScene(sceneName, bd.bgmFile, clip =>
        {
            if (clip == null) return;
            src.clip = clip;
            src.volume = bd.bgmVolume;
            src.Play();
        });
    }

    private void LateUpdate()
    {
        if (settings == null) return;

        int curBlock = galleryCam != null ? galleryCam.CurrentBlock : 0;
        if (curBlock != lastBlock)
        {
            lastBlock = curBlock;
            OnBlockChanged(curBlock);
        }

        bool needsTransition = bgFadeT < 1f || bgmFadeT < 1f || transitionT < 1f;
        if (needsTransition) UpdateTransitions();

        FollowCamera();
    }

    private void OnBlockChanged(int newBlock)
    {
        currentBlockData = targetBlockData;
        targetBlockData = settings.GetBlockSettings(newBlock);
        transitionT = 0f;

        StartBGTransition(targetBlockData);
        StartBGMTransition(targetBlockData);

        if ((BlockTransitionType)settings.weatherTransition == BlockTransitionType.Cut)
            ApplyWeather(targetBlockData);

        if ((BlockTransitionType)settings.filterTransition == BlockTransitionType.Cut)
            ApplyFilter(targetBlockData);
    }

    private void StartBGTransition(BlockSettingsData bd)
    {
        var incoming = bgUsingA ? bgB : bgA;
        ApplyBGImmediate(incoming, bd);

        if ((BlockTransitionType)settings.backgroundTransition == BlockTransitionType.Cut)
        {
            var outgoing = bgUsingA ? bgA : bgB;
            incoming.color = SetAlpha(incoming.color, 1f);
            outgoing.color = SetAlpha(outgoing.color, 0f);
        }
        else
        {
            incoming.color = SetAlpha(incoming.color, 0f);
        }
        bgUsingA = !bgUsingA;
        bgFadeT = 0f;
    }

    private void StartBGMTransition(BlockSettingsData bd)
    {
        if ((BlockTransitionType)settings.bgmTransition == BlockTransitionType.Cut)
        {
            var cur = bgmUsingA ? bgmA : bgmB;
            var other = bgmUsingA ? bgmB : bgmA;
            other.Stop();
            LoadAndPlayBGM(cur, bd);
            return;
        }

        var incoming = bgmUsingA ? bgmB : bgmA;
        bgmTargetVol = bd.bgmVolume;
        incoming.volume = 0f;
        LoadAndPlayBGM(incoming, bd);
        bgmUsingA = !bgmUsingA;
        bgmFadeT = 0f;
    }

    private void UpdateTransitions()
    {
        float dt = Time.deltaTime;
        float dur = Mathf.Max(0.01f, settings.transitionDuration);
        float step = dt / dur;

        if (bgFadeT < 1f && (BlockTransitionType)settings.backgroundTransition != BlockTransitionType.Cut)
        {
            bgFadeT = Mathf.Clamp01(bgFadeT + step);
            var cur = bgUsingA ? bgA : bgB;
            var old = bgUsingA ? bgB : bgA;
            cur.color = SetAlpha(cur.color, bgFadeT);
            old.color = SetAlpha(old.color, 1f - bgFadeT);
            if (bgFadeT >= 1f)
            {
                cur.sortingOrder = -999;
                old.sortingOrder = -1000;
            }
        }

        if (bgmFadeT < 1f && (BlockTransitionType)settings.bgmTransition != BlockTransitionType.Cut)
        {
            bgmFadeT = Mathf.Clamp01(bgmFadeT + step);
            var cur = bgmUsingA ? bgmA : bgmB;
            var old = bgmUsingA ? bgmB : bgmA;
            cur.volume = Mathf.Lerp(0f, bgmTargetVol, bgmFadeT);
            old.volume = Mathf.Lerp(old.volume, 0f, bgmFadeT);
            if (bgmFadeT >= 1f) { old.Stop(); old.volume = 0f; }
        }

        if (transitionT < 1f)
        {
            transitionT = Mathf.Clamp01(transitionT + step);

            if ((BlockTransitionType)settings.lightingTransition == BlockTransitionType.Lerp)
            {
                Color fromAmb = SceneDataHelper.ToColor(currentBlockData.ambientColor) * currentBlockData.ambientBrightness;
                Color toAmb = SceneDataHelper.ToColor(targetBlockData.ambientColor) * targetBlockData.ambientBrightness;
                RenderSettings.ambientLight = Color.Lerp(fromAmb, toAmb, transitionT);
            }
            else if ((BlockTransitionType)settings.lightingTransition == BlockTransitionType.Cut && transitionT == step)
            {
                ApplyLighting(targetBlockData);
            }

            if ((BlockTransitionType)settings.weatherTransition == BlockTransitionType.Fade ||
                (BlockTransitionType)settings.weatherTransition == BlockTransitionType.Lerp)
            {
                if (transitionT >= 0.5f && transitionT - step < 0.5f)
                    ApplyWeather(targetBlockData);
            }

            if ((BlockTransitionType)settings.filterTransition == BlockTransitionType.Fade)
            {
                if (transitionT >= 0.5f && transitionT - step < 0.5f)
                    ApplyFilter(targetBlockData);
            }

            if (transitionT >= 1f)
            {
                ApplyLighting(targetBlockData);
                currentBlockData = targetBlockData;
            }
        }
    }

    private void FollowCamera()
    {
        if (cachedCam == null) { cachedCam = Camera.main; if (cachedCam == null) return; cachedCamTransform = cachedCam.transform; }
        Vector3 pos = cachedCamTransform.position;
        float ortho = cachedCam.orthographicSize;
        float aspect = cachedCam.aspect;

        bool posChanged = pos.x != lastFollowCamPos.x || pos.y != lastFollowCamPos.y;
        bool sizeChanged = ortho != lastFollowOrtho || aspect != lastFollowAspect;

        if (!posChanged && !sizeChanged) return;

        Vector3 bgPos = new Vector3(pos.x, pos.y, 10f);
        if (bgA != null) bgA.transform.position = bgPos;
        if (bgB != null) bgB.transform.position = bgPos;

        if (filterOverlay != null)
        {
            filterOverlay.transform.position = new Vector3(pos.x, pos.y, -5f);
            if (sizeChanged)
            {
                float camH = ortho * 2f;
                float camW = camH * aspect;
                filterOverlay.transform.localScale = new Vector3(camW + 1f, camH + 1f, 1f);
            }
        }
        if (weatherGO != null) weatherGO.transform.position = new Vector3(pos.x, pos.y, 0);

        lastFollowCamPos = pos;
        lastFollowOrtho = ortho;
        lastFollowAspect = aspect;
    }

    private static void ApplyColorFilterParams(Material mat, int filterIndex, float intensity)
    {
        Color tint = Color.white;
        float sat = 1f, bright = 0f, contrast = 1f;
        Color overlay = new Color(0, 0, 0, 0);
        float i = intensity;

        switch (filterIndex)
        {
            case 1: tint = new Color(1.1f, 0.9f, 0.65f); sat = 0.3f; break;
            case 2: tint = new Color(1.05f, 0.95f, 0.8f); sat = 0.6f; bright = -0.05f; overlay = new Color(0.4f, 0.2f, 0.1f, 0.15f); break;
            case 3: tint = new Color(0.85f, 0.92f, 1.1f); sat = 0.85f; break;
            case 4: tint = new Color(1.15f, 1.0f, 0.85f); sat = 0.9f; bright = 0.03f; break;
            case 5: sat = 0f; contrast = 1.3f; break;
            case 6: tint = new Color(1.05f, 0.98f, 1.1f); sat = 0.7f; bright = 0.08f; contrast = 0.85f; overlay = new Color(0.9f, 0.8f, 1f, 0.1f); break;
            case 7: tint = new Color(1.2f, 0.85f, 0.7f); sat = 1.1f; overlay = new Color(1f, 0.5f, 0.2f, 0.12f); break;
            case 8: tint = new Color(0.8f, 0.95f, 1.15f); sat = 0.9f; overlay = new Color(0.1f, 0.3f, 0.6f, 0.1f); break;
            case 9: tint = new Color(0.9f, 1.1f, 0.85f); sat = 0.85f; overlay = new Color(0.1f, 0.3f, 0.1f, 0.08f); break;
            case 10: tint = new Color(1f, 0.9f, 1.15f); sat = 1.4f; contrast = 1.2f; bright = 0.05f; break;
            case 11: tint = new Color(1.05f, 1.02f, 1.05f); sat = 0.5f; bright = 0.15f; contrast = 0.8f; break;
            case 12: sat = 1.1f; contrast = 1.5f; bright = -0.02f; break;
            case 13: sat = 0.6f; bright = 0.1f; contrast = 0.8f; overlay = new Color(0.5f, 0.5f, 0.5f, 0.1f); break;
            case 14: tint = new Color(0.9f, 0.85f, 1.2f); sat = 0.8f; overlay = new Color(0.3f, 0.2f, 0.6f, 0.12f); break;
            case 15: tint = new Color(1.2f, 1.05f, 0.8f); sat = 1.05f; bright = 0.05f; overlay = new Color(1f, 0.8f, 0.4f, 0.1f); break;
        }

        mat.SetColor("_Tint", Color.Lerp(Color.white, tint, i));
        mat.SetFloat("_Saturation", Mathf.Lerp(1f, sat, i));
        mat.SetFloat("_Brightness", bright * i);
        mat.SetFloat("_Contrast", Mathf.Lerp(1f, contrast, i));
        overlay.a *= i;
        mat.SetColor("_Overlay", overlay);
        mat.SetFloat("_Vignette", 0f);
    }

    private Color SetAlpha(Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }

    private void OnDestroy()
    {
        if (filterColorMat != null) Destroy(filterColorMat);
        if (filterArtMat != null) Destroy(filterArtMat);
        if (bgA != null) Destroy(bgA.gameObject);
        if (bgB != null) Destroy(bgB.gameObject);
        if (weatherGO != null) Destroy(weatherGO);
    }
}

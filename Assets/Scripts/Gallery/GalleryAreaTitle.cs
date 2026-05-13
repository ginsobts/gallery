using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public class GalleryAreaTitle : MonoBehaviour
{
    [Header("区域信息")]
    [Tooltip("进入此区域时显示的标题")]
    [SerializeField] private string title = "Area Name";
    [Tooltip("副标题（如日期、地点）")]
    [SerializeField] private string subtitle = "";

    [Header("显示设置")]
    [Tooltip("标题显示持续时间")]
    [SerializeField] private float displayDuration = 3f;
    [Tooltip("淡入时间")]
    [SerializeField] private float fadeInTime = 0.8f;
    [Tooltip("淡出时间")]
    [SerializeField] private float fadeOutTime = 1.2f;
    [Tooltip("标题字号")]
    [SerializeField] private int titleFontSize = 48;
    [Tooltip("副标题字号")]
    [SerializeField] private int subtitleFontSize = 24;
    [Tooltip("文字颜色")]
    [SerializeField] private Color textColor = Color.white;
    [Tooltip("显示位置（屏幕归一化 Y 坐标）")]
    [Range(0f, 1f)]
    [SerializeField] private float yPosition = 0.8f;

    private static Canvas titleCanvas;
    private static Text titleText;
    private static Text subtitleText;
    private static CanvasGroup canvasGroup;
    private static float timer;
    private static float currentFadeIn, currentDuration, currentFadeOut;
    private static bool showing;
    private static GalleryAreaTitle lastTriggered;

    private bool playerInside;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<GalleryPlayer>() == null) return;
        if (playerInside) return;
        playerInside = true;

        if (lastTriggered == this) return;
        lastTriggered = this;
        ShowTitle(title, subtitle, titleFontSize, subtitleFontSize, textColor, yPosition,
            fadeInTime, displayDuration, fadeOutTime);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<GalleryPlayer>() == null) return;
        playerInside = false;
        if (lastTriggered == this) lastTriggered = null;
    }

    private static void ShowTitle(string t, string sub, int tSize, int sSize,
        Color color, float yPos, float fadeIn, float dur, float fadeOut)
    {
        EnsureCanvas();
        titleText.text = t;
        titleText.fontSize = tSize;
        titleText.color = color;
        subtitleText.text = sub;
        subtitleText.fontSize = sSize;
        subtitleText.color = new Color(color.r, color.g, color.b, 0.7f);

        var rt = titleCanvas.GetComponent<RectTransform>();
        var textRT = titleText.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, yPos);
        textRT.anchorMax = new Vector2(1, yPos);
        textRT.anchoredPosition = Vector2.zero;

        var subRT = subtitleText.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, yPos);
        subRT.anchorMax = new Vector2(1, yPos);
        subRT.anchoredPosition = new Vector2(0, -(tSize * 0.8f));

        canvasGroup.alpha = 0f;
        titleCanvas.gameObject.SetActive(true);

        currentFadeIn = fadeIn;
        currentDuration = dur;
        currentFadeOut = fadeOut;
        timer = 0f;
        showing = true;
    }

    private void Update()
    {
        if (!showing || canvasGroup == null) return;

        timer += Time.deltaTime;
        float totalTime = currentFadeIn + currentDuration + currentFadeOut;

        if (timer < currentFadeIn)
        {
            canvasGroup.alpha = timer / currentFadeIn;
        }
        else if (timer < currentFadeIn + currentDuration)
        {
            canvasGroup.alpha = 1f;
        }
        else if (timer < totalTime)
        {
            float fadeProgress = (timer - currentFadeIn - currentDuration) / currentFadeOut;
            canvasGroup.alpha = 1f - fadeProgress;
        }
        else
        {
            canvasGroup.alpha = 0f;
            titleCanvas.gameObject.SetActive(false);
            showing = false;
        }
    }

    private static void EnsureCanvas()
    {
        if (titleCanvas != null) return;

        var go = new GameObject("AreaTitleCanvas");
        DontDestroyOnLoad(go);
        titleCanvas = go.AddComponent<Canvas>();
        titleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        titleCanvas.sortingOrder = 800;
        go.AddComponent<CanvasScaler>();
        canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(go.transform, false);
        titleText = titleGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.sizeDelta = new Vector2(800, 80);

        var subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(go.transform, false);
        subtitleText = subGO.AddComponent<Text>();
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.sizeDelta = new Vector2(600, 40);
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.1f);
        Gizmos.DrawCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, title);
#endif
    }
}

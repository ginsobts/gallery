using UnityEngine;

public class GalleryCamera : MonoBehaviour
{
    [Header("分块参数")]
    [Tooltip("一共有多少个区块（横向）")]
    [SerializeField] private int blockCount = 4;
    [Tooltip("第一个区块的中心 X 坐标")]
    [SerializeField] private float firstBlockCenterX = 0f;
    [Tooltip("区块宽度（0 = 自动使用相机宽度）")]
    [SerializeField] private float blockWidthOverride = 0f;

    [Header("过渡")]
    [Tooltip("相机切换区块时的移动速度")]
    [SerializeField] private float transitionSpeed = 4f;
    [Tooltip("相机 Y 坐标（固定）")]
    [SerializeField] private float cameraY = 0f;

    private Camera cam;
    private Transform playerTransform;
    private int currentBlock;
    private float blockWidth;
    private Vector3 targetPos;

    public float BlockWidth => blockWidth;
    public int CurrentBlock => currentBlock;
    public int BlockCount => blockCount;
    public float FirstBlockCenterX => firstBlockCenterX;

    public float GetBlockCenterX(int block)
    {
        return firstBlockCenterX + block * blockWidth;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void Start()
    {
        if (blockWidthOverride > 0)
            blockWidth = blockWidthOverride;
        else
            blockWidth = cam.orthographicSize * 2f * cam.aspect;

        var player = FindObjectOfType<GalleryPlayer>();
        if (player != null)
            playerTransform = player.transform;

        currentBlock = GetBlockForPosition(playerTransform != null ? playerTransform.position.x : 0f);
        float targetX = GetBlockCenterX(currentBlock);
        transform.position = new Vector3(targetX, cameraY, transform.position.z);
        targetPos = transform.position;
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        int newBlock = GetBlockForPosition(playerTransform.position.x);
        if (newBlock != currentBlock)
        {
            currentBlock = newBlock;
            float targetX = GetBlockCenterX(currentBlock);
            targetPos = new Vector3(targetX, cameraY, transform.position.z);
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, transitionSpeed * Time.deltaTime);
    }

    private int GetBlockForPosition(float x)
    {
        float leftEdge = firstBlockCenterX - blockWidth * 0.5f;
        int block = Mathf.FloorToInt((x - leftEdge) / blockWidth);
        return Mathf.Clamp(block, 0, blockCount - 1);
    }

    private void OnDrawGizmos()
    {
        float w = blockWidthOverride > 0 ? blockWidthOverride : 36f;
        float h = 20f;

        for (int i = 0; i < blockCount; i++)
        {
            float cx = firstBlockCenterX + i * w;
            Color c = Color.HSVToRGB((float)i / blockCount, 0.5f, 0.8f);
            c.a = 0.08f;
            Gizmos.color = c;
            Gizmos.DrawCube(new Vector3(cx, cameraY, 0), new Vector3(w, h, 0.1f));
            c.a = 0.4f;
            Gizmos.color = c;
            Gizmos.DrawWireCube(new Vector3(cx, cameraY, 0), new Vector3(w, h, 0.1f));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(new Vector3(cx, cameraY + h * 0.45f, 0), $"Block {i}");
#endif
        }
    }
}

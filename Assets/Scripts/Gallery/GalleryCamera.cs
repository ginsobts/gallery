using UnityEngine;

public class GalleryCamera : MonoBehaviour
{
    [Header("分块参数")]
    [SerializeField] private int blockCount = 4;
    [SerializeField] private float firstBlockCenterX = 0f;
    [SerializeField] private float blockWidthOverride = 0f;

    [Header("过渡")]
    [SerializeField] private float transitionSpeed = 4f;
    [SerializeField] private float cameraY = 0f;

    private Camera cam;
    private Transform playerTransform;
    private int currentBlock;
    private float blockWidth;
    private Vector3 targetPos;

    private float[] boundaries;
    private bool useBoundaries;

    public float BlockWidth => blockWidth;
    public int CurrentBlock => currentBlock;
    public int BlockCount => blockCount;
    public float FirstBlockCenterX => firstBlockCenterX;

    public float GetBlockCenterX(int block)
    {
        if (useBoundaries && boundaries != null && boundaries.Length > 0)
        {
            float left = block > 0 ? boundaries[block - 1] : GetSceneLeftEdge();
            float right = block < boundaries.Length ? boundaries[block] : GetSceneRightEdge();
            return (left + right) * 0.5f;
        }
        return firstBlockCenterX + block * blockWidth;
    }

    private float GetSceneLeftEdge()
    {
        if (boundaries != null && boundaries.Length > 0)
            return boundaries[0] - (boundaries.Length > 1 ? boundaries[1] - boundaries[0] : blockWidth);
        return firstBlockCenterX - blockWidth * 0.5f;
    }

    private float GetSceneRightEdge()
    {
        if (boundaries != null && boundaries.Length > 0)
        {
            float lastB = boundaries[boundaries.Length - 1];
            return lastB + (boundaries.Length > 1 ? lastB - boundaries[boundaries.Length - 2] : blockWidth);
        }
        return firstBlockCenterX + (blockCount - 0.5f) * blockWidth;
    }

    public void SetParams(int count, float firstX, float widthOverride, float speed, float y)
    {
        blockCount = count;
        firstBlockCenterX = firstX;
        blockWidthOverride = widthOverride;
        transitionSpeed = speed;
        cameraY = y;
        useBoundaries = false;
        boundaries = null;

        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        blockWidth = blockWidthOverride > 0 ? blockWidthOverride : cam.orthographicSize * 2f * cam.aspect;

        if (playerTransform == null)
        {
            var player = GalleryPlayer.Instance;
            if (player != null) playerTransform = player.transform;
        }

        currentBlock = GetBlockForPosition(playerTransform != null ? playerTransform.position.x : 0f);
        float targetX = GetBlockCenterX(currentBlock);
        targetPos = new Vector3(targetX, cameraY, transform.position.z);
    }

    public void SetBoundaries(float[] boundaryPositions, float speed, float y)
    {
        if (boundaryPositions == null || boundaryPositions.Length == 0)
        {
            useBoundaries = false;
            boundaries = null;
            return;
        }

        System.Array.Sort(boundaryPositions);
        boundaries = boundaryPositions;
        useBoundaries = true;
        blockCount = boundaries.Length + 1;
        transitionSpeed = speed;
        cameraY = y;

        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        blockWidth = cam.orthographicSize * 2f * cam.aspect;

        if (playerTransform == null)
        {
            var player = GalleryPlayer.Instance;
            if (player != null) playerTransform = player.transform;
        }

        currentBlock = GetBlockForPosition(playerTransform != null ? playerTransform.position.x : 0f);
        float targetX = GetBlockCenterX(currentBlock);
        targetPos = new Vector3(targetX, cameraY, transform.position.z);
        transform.position = targetPos;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void Start()
    {
        if (!useBoundaries)
        {
            if (blockWidthOverride > 0)
                blockWidth = blockWidthOverride;
            else
                blockWidth = cam.orthographicSize * 2f * cam.aspect;
        }

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
        if (useBoundaries && boundaries != null)
        {
            for (int i = 0; i < boundaries.Length; i++)
            {
                if (x < boundaries[i]) return i;
            }
            return boundaries.Length;
        }

        float leftEdge = firstBlockCenterX - blockWidth * 0.5f;
        int block = Mathf.FloorToInt((x - leftEdge) / blockWidth);
        return Mathf.Clamp(block, 0, blockCount - 1);
    }

    private void OnDrawGizmos()
    {
        float w = blockWidthOverride > 0 ? blockWidthOverride : 36f;
        float h = 20f;

        if (useBoundaries && boundaries != null && boundaries.Length > 0)
        {
            for (int i = 0; i < boundaries.Length; i++)
            {
                Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                Gizmos.DrawLine(
                    new Vector3(boundaries[i], cameraY - h * 0.5f, 0),
                    new Vector3(boundaries[i], cameraY + h * 0.5f, 0));
            }
            return;
        }

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
        }
    }
}

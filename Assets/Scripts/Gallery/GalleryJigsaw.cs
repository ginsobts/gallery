using UnityEngine;
using System.Collections.Generic;

public class GalleryJigsaw : MonoBehaviour
{
    [Header("拼图设置")]
    [Tooltip("完整图片")]
    [SerializeField] private Sprite fullImage;
    [Tooltip("网格列数")]
    [SerializeField] private int columns = 3;
    [Tooltip("网格行数")]
    [SerializeField] private int rows = 3;
    [Tooltip("每块碎片的世界大小")]
    [SerializeField] private float pieceWorldSize = 0.8f;
    [Tooltip("碎片间距")]
    [SerializeField] private float spacing = 0.05f;
    [Tooltip("完成后触发的事件对象（可选）")]
    [SerializeField] private GameObject completionTarget;
    [Tooltip("交互距离")]
    [SerializeField] private float interactRange = 2f;
    [Tooltip("交互键")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("外框")]
    [Tooltip("外框颜色")]
    [SerializeField] private Color frameColor = new Color(0.3f, 0.25f, 0.2f);
    [Tooltip("外框边距")]
    [SerializeField] private float framePadding = 0.15f;

    private struct Piece
    {
        public GameObject go;
        public SpriteRenderer sr;
        public int correctSlot;
        public int currentSlot;
    }

    private Piece[] pieces;
    private Vector2[] slotPositions;
    private int dragIndex = -1;
    private Vector3 dragOffset;
    private int dragOriginalSlot;
    private Transform playerTransform;
    private bool puzzleActive;
    private bool puzzleComplete;
    private SpriteRenderer frameSR;
    private Camera cachedCam;

    private void Start()
    {
        var player = GalleryPlayer.Instance;
        if (player != null) playerTransform = player.transform;
        cachedCam = Camera.main;

        CreateFrame();
        CreatePuzzle();
    }

    private void CreateFrame()
    {
        float totalW = columns * (pieceWorldSize + spacing) - spacing + framePadding * 2;
        float totalH = rows * (pieceWorldSize + spacing) - spacing + framePadding * 2;

        var frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(transform);
        frameGO.transform.localPosition = Vector3.zero;
        frameSR = frameGO.AddComponent<SpriteRenderer>();
        frameSR.sprite = RuntimeSprite.Get();
        frameSR.color = frameColor;
        frameSR.sortingOrder = 3;
        frameGO.transform.localScale = new Vector3(totalW, totalH, 1);
    }

    private void CreatePuzzle()
    {
        if (fullImage == null || fullImage.texture == null) return;

        var tex = fullImage.texture;
        int pw = tex.width / columns;
        int ph = tex.height / rows;
        int total = columns * rows;

        pieces = new Piece[total];
        slotPositions = new Vector2[total];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int idx = y * columns + x;
                float lx = (x - columns * 0.5f + 0.5f) * (pieceWorldSize + spacing);
                float ly = (y - rows * 0.5f + 0.5f) * (pieceWorldSize + spacing);
                slotPositions[idx] = new Vector2(lx, ly);
            }
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int idx = y * columns + x;
                Rect rect = new Rect(x * pw, y * ph, pw, ph);
                var sprite = Sprite.Create(tex, rect, Vector2.one * 0.5f, pw / pieceWorldSize);

                var go = new GameObject($"Piece_{x}_{y}");
                go.transform.SetParent(transform);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 5;

                pieces[idx] = new Piece
                {
                    go = go, sr = sr,
                    correctSlot = idx, currentSlot = idx
                };
            }
        }

        Shuffle();
    }

    private void Shuffle()
    {
        if (pieces == null) return;
        int total = pieces.Length;
        var rand = new System.Random(42);

        var slotAssign = new int[total];
        for (int i = 0; i < total; i++) slotAssign[i] = i;
        for (int i = total - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            int tmp = slotAssign[i]; slotAssign[i] = slotAssign[j]; slotAssign[j] = tmp;
        }

        for (int i = 0; i < total; i++)
        {
            pieces[i].currentSlot = slotAssign[i];
            pieces[i].go.transform.localPosition = (Vector3)slotPositions[slotAssign[i]];
        }
    }

    private void Update()
    {
        if (puzzleComplete || pieces == null) return;
        if (playerTransform == null)
        {
            if (GalleryPlayer.Instance != null)
                playerTransform = GalleryPlayer.Instance.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > interactRange)
        {
            if (puzzleActive)
            {
                CancelDrag();
                puzzleActive = false;
                GalleryPlayer.Unfreeze();
            }
            return;
        }

        if (Input.GetKeyDown(interactKey) && !puzzleActive)
        {
            puzzleActive = true;
            GalleryPlayer.Freeze();
        }

        if (!puzzleActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelDrag();
            puzzleActive = false;
            GalleryPlayer.Unfreeze();
            return;
        }

        HandleDrag();
    }

    private void HandleDrag()
    {
        Vector2 worldPos = cachedCam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            int hit = FindPieceAt(worldPos);
            if (hit >= 0)
            {
                dragIndex = hit;
                dragOriginalSlot = pieces[hit].currentSlot;
                dragOffset = pieces[hit].go.transform.position - (Vector3)worldPos;
                pieces[hit].sr.sortingOrder = 10;
                pieces[hit].sr.color = new Color(1f, 1f, 0.85f);
            }
        }

        if (dragIndex >= 0 && Input.GetMouseButton(0))
        {
            pieces[dragIndex].go.transform.position = (Vector3)worldPos + dragOffset;
        }

        if (dragIndex >= 0 && Input.GetMouseButtonUp(0))
        {
            int targetSlot = FindNearestSlot(pieces[dragIndex].go.transform.position);
            int occupant = FindPieceInSlot(targetSlot);

            if (occupant >= 0 && occupant != dragIndex)
            {
                pieces[occupant].currentSlot = dragOriginalSlot;
                pieces[occupant].go.transform.localPosition = (Vector3)slotPositions[dragOriginalSlot];
            }

            pieces[dragIndex].currentSlot = targetSlot;
            pieces[dragIndex].go.transform.localPosition = (Vector3)slotPositions[targetSlot];
            pieces[dragIndex].sr.sortingOrder = 5;
            pieces[dragIndex].sr.color = Color.white;
            dragIndex = -1;

            CheckCompletion();
        }
    }

    private void CancelDrag()
    {
        if (dragIndex < 0) return;
        pieces[dragIndex].go.transform.localPosition = (Vector3)slotPositions[pieces[dragIndex].currentSlot];
        pieces[dragIndex].sr.sortingOrder = 5;
        pieces[dragIndex].sr.color = Color.white;
        dragIndex = -1;
    }

    private int FindPieceAt(Vector2 worldPos)
    {
        float best = float.MaxValue;
        int result = -1;
        for (int i = 0; i < pieces.Length; i++)
        {
            float d = Vector2.Distance(worldPos, pieces[i].go.transform.position);
            if (d < pieceWorldSize * 0.55f && d < best)
            {
                best = d;
                result = i;
            }
        }
        return result;
    }

    private int FindNearestSlot(Vector3 worldPos)
    {
        Vector2 localPos = transform.InverseTransformPoint(worldPos);
        float best = float.MaxValue;
        int result = 0;
        for (int i = 0; i < slotPositions.Length; i++)
        {
            float d = Vector2.Distance(localPos, slotPositions[i]);
            if (d < best) { best = d; result = i; }
        }
        return result;
    }

    private int FindPieceInSlot(int slot)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            if (i != dragIndex && pieces[i].currentSlot == slot)
                return i;
        }
        return -1;
    }

    private void CheckCompletion()
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].currentSlot != pieces[i].correctSlot)
                return;
        }

        puzzleComplete = true;
        puzzleActive = false;
        GalleryPlayer.Unfreeze();

        if (completionTarget != null)
            completionTarget.SendMessage("OnPuzzleComplete", SendMessageOptions.DontRequireReceiver);
    }

    private void OnDestroy()
    {
        if (puzzleActive) GalleryPlayer.Unfreeze();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
        float totalW = columns * (pieceWorldSize + spacing);
        float totalH = rows * (pieceWorldSize + spacing);
        Gizmos.DrawWireCube(transform.position, new Vector3(totalW, totalH, 0));
    }
}

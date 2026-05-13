using UnityEngine;

[ExecuteInEditMode]
public class GalleryGround : MonoBehaviour
{
    [Header("地面大小（世界单位）")]
    [SerializeField] private float groundWidth = 40f;
    [SerializeField] private float groundHeight = 12f;

    [Header("地面贴图")]
    [Tooltip("直接绘制的地面纹理（由笔刷工具生成）")]
    public Texture2D groundTexture;

    [Header("排序")]
    [SerializeField] private int sortingOrder = -10;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Material mat;

    private void OnEnable()
    {
        EnsureMesh();
        UpdateMaterial();
    }

    private void OnValidate()
    {
        if (meshRenderer != null)
        {
            EnsureMesh();
            UpdateMaterial();
        }
    }

    private void EnsureMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshRenderer.sortingOrder = sortingOrder;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null || mesh.name != "GroundQuad")
        {
            mesh = new Mesh { name = "GroundQuad" };
            meshFilter.sharedMesh = mesh;
        }

        float hw = groundWidth * 0.5f, hh = groundHeight * 0.5f;
        mesh.vertices = new[] {
            new Vector3(-hw, -hh, 0), new Vector3(hw, -hh, 0),
            new Vector3(hw, hh, 0), new Vector3(-hw, hh, 0)
        };
        mesh.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void UpdateMaterial()
    {
        Shader shader = Shader.Find("Hidden/GroundBlend");
        if (shader == null) return;
        if (mat == null) { mat = new Material(shader); mat.name = "GroundBlend (Instance)"; }
        mat.mainTexture = groundTexture != null ? groundTexture : Texture2D.whiteTexture;
        meshRenderer.sharedMaterial = mat;
    }

    public void RefreshTexture()
    {
        if (mat != null && groundTexture != null)
            mat.mainTexture = groundTexture;
    }

    public Vector2 WorldToPixel(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        float u = (local.x / groundWidth) + 0.5f;
        float v = (local.y / groundHeight) + 0.5f;
        if (groundTexture == null) return Vector2.zero;
        return new Vector2(u * groundTexture.width, v * groundTexture.height);
    }

    public float GroundWidth => groundWidth;
    public float GroundHeight => groundHeight;

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.15f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(groundWidth, groundHeight, 0.01f));
        Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.6f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(groundWidth, groundHeight, 0.01f));
    }
}

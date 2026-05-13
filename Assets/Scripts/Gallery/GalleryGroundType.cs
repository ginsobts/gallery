using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GalleryGroundType : MonoBehaviour
{
    public enum GroundMaterial
    {
        Default,
        Sand,
        Stone,
        Grass,
        Wood,
        Water
    }

    [SerializeField] private GroundMaterial material = GroundMaterial.Default;
    [Tooltip("该材质的脚步音效（覆盖玩家默认音效）")]
    [SerializeField] private AudioClip[] stepClips;

    public GroundMaterial Material => material;
    public AudioClip[] StepClips => stepClips;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnDrawGizmos()
    {
        Color c;
        switch (material)
        {
            case GroundMaterial.Sand: c = new Color(0.9f, 0.8f, 0.4f, 0.15f); break;
            case GroundMaterial.Stone: c = new Color(0.5f, 0.5f, 0.5f, 0.15f); break;
            case GroundMaterial.Grass: c = new Color(0.3f, 0.8f, 0.3f, 0.15f); break;
            case GroundMaterial.Wood: c = new Color(0.6f, 0.4f, 0.2f, 0.15f); break;
            case GroundMaterial.Water: c = new Color(0.2f, 0.5f, 0.9f, 0.15f); break;
            default: c = new Color(0.5f, 0.5f, 0.5f, 0.1f); break;
        }
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = c;
        Gizmos.DrawCube(transform.position + (Vector3)col.offset,
            Vector3.Scale(col.size, transform.localScale));
    }
}

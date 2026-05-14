using UnityEngine;

public class WorldScaleKeeper : MonoBehaviour
{
    public Vector3 targetScale = Vector3.one;

    private Vector3 lastParentScale;

    private void Start()
    {
        ApplyScale();
    }

    private void LateUpdate()
    {
        Transform parent = transform.parent;
        if (parent == null) return;
        Vector3 ps = parent.lossyScale;
        if (ps == lastParentScale) return;
        lastParentScale = ps;
        ApplyScale();
    }

    private void ApplyScale()
    {
        Transform parent = transform.parent;
        if (parent == null) return;
        Vector3 ps = parent.lossyScale;
        if (ps.x == 0 || ps.y == 0) return;
        transform.localScale = new Vector3(
            targetScale.x / ps.x,
            targetScale.y / ps.y,
            targetScale.z / (ps.z == 0 ? 1f : ps.z));
        lastParentScale = ps;
    }
}

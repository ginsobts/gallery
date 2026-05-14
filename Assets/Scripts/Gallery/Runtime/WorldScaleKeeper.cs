using UnityEngine;

public class WorldScaleKeeper : MonoBehaviour
{
    public Vector3 targetScale = Vector3.one;

    private Vector3 lastParentScale;
    private Transform cachedParent;
    private int updateFrame;

    private void Start()
    {
        cachedParent = transform.parent;
        updateFrame = Random.Range(0, 4);
        ApplyScale();
    }

    private void LateUpdate()
    {
        if ((Time.frameCount + updateFrame) % 4 != 0) return;

        if (cachedParent == null) { cachedParent = transform.parent; if (cachedParent == null) return; }
        Vector3 ps = cachedParent.lossyScale;
        if (ps.x == lastParentScale.x && ps.y == lastParentScale.y && ps.z == lastParentScale.z) return;
        lastParentScale = ps;
        if (ps.x == 0 || ps.y == 0) return;
        transform.localScale = new Vector3(
            targetScale.x / ps.x,
            targetScale.y / ps.y,
            targetScale.z / (ps.z == 0 ? 1f : ps.z));
    }

    private void ApplyScale()
    {
        if (cachedParent == null) cachedParent = transform.parent;
        if (cachedParent == null) return;
        Vector3 ps = cachedParent.lossyScale;
        if (ps.x == 0 || ps.y == 0) return;
        transform.localScale = new Vector3(
            targetScale.x / ps.x,
            targetScale.y / ps.y,
            targetScale.z / (ps.z == 0 ? 1f : ps.z));
        lastParentScale = ps;
    }
}

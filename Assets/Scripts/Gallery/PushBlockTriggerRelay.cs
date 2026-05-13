using UnityEngine;

public class PushBlockTriggerRelay : MonoBehaviour
{
    [HideInInspector] public GalleryPushBlock owner;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null) owner.OnDoorContact(other);
    }
}

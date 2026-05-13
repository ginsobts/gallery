using UnityEngine;

[RequireComponent(typeof(GalleryPlayer))]
public class GalleryFootsteps : MonoBehaviour
{
    [Header("脚步音效")]
    [Tooltip("默认脚步声")]
    [SerializeField] private AudioClip[] defaultSteps;
    [Tooltip("脚步间隔（秒）")]
    [SerializeField] private float stepInterval = 0.35f;
    [Tooltip("音量")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.3f;
    [Tooltip("音调随机范围")]
    [SerializeField] private float pitchVariation = 0.1f;

    private AudioSource audioSource;
    private float stepTimer;
    private Rigidbody2D rb;
    private GalleryGroundType currentGround;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
    }

    private void Update()
    {
        if (rb == null) return;
        bool moving = rb.velocity.sqrMagnitude > 0.1f;

        if (moving)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                PlayStep();
            }
        }
        else
        {
            stepTimer = stepInterval * 0.8f;
        }
    }

    private void PlayStep()
    {
        AudioClip[] clips = null;

        if (currentGround != null && currentGround.StepClips != null && currentGround.StepClips.Length > 0)
            clips = currentGround.StepClips;
        else
            clips = defaultSteps;

        if (clips == null || clips.Length == 0) return;

        var clip = clips[Random.Range(0, clips.Length)];
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.volume = volume;
        audioSource.PlayOneShot(clip);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var ground = other.GetComponent<GalleryGroundType>();
        if (ground != null)
            currentGround = ground;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var ground = other.GetComponent<GalleryGroundType>();
        if (ground != null && ground == currentGround)
            currentGround = null;
    }
}

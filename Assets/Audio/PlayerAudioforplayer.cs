using UnityEngine;

public class StickCollisionAudio : MonoBehaviour
{
    public enum Part { Head, Butt }
    public Part stickPart = Part.Head;

    [Header("Audio Objects")]
    public GameObject headAudioObject; // for puck hits
    public GameObject buttAudioObject; // for skating

    [Header("Skating Settings")]
    [Tooltip("Seconds between skate scrape sounds while on ice.")]
    public float skateRepeatInterval = 0.35f;

    private int iceLayer;
    private AudioSource headSource;
    private AudioSource buttSource;

    private bool isOnIce = false;  // Tracks butt sliding
    private float nextSkateTime = 0f;

    void Awake()
    {
        iceLayer = LayerMask.NameToLayer("ice");

        // Cache audio sources
        if (headAudioObject)
        {
            headSource = headAudioObject.GetComponent<AudioSource>();
            if (headSource) headSource.spatialBlend = 1f;
        }

        if (buttAudioObject)
        {
            buttSource = buttAudioObject.GetComponent<AudioSource>();
            if (buttSource) buttSource.spatialBlend = 1f;
        }
    }

    // -------------------------------------------------------------------
    // Handle HEAD hits puck
    // -------------------------------------------------------------------
    private void TryPlayHead(GameObject other)
    {
        if (stickPart != Part.Head) return;
        if (!other.CompareTag("hockey_puck")) return;
        if (headSource == null) return;

        headSource.Play(); // one shot
    }

    // -------------------------------------------------------------------
    // Handle BUTT skating on ice
    // -------------------------------------------------------------------
    private void TryStartSkating(GameObject other)
    {
        if (stickPart != Part.Butt) return;
        if (other.layer != iceLayer) return;
        if (buttSource == null) return;

        isOnIce = true;
        nextSkateTime = Time.time; // Play immediately
    }

    private void TryStopSkating(GameObject other)
    {
        if (stickPart != Part.Butt) return;
        if (other.layer != iceLayer) return;

        isOnIce = false;
    }

    private void Update()
    {
        if (stickPart != Part.Butt) return;
        if (!isOnIce) return;
        if (buttSource == null) return;

        // Repeats every interval while on ice
        if (Time.time >= nextSkateTime)
        {
            buttSource.Play();
            nextSkateTime = Time.time + skateRepeatInterval;
        }
    }

    // -------------------------------------------------------------------
    // Collision Handling
    // -------------------------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        TryPlayHead(collision.gameObject);
        TryStartSkating(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryStartSkating(collision.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        TryStopSkating(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPlayHead(other.gameObject);
        TryStartSkating(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartSkating(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        TryStopSkating(other.gameObject);
    }
}

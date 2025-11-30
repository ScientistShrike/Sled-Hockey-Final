using UnityEngine;

// This script is automatically added by PlayerPuckHandler to the designated shot hitboxes.
// It detects collisions with the puck and tells the handler to execute a shot.
[RequireComponent(typeof(Collider))]
public class ShotHitbox : MonoBehaviour
{
    private Vector3 prevPos;

    void Start()
    {
        prevPos = transform.position;
    }

    void LateUpdate()
    {
        // store position at end of frame so collision handlers during the frame can use previous-frame position
        prevPos = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryNotifyHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryNotifyHit(other);
    }

    private void TryNotifyHit(Collider col)
    {
        if (col == null) return;

        // Check if we hit the registered puck
        if (col.attachedRigidbody != null && PlayerPuckHandler.Instance != null && PlayerPuckHandler.Instance.IsRegisteredPuck(col.attachedRigidbody))
        {
            if (PlayerPuckHandler.Instance != null)
            {
                // Use PlayerPuckHandler's centralized collision handling to preserve shot logic and cooldowns
                PlayerPuckHandler.Instance.HitPuckFromCollider(transform);
            }
        }
        else
        {
            if (col.attachedRigidbody != null)
            {
                //Debug.Log("ShotHitbox: collided with non-puck " + col.attachedRigidbody.gameObject.name, this);
            }
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StickTipCollision : MonoBehaviour
{
    // small script placed on the stick tip object (or the stick model) to detect hits against the puck
    // It computes a simple velocity estimate and notifies PlayerPuckHandler.Instance when it collides with the puck.

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

        // Only react if this is the registered puck object
        if (col.attachedRigidbody != null && PlayerPuckHandler.Instance != null && PlayerPuckHandler.Instance.IsRegisteredPuck(col.attachedRigidbody))
        {
            if (PlayerPuckHandler.Instance != null)
            {
                    // Use PlayerPuckHandler's centralized collision handling so velocity and force logic remains consistent
                    PlayerPuckHandler.Instance.HitPuckFromCollider(transform);
            }
        }
        else
        {
            // helpful debug - log unexpected collisions with other objects
            if (col.attachedRigidbody != null)
            {
                //Debug.Log("StickTipCollision: collided with non-puck " + col.attachedRigidbody.gameObject.name, this);
            }
        }
    }
}
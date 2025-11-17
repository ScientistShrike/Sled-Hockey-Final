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

        // Accept either exact name or a component check for Rigidbody
        if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject.name == "hockey_puck")
        {
            if (PlayerPuckHandler.Instance != null)
            {
                float dt = Mathf.Max(0.0001f, Time.deltaTime);
                Vector3 velocity = (transform.position - prevPos) / dt;
                PlayerPuckHandler.Instance.HitPuck(velocity);
            }
        }
    }
}

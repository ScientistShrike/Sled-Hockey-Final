using UnityEngine;

public class PuckCollisionLogger : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        string name = collision.collider.attachedRigidbody != null ? collision.collider.attachedRigidbody.gameObject.name : collision.collider.gameObject.name;
        
        TryHandleStickContact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        string name = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject.name : other.gameObject.name;
        
        TryHandleStickContact(other);
    }

    private void TryHandleStickContact(Collider col)
    {
        if (col == null) return;
        // If this collider belongs to a stick's tip (has StickTipCollision or ShotHitbox), notify PlayerPuckHandler.
        // Check multiple locations: the collider GameObject, its parent chain, attached Rigidbody root, and its children.
        GameObject colliderGO = col.gameObject;
        GameObject rbGO = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : null;

        bool IsStickOrHitbox(GameObject go)
        {
            if (go == null) return false;
            if (go.GetComponent<StickTipCollision>() != null) return true;
            if (go.GetComponent<ShotHitbox>() != null) return true;
            if (go.GetComponentInParent<StickTipCollision>() != null) return true;
            if (go.GetComponentInParent<ShotHitbox>() != null) return true;
            if (go.GetComponentInChildren<StickTipCollision>() != null) return true;
            if (go.GetComponentInChildren<ShotHitbox>() != null) return true;
            return false;
        }

        bool detected = IsStickOrHitbox(colliderGO) || IsStickOrHitbox(rbGO);
        if (!detected)
        {
            // Some XR setups attach colliders in unusual places. Try simple name heuristic as a last resort.
            if (colliderGO.name.ToLower().Contains("hitpoint") || (rbGO != null && rbGO.name.ToLower().Contains("hitpoint")))
                detected = true;
        }

        if (detected)
        {
            if (PlayerPuckHandler.Instance != null)
            {
                
                PlayerPuckHandler.Instance.BreakPossessionFromCollision();
            }
        }
    }
}
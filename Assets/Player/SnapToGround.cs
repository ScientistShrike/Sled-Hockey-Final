using UnityEngine;

public class SnapToGround : MonoBehaviour
{
    public float rayHeightOffset = 5f;          // How far above the player to start the ray
    public LayerMask groundLayer;              // Set to your ground layer
    public bool snapOnStart = true;            // Auto-snap on start

    void Start()
    {
        if (snapOnStart)
            Snap();
    }

    public void Snap()
    {
        Vector3 rayStart = transform.position + Vector3.up * rayHeightOffset;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayHeightOffset * 2f, groundLayer))
        {
            transform.position = hit.point;
            Debug.Log("Player snapped to ground at: " + hit.point);
        }
        else
        {
            Debug.LogWarning("No ground found below. Check ground layer and position.");
        }
    }
}

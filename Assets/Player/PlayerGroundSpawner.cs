using UnityEngine;

public class PlayerGroundSpawner : MonoBehaviour
{
    public float startRaycastHeight = 5f;               // How high above to start the raycast
    public float spawnHeightAboveGround = 0.5f;         // How far above the ground to place the player
    public LayerMask groundLayer;                       // Layer assigned to your arena floor

    void Start()
    {
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + startRaycastHeight, transform.position.z);
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, startRaycastHeight * 2f, groundLayer))
        {
            Vector3 spawnPosition = hit.point + Vector3.up * spawnHeightAboveGround;
            transform.position = spawnPosition;
        }
        else
        {
        }
    }
}

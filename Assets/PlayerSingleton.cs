using UnityEngine;

/// <summary>
/// Attach this script to your main player GameObject to make it a persistent singleton.
/// This will prevent the player from being duplicated when scenes are reloaded.
/// </summary>
public class PlayerSingleton : MonoBehaviour
{
    public static PlayerSingleton Instance { get; private set; }

    public Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // An instance already exists, so destroy this new one.
            // This happens when a scene reloads and tries to create a new player.
            Destroy(gameObject);
        }
        else
        {
            // This is the first and only instance, so make it the singleton.
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main;
        }
    }

    /// <summary>
    /// Resets the player's position and rotation to the spawn point in a VR-safe way.
    /// </summary>
    public void ResetToSpawnPoint(Transform spawnPoint)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera is not assigned on PlayerSingleton!", this);
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            return;
        }

        // Calculate the offset from the rig's origin to the camera's position on the horizontal plane.
        Vector3 cameraOffset = mainCamera.transform.position - transform.position;
        cameraOffset.y = 0;

        // Move the rig so the camera is at the spawn point.
        transform.position = spawnPoint.position - cameraOffset;

        // Rotate the rig to match the spawn point's orientation.
        float angleOffset = spawnPoint.eulerAngles.y - mainCamera.transform.eulerAngles.y;
        transform.Rotate(0, angleOffset, 0);
    }
}
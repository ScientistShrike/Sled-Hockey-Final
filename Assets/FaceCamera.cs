using UnityEngine;

/// <summary>
/// This component positions and orients its GameObject in front of a target camera when enabled.
/// It's useful for world-space UI panels in VR that need to face the player.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The camera to face. If not set, it will default to the main camera.")]
    public Camera targetCamera;

    [Header("Positioning")]
    [Tooltip("The distance from the camera to place the object.")]
    public float distance = 2.0f;
    [Tooltip("The vertical offset from the camera's height.")]
    public float heightOffset = -0.5f;

    [Header("Orientation")]
    [Tooltip("If true, the object will rotate to face the player.")]
    public bool facePlayer = true;

    void Awake()
    {
        // Find the target camera if not already assigned.
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("FaceCamera: No target camera assigned and Camera.main is not found!", this);
            }
        }
    }

    void OnEnable()
    {
        PositionInFrontOfCamera();
    }

    /// <summary>
    /// Positions and orients the GameObject in front of the target camera.
    /// </summary>
    public void PositionInFrontOfCamera()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("FaceCamera: Target camera is not set. Cannot position the object.", this);
            return;
        }

        Transform cameraTransform = targetCamera.transform;

        // Determine the forward direction on the horizontal plane to prevent tilting.
        Vector3 forwardDirection = cameraTransform.forward;
        forwardDirection.y = 0;
        forwardDirection.Normalize();

        // If the camera is looking straight up or down, forwardDirection will be zero.
        // In that case, use a fallback direction (e.g., world forward) to prevent issues.
        if (forwardDirection == Vector3.zero)
        {
            forwardDirection = Vector3.forward;
        }

        // Position the panel in front of the camera on the horizontal plane.
        Vector3 targetPosition = cameraTransform.position + forwardDirection * distance;
        
        // Apply the vertical offset relative to the camera's height.
        targetPosition.y = cameraTransform.position.y + heightOffset;

        transform.position = targetPosition;

        // Orient the object to face the player.
        if (facePlayer)
        {
            // Make the panel face the user, but stay upright
            Vector3 lookAtPosition = new Vector3(cameraTransform.position.x, transform.position.y, cameraTransform.position.z);
            transform.LookAt(lookAtPosition);
            
            // A UI Canvas is typically viewed from its back (Z-).
            // LookAt points the Z+ axis towards the target, so we need to rotate it 180 degrees on its Y axis.
            transform.Rotate(0, 180, 0);
        }
    }
}


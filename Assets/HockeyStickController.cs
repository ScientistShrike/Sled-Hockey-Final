using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal dual-stick flip controller.
/// - Each stick follows its controller (position and rotation) maintaining the spawn offsets.
/// - Press the configured flip button to toggle a flip (applies an Euler delta to the stored local rotation).
/// This script focuses only on flipping behavior (no hit physics) to keep it simple and reliable.
/// </summary>
public class HockeyStickController : MonoBehaviour
{
    [Header("Stick Models & Controllers")]
    public GameObject leftStickModel;
    public Transform leftControllerTransform;

    public GameObject rightStickModel;
    public Transform rightControllerTransform;

    [Header("Flip Settings")]
    public Vector3 leftFlipEulerDelta = new Vector3(180f, 0f, 0f);
    public Vector3 rightFlipEulerDelta = new Vector3(180f, 0f, 0f);

    [Header("Input Actions")]
    public InputActionReference leftFlipAction;
    public InputActionReference rightFlipAction;

    [Header("Smoothing")]
    [Tooltip("Rotation slerp speed when flipping")]
    public float rotationSlerpSpeed = 20f;

    // Stored local rotations relative to the controller
    private Quaternion leftInitialLocalRot;
    private Quaternion leftFlippedLocalRot;
    private bool leftFlipped = false;

    private Quaternion rightInitialLocalRot;
    private Quaternion rightFlippedLocalRot;
    private bool rightFlipped = false;

    void Start()
    {
        // Parent the stick models to their controllers and capture initial local rotations.
        if (leftStickModel != null && leftControllerTransform != null)
        {
            leftStickModel.transform.SetParent(leftControllerTransform, worldPositionStays: true);
            leftInitialLocalRot = leftStickModel.transform.localRotation;
            leftFlippedLocalRot = leftInitialLocalRot * Quaternion.Euler(leftFlipEulerDelta);
            leftFlipped = false;
        }

        // Capture right initial offsets
        if (rightStickModel != null && rightControllerTransform != null)
        {
            rightStickModel.transform.SetParent(rightControllerTransform, worldPositionStays: true);
            rightInitialLocalRot = rightStickModel.transform.localRotation;
            rightFlippedLocalRot = rightInitialLocalRot * Quaternion.Euler(rightFlipEulerDelta);
            rightFlipped = false;
        }
    }

    void OnEnable()
    {
        if (leftFlipAction != null)
        {
            leftFlipAction.action.Enable();
            leftFlipAction.action.performed += OnLeftFlipPerformed;
        }
        if (rightFlipAction != null)
        {
            rightFlipAction.action.Enable();
            rightFlipAction.action.performed += OnRightFlipPerformed;
        }
    }

    void OnDisable()
    {
        if (leftFlipAction != null)
        {
            leftFlipAction.action.performed -= OnLeftFlipPerformed;
            leftFlipAction.action.Disable();
        }
        if (rightFlipAction != null)
        {
            rightFlipAction.action.performed -= OnRightFlipPerformed;
            rightFlipAction.action.Disable();
        }
    }

    void Update()
    {
        // Smoothly update the local rotation of the sticks towards their target (flipped or not)
        if (leftStickModel != null && leftControllerTransform != null)
        {
            Quaternion targetLocalRot = leftFlipped ? leftFlippedLocalRot : leftInitialLocalRot;
            leftStickModel.transform.localRotation = Quaternion.Slerp(leftStickModel.transform.localRotation, targetLocalRot, Time.deltaTime * rotationSlerpSpeed);
        }

        if (rightStickModel != null && rightControllerTransform != null)
        {
            Quaternion targetLocalRot = rightFlipped ? rightFlippedLocalRot : rightInitialLocalRot;
            rightStickModel.transform.localRotation = Quaternion.Slerp(rightStickModel.transform.localRotation, targetLocalRot, Time.deltaTime * rotationSlerpSpeed);
        }
    }

    private void OnLeftFlipPerformed(InputAction.CallbackContext ctx)
    {
        leftFlipped = !leftFlipped;
    }

    private void OnRightFlipPerformed(InputAction.CallbackContext ctx)
    {
        rightFlipped = !rightFlipped;
    }
}

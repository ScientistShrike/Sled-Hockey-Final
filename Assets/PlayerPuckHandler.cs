using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Added for TextMeshPro UI

[RequireComponent(typeof(LineRenderer))]
public class PlayerPuckHandler : MonoBehaviour
{
    private enum PuckState { Idle, Aiming }
    private PuckState currentState = PuckState.Idle;

    [Header("Puck Handling")]
    public float catchRange = 2f;
    public float holdTimeLimit = 3.0f;

    [Header("Shooting")]
    public InputActionReference shootActionReference;
    public float shotForce = 25f;
    [Tooltip("Time after shooting before the puck can be re-caught.")]
    public float shotCooldown = 0.5f;

    [Header("Aiming")]
    public float aimSweepAngle = 45f;
    public float aimSweepSpeed = 2f;

    [Header("UI")]
    public GameObject timerCanvas;
    public TextMeshProUGUI timerText;

    [Header("External References")]
    public XRStickMovement playerMovement;

    public bool hasPuck { get; private set; } = false;
    public static PlayerPuckHandler Instance { get; private set; }

    private Transform puckTransform;
    private Rigidbody puckRigidbody;
    private LineRenderer aimLineRenderer;
    private float holdTimer;
    private Vector3 currentAimDirection;
    private float puckReleaseCooldown = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }

        aimLineRenderer = GetComponent<LineRenderer>();
        if (playerMovement == null) { playerMovement = GetComponent<XRStickMovement>(); }
    }

    void OnEnable()
    {
        if (shootActionReference != null)
        {
            shootActionReference.action.Enable();
            shootActionReference.action.performed += OnShoot;
        }
    }

    void OnDisable()
    {
        if (shootActionReference != null)
        {
            shootActionReference.action.performed -= OnShoot;
        }
    }

    void Start()
    {
        GameObject puckObj = GameObject.Find("hockey_puck");
        if (puckObj != null)
        {
            puckTransform = puckObj.transform;
            puckRigidbody = puckObj.GetComponent<Rigidbody>();
        }
        else
        {
            Debug.LogError("PlayerPuckHandler Error: Could not find 'hockey_puck'.", this);
            this.enabled = false;
            return;
        }

        ConfigureAimLine();
        if (timerCanvas != null) timerCanvas.SetActive(false);
    }

    private void ConfigureAimLine()
    {
        aimLineRenderer.positionCount = 2;
        aimLineRenderer.startWidth = 0.1f;
        aimLineRenderer.endWidth = 0.05f;
        aimLineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        aimLineRenderer.startColor = Color.cyan;
        aimLineRenderer.endColor = Color.clear;
        aimLineRenderer.enabled = false;
    }

    void Update()
    {
        if (puckReleaseCooldown > 0)
        {
            puckReleaseCooldown -= Time.deltaTime;
        }

        if (currentState == PuckState.Idle)
        {
            HandleIdleState();
        }
        else if (currentState == PuckState.Aiming)
        {
            HandleAimingState();
        }
    }

    private void HandleIdleState()
    {
        // Only attempt to catch the puck if the cooldown is over.
        if (puckReleaseCooldown <= 0 && Vector3.Distance(transform.position, puckTransform.position) < catchRange)
        {
            puckRigidbody.linearVelocity = Vector3.zero;
            puckRigidbody.angularVelocity = Vector3.zero;
            hasPuck = true;

            if (playerMovement != null) playerMovement.enabled = false;
            if (timerCanvas != null) timerCanvas.SetActive(true);

            currentState = PuckState.Aiming;
            holdTimer = holdTimeLimit;
            aimLineRenderer.enabled = true;
        }
    }

    private void HandleAimingState()
    {
        if (Vector3.Distance(transform.position, puckTransform.position) > catchRange * 1.2f)
        {
            LoseControlOfPuck();
            return;
        }

        puckRigidbody.linearVelocity = Vector3.zero;
        puckRigidbody.angularVelocity = Vector3.zero;

        holdTimer -= Time.deltaTime;
        if (timerText != null) timerText.text = holdTimer.ToString("F1");

        if (holdTimer <= 0f)
        {
            LoseControlOfPuck();
            return;
        }

        float sweepValue = Mathf.Sin(Time.time * aimSweepSpeed) * aimSweepAngle;
        currentAimDirection = Quaternion.Euler(0, sweepValue, 0) * transform.forward;

        aimLineRenderer.SetPosition(0, transform.position);
        aimLineRenderer.SetPosition(1, transform.position + currentAimDirection * 10f);
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (currentState == PuckState.Aiming)
        {
            ShootPuck();
        }
    }

    private void ShootPuck()
    {
        puckRigidbody.AddForce(currentAimDirection * shotForce, ForceMode.Impulse);
        puckReleaseCooldown = shotCooldown; // Start cooldown to prevent re-catching.
        ResetToIdle();
    }

    private void LoseControlOfPuck()
    {
        Vector3 randomNudge = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        puckRigidbody.AddForce(randomNudge * 2f, ForceMode.Impulse);
        puckReleaseCooldown = shotCooldown; // Start cooldown to prevent re-catching.
        ResetToIdle();
    }

    private void ResetToIdle()
    {
        hasPuck = false;
        aimLineRenderer.enabled = false;

        if (playerMovement != null) playerMovement.enabled = true;
        if (timerCanvas != null) timerCanvas.SetActive(false);

        currentState = PuckState.Idle;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, catchRange);
    }
}

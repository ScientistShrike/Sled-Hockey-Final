using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Added for TextMeshPro UI

public class PlayerPuckHandler : MonoBehaviour
{

    [Header("Puck Handling")]
    public float catchRange = 2f;
    public float holdTimeLimit = 3.0f;

    [Header("Shooting")]
    public InputActionReference shootActionReference;
    public float shotForce = 1.5f; // multiplier applied to stick velocity
    [Tooltip("Time after shooting before the puck can be re-caught.")]
    public float shotCooldown = 0.5f;

    [Header("UI")]
    public GameObject timerCanvas;
    public TextMeshProUGUI timerText;

    [Header("External References")]
    public XRStickMovement playerMovement;
    [Tooltip("If set, this Transform will be used as the stick tip to hold/release the puck. If empty, the player's rightStickEnd will be used.")]
    public Transform stickTipOverride;
    [Tooltip("Drag the GameObjects with the colliders you want to use for shooting here.")]
    public System.Collections.Generic.List<GameObject> shotHitboxes;

    [Header("Possession Settings")]
    [Tooltip("Linear drag applied to puck while in possession (higher = stops faster)")]
    public float possessionDrag = 8f;
    [Tooltip("Angular drag applied to puck while in possession")]
    public float possessionAngularDrag = 4f;
    [Tooltip("Local offset from player where puck should softly hold (in player's local space)")]
    public Vector3 possessionOffset = new Vector3(0f, 0f, 1f);
    [Tooltip("How fast the puck moves towards the possession offset (higher = snaps faster)")]
    public float possessionSnapSpeed = 10f;
    [Header("Possession Timing")]
    [Tooltip("How long possession lasts before auto-release (seconds)")]
    public float possessionDuration = 5f;
    [Tooltip("Cooldown after possession ends before it can be regained")]
    public float possessionCooldown = 1.0f;
    [Tooltip("How long possession remains disabled when a stick collides with the puck (seconds)")]
    public float stickTouchPossessionCooldown = 0.4f;

    [Header("Side Offsets")]
    public Vector3 leftPossessionOffset = new Vector3(-0.5f, 0f, 1f);
    public Vector3 rightPossessionOffset = new Vector3(0.5f, 0f, 1f);
    public Vector3 centerPossessionOffset = new Vector3(0f, 0f, 1f);

    [Header("Side Switch Inputs")]
    public InputActionReference leftSwitchAction;
    public InputActionReference rightSwitchAction;

    public bool hasPuck { get; private set; } = false;
    public static PlayerPuckHandler Instance { get; private set; }

    private Transform puckTransform;
    private Rigidbody puckRigidbody;
    [Header("References")]
    [Tooltip("If set, this is the puck object that this PlayerPuckHandler will use. If empty, the handler will attempt to find GameObject named 'hockey_puck'.")]
    // Assign the puck in the Inspector to ensure the handler references the correct object.
    // If left empty, the handler will fall back to finding the object named "hockey_puck" at Start.
    public GameObject puckObject;
    private float puckReleaseCooldown = 0f;
    // stick tracking for velocity calculation
    private Transform leftStickTip;
    private Transform rightStickTip;
    private Vector3 prevLeftPos;
    private Vector3 prevRightPos;
    private Vector3 lastLeftVelocity;
    private Vector3 lastRightVelocity;
    private bool puckPossessed = false;
    private float originalLinearDamping = 0f;
    private float originalAngularDamping = 0f;
    [Header("Hit Settings")]
    [Tooltip("Multiplier applied to measured stick velocity when hitting")]
    public float hitForceMultiplier = 1.5f;
    [Tooltip("Minimum stick speed required to register a hit (m/s)")]
    public float minHitSpeed = 0.5f;
    [Tooltip("Frames used to average stick velocity for more stable hits")] 
    public int velocityAverageFrames = 3;

    // velocity history for smoothing
    private Vector3[] leftVelHistory;
    private Vector3[] rightVelHistory;
    private int leftVelIndex = 0;
    private int rightVelIndex = 0;
    private int leftVelCount = 0;
    private int rightVelCount = 0;
    private float possessionTimer = 0f;
    private float possessionCooldownTimer = 0f;

    private enum PossessionSide { Center, Left, Right }
    private PossessionSide currentSide = PossessionSide.Center;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }

        if (playerMovement == null) { playerMovement = GetComponent<XRStickMovement>(); }
    }

    void OnEnable()
    {
        if (shootActionReference != null)
        {
            shootActionReference.action.Enable();
            shootActionReference.action.performed += OnShoot;
        }
        if (leftSwitchAction != null)
        {
            leftSwitchAction.action.Enable();
            leftSwitchAction.action.performed += OnLeftSwitch;
            leftSwitchAction.action.started += OnLeftSwitch;
        }
        if (rightSwitchAction != null)
        {
            rightSwitchAction.action.Enable();
            rightSwitchAction.action.performed += OnRightSwitch;
            rightSwitchAction.action.started += OnRightSwitch;
        }
    }

    void OnDisable()
    {
        if (shootActionReference != null)
        {
            shootActionReference.action.performed -= OnShoot;
            shootActionReference.action.Disable();
        }
        if (leftSwitchAction != null)
        {
            leftSwitchAction.action.performed -= OnLeftSwitch;
            leftSwitchAction.action.started -= OnLeftSwitch;
            leftSwitchAction.action.Disable();
        }
        if (rightSwitchAction != null)
        {
            rightSwitchAction.action.performed -= OnRightSwitch;
            rightSwitchAction.action.started -= OnRightSwitch;
            rightSwitchAction.action.Disable();
        }
    }

    void Start()
    {
        // Use inspector assigned puck if available
        if (puckObject != null)
        {
            puckTransform = puckObject.transform;
            puckRigidbody = puckObject.GetComponent<Rigidbody>();
        }
        else
        {
            GameObject puckObj = GameObject.Find("hockey_puck");
            if (puckObj != null)
            {
                puckTransform = puckObj.transform;
                puckRigidbody = puckObj.GetComponent<Rigidbody>();
            }
            else
            {
                this.enabled = false;
                return;
            }
        }

        if (timerCanvas != null) timerCanvas.SetActive(false);

        // setup velocity history buffers
        int n = Mathf.Max(1, velocityAverageFrames);
        leftVelHistory = new Vector3[n];
        rightVelHistory = new Vector3[n];

        // Ensure collision helper components exist on stick tips and hitboxes so gameplay interactions run
        SetupCollisionHelpers();
        // Add a PuckCollisionLogger to help debug collisions between stick and puck
        if (puckObject != null && puckObject.GetComponent<PuckCollisionLogger>() == null)
        {
            puckObject.AddComponent<PuckCollisionLogger>();
        }
    }

    private void SetupCollisionHelpers()
    {
        if (playerMovement == null)
        {
        }
        // Left stick tip
        if (playerMovement != null && playerMovement.leftStickEnd != null)
        {
            Transform leftTransform = playerMovement.leftStickEnd;
            var leftColliders = leftTransform.GetComponentsInChildren<Collider>(true);
            

            foreach (var collider in leftColliders)
            {
                var go = collider.gameObject;
                    if (go.GetComponent<StickTipCollision>() == null)
                    {
                        go.AddComponent<StickTipCollision>();
                    }
            }
        }
        else
        {
        }

        // Right stick tip (with override support)
        Transform rTip = stickTipOverride != null ? stickTipOverride : (playerMovement != null ? playerMovement.rightStickEnd : null);
        if (rTip != null)
        {
            Transform rightTransform = rTip;
            var rightColliders = rightTransform.GetComponentsInChildren<Collider>(true);
            

            foreach (var collider in rightColliders)
            {
                var go = collider.gameObject;
                    if (go.GetComponent<StickTipCollision>() == null)
                    {
                        go.AddComponent<StickTipCollision>();
                    }
            }
        }
        else
        {
        }

        // Shot hitboxes
        if (shotHitboxes != null)
        {
            foreach (var go in shotHitboxes)
            {
                if (go == null) continue;
                var col = go.GetComponent<Collider>();
                
                // Attach ShotHitbox to each collider under the hitbox GameObject
                var colliders = go.GetComponentsInChildren<Collider>(true);
                    
                foreach (var c in colliders)
                {
                    var goC = c.gameObject;
                    if (goC.GetComponent<ShotHitbox>() == null)
                    {
                        goC.AddComponent<ShotHitbox>();
                    }
                }
            }
        }
    }

    // Helper: check if a given Rigidbody is the puck registered with this handler
    public bool IsRegisteredPuck(Rigidbody rb)
    {
        return rb != null && rb == puckRigidbody;
    }

    // Programmatic setter for runtime or dynamic puck assignment
    public void SetPuck(GameObject go)
    {
        if (go == null) return;
        puckObject = go;
        puckTransform = puckObject.transform;
        puckRigidbody = puckObject.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // While possessed, softly move the puck toward the desired local offset position
        if (puckPossessed && puckRigidbody != null && puckTransform != null)
        {
            Vector3 desiredWorld = transform.TransformPoint(possessionOffset);

            // compute velocity needed to move toward desired position this fixed step
            float dt = Time.fixedDeltaTime;
            Vector3 toTarget = desiredWorld - puckTransform.position;
            Vector3 desiredVel = toTarget * possessionSnapSpeed;

            // apply desired velocity but respect damping (we set linear damping earlier)
            puckRigidbody.linearVelocity = desiredVel;
            puckRigidbody.angularVelocity *= 0.1f; // reduce spin while possessed
        }
    }

    // No grab/attach behaviour: we compute stick velocity and hit the puck on input

    void Update()
    {
        if (puckReleaseCooldown > 0)
        {
            puckReleaseCooldown -= Time.deltaTime;
        }

        // update stick tip references
        if (playerMovement != null)
        {
            leftStickTip = playerMovement.leftStickEnd;
            rightStickTip = playerMovement.rightStickEnd;
        }
        // allow override to replace right stick
        if (stickTipOverride != null)
        {
            rightStickTip = stickTipOverride;
        }

        // compute left stick velocity
        if (leftStickTip != null)
        {
            if (prevLeftPos == Vector3.zero) prevLeftPos = leftStickTip.position;
            float dtL = Mathf.Max(0.0001f, Time.deltaTime);
            lastLeftVelocity = (leftStickTip.position - prevLeftPos) / dtL;
            prevLeftPos = leftStickTip.position;

            // store in history
            if (leftVelHistory != null && leftVelHistory.Length > 0)
            {
                leftVelHistory[leftVelIndex] = lastLeftVelocity;
                leftVelIndex = (leftVelIndex + 1) % leftVelHistory.Length;
                if (leftVelCount < leftVelHistory.Length) leftVelCount++;
            }
        }

        // compute right stick velocity
        if (rightStickTip != null)
        {
            if (prevRightPos == Vector3.zero) prevRightPos = rightStickTip.position;
            float dtR = Mathf.Max(0.0001f, Time.deltaTime);
            lastRightVelocity = (rightStickTip.position - prevRightPos) / dtR;
            prevRightPos = rightStickTip.position;

            // store in history
            if (rightVelHistory != null && rightVelHistory.Length > 0)
            {
                rightVelHistory[rightVelIndex] = lastRightVelocity;
                rightVelIndex = (rightVelIndex + 1) % rightVelHistory.Length;
                if (rightVelCount < rightVelHistory.Length) rightVelCount++;
            }
        }

        // Soft possession when near player: increase drag and slow puck but keep it hittable
        if (puckRigidbody != null && !puckPossessed && puckReleaseCooldown <= 0f && possessionCooldownTimer <= 0f)
        {
            float dist = Vector3.Distance(transform.position, puckTransform.position);
            if (dist <= catchRange)
            {
                // store original physics values
                originalLinearDamping = puckRigidbody.linearDamping;
                originalAngularDamping = puckRigidbody.angularDamping;

                // apply possession damping so puck slows significantly but remains dynamic
                puckRigidbody.linearDamping = possessionDrag;
                puckRigidbody.angularDamping = possessionAngularDrag;
                puckRigidbody.linearVelocity *= 0.1f;
                puckRigidbody.angularVelocity *= 0.1f;

                puckPossessed = true;
                hasPuck = true;
                possessionTimer = possessionDuration;
                possessionCooldownTimer = 0f;

                // set offset according to current side selection
                switch (currentSide)
                {
                    case PossessionSide.Left: possessionOffset = leftPossessionOffset; break;
                    case PossessionSide.Right: possessionOffset = rightPossessionOffset; break;
                    default: possessionOffset = centerPossessionOffset; break;
                }
            }
        }

        // decrement possession timers
        if (possessionTimer > 0f)
        {
            possessionTimer -= Time.deltaTime;
            if (possessionTimer <= 0f && puckPossessed)
            {
                // release possession without applying a hit
                ReleasePossession();
            }
        }
        if (possessionCooldownTimer > 0f)
        {
            possessionCooldownTimer -= Time.deltaTime;
        }
    }

    // No idle/holding states anymore - hitting is proximity based when shooting input occurs

    private void OnLeftSwitch(InputAction.CallbackContext ctx)
    {
        currentSide = PossessionSide.Left;
        possessionOffset = leftPossessionOffset;

        // If currently possessed, nudge the puck toward the new offset immediately
        if (puckPossessed && puckRigidbody != null && puckTransform != null)
        {
            Vector3 desiredWorld = transform.TransformPoint(possessionOffset);
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            Vector3 desiredVel = (desiredWorld - puckTransform.position) * possessionSnapSpeed;
            puckRigidbody.linearVelocity = desiredVel;
        }
    }

    private void OnRightSwitch(InputAction.CallbackContext ctx)
    {
        currentSide = PossessionSide.Right;
        possessionOffset = rightPossessionOffset;

        // If currently possessed, nudge the puck toward the new offset immediately
        if (puckPossessed && puckRigidbody != null && puckTransform != null)
        {
            Vector3 desiredWorld = transform.TransformPoint(possessionOffset);
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            Vector3 desiredVel = (desiredWorld - puckTransform.position) * possessionSnapSpeed;
            puckRigidbody.linearVelocity = desiredVel;
        }
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (puckRigidbody == null || puckTransform == null) return;

        if (puckReleaseCooldown > 0f) return;


        // check left stick
        if (leftStickTip != null && Vector3.Distance(leftStickTip.position, puckTransform.position) <= catchRange)
        {
            HitPuck(lastLeftVelocity);
            return;
        }

        // check right stick
        if (rightStickTip != null && Vector3.Distance(rightStickTip.position, puckTransform.position) <= catchRange)
        {
            HitPuck(lastRightVelocity);
            return;
        }
    }

    // Called by stick-tip collision or input-based hit. Unfreeze if necessary and apply velocity.
    public void HitPuck(Vector3 stickVelocity)
    {

        if (puckRigidbody == null || puckTransform == null) return;

        // If the passed stickVelocity is too small, try to use averaged velocities as a fallback
        if (stickVelocity.magnitude < minHitSpeed)
        {
            Vector3 avgL = Vector3.zero;
            Vector3 avgR = Vector3.zero;

            if (leftVelCount > 0)
            {
                for (int i = 0; i < leftVelCount; i++) avgL += leftVelHistory[i];
                avgL /= Mathf.Max(1, leftVelCount);
            }
            if (rightVelCount > 0)
            {
                for (int i = 0; i < rightVelCount; i++) avgR += rightVelHistory[i];
                avgR /= Mathf.Max(1, rightVelCount);
            }

            // pick whichever average is stronger
            if (avgL.magnitude > avgR.magnitude && avgL.magnitude >= minHitSpeed)
                stickVelocity = avgL;
            else if (avgR.magnitude >= minHitSpeed)
                stickVelocity = avgR;
        }

        // If still below threshold, ignore the hit completely.
        if (stickVelocity.magnitude < minHitSpeed)
        {
            return;
        }

        // If we get here, the hit is valid. Now, release possession and apply force.
        if (puckPossessed)
        {
            puckRigidbody.linearDamping = originalLinearDamping;
            puckRigidbody.angularDamping = originalAngularDamping;
            puckPossessed = false;
            hasPuck = false;
        }

        // Apply hit velocity scaled by shotForce and hitForceMultiplier
        float appliedMultiplier = shotForce * hitForceMultiplier;
        Vector3 hitVel = stickVelocity * appliedMultiplier;
        puckRigidbody.linearVelocity = hitVel;

        // If the measured stick velocity is tiny or the calculated hitVel magnitude is very low, use a minimum fallback
        
        
        puckRigidbody.angularVelocity = Vector3.Cross(Vector3.up, stickVelocity) * 0.1f * appliedMultiplier;
        
        puckReleaseCooldown = shotCooldown;
        // After a hit, block re-possession for the configured possessionCooldown as well
        possessionCooldownTimer = Mathf.Max(possessionCooldownTimer, Mathf.Max(possessionCooldown, shotCooldown));
    }

    // Centralized entry point for colliders (stick tips / shot hitboxes) to report a collision
    // rather than computing velocity locally. This makes collision-handling consistent with input-based shots.
    public void HitPuckFromCollider(Transform colliderTransform)
    {
        if (puckRigidbody == null || puckTransform == null) return;

        // Break possession and set cooldowns
        BreakPossessionFromCollision();

        Vector3 stickVelocity = Vector3.zero;

        // Use known stick transforms when possible
        if (leftStickTip != null && (colliderTransform == leftStickTip || colliderTransform.IsChildOf(leftStickTip)))
        {
            stickVelocity = lastLeftVelocity;
        }
        else if (rightStickTip != null && (colliderTransform == rightStickTip || colliderTransform.IsChildOf(rightStickTip)))
        {
            stickVelocity = lastRightVelocity;
        }
        else
        {
            // Fall back to the average velocities we already record
            Vector3 avgL = Vector3.zero;
            Vector3 avgR = Vector3.zero;
            if (leftVelCount > 0)
            {
                for (int i = 0; i < leftVelCount; i++) avgL += leftVelHistory[i];
                avgL /= Mathf.Max(1, leftVelCount);
            }
            if (rightVelCount > 0)
            {
                for (int i = 0; i < rightVelCount; i++) avgR += rightVelHistory[i];
                avgR /= Mathf.Max(1, rightVelCount);
            }
            if (avgL.magnitude > avgR.magnitude && avgL.magnitude >= minHitSpeed)
                stickVelocity = avgL;
            else if (avgR.magnitude >= minHitSpeed)
                stickVelocity = avgR;
        }

        HitPuck(stickVelocity);
    }

    private void ReleasePossession()
    {
        if (puckRigidbody == null) return;
        // restore damping
        puckRigidbody.linearDamping = originalLinearDamping;
        puckRigidbody.angularDamping = originalAngularDamping;
        puckPossessed = false;
        hasPuck = false;
        possessionCooldownTimer = possessionCooldown;
    }

    // Called by stick collision detectors to break possession and set a cooldown
    public void BreakPossessionFromCollision(float cooldownOverride = -1f)
    {
        if (puckRigidbody == null) return;

        if (puckPossessed)
        {
            // restore damping
            puckRigidbody.linearDamping = originalLinearDamping;
            puckRigidbody.angularDamping = originalAngularDamping;
            puckPossessed = false;
            hasPuck = false;
        }

        float cooldown = cooldownOverride > 0f ? cooldownOverride : stickTouchPossessionCooldown;
        
        possessionCooldownTimer = Mathf.Max(possessionCooldownTimer, cooldown);
        puckReleaseCooldown = Mathf.Max(puckReleaseCooldown, cooldown);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, catchRange);
    }
}
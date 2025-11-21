using System.Linq; // Add at the top for LINQ
using System.Collections.Generic; // Add this line
using UnityEngine;
using UnityEngine.AI;

public class EnemyAiTutorial : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player; // Hockey puck
    public Transform goal;   // Player's goal

    public LayerMask whatIsGround, whatIsPlayer;

    [Header("Stamina Settings")]
    public float minMoveTime = 8f;
    public float maxMoveTime = 12f;
    public float restTime = 5f;
    private float staminaTimer;
    private enum AiState { Moving, Resting }
    private AiState currentState = AiState.Moving;

    [Header("Patrol Settings")]
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange = 10f;

    [Header("Attacking Settings")]
    public float timeBetweenAttacks = 2f;
    bool alreadyAttacked;
    public float meleeRange = 2f;
    public float meleeForce = 20f;

    [Header("Shooting Settings")]
    [Range(0f, 100f)]
    public float shotAccuracyPercentage = 75f;
    public float shotInaccuracyAngle = 15f;

    [Header("Hesitation Settings")]
    public float playerPickupHesitation = 2.0f; // How long AI hesitates when player gets the puck
    private float hesitationTimer = 0f;

    [Header("AI States")]
    public float sightRange = 10f, attackRange = 3f;

    [Header("Zone Restriction")]
    public Vector3 minBounds = new Vector3(-10, 0, -10);
    public Vector3 maxBounds = new Vector3(10, 0, 10);

    [Header("Puck Logic")]
    public float pushOffset = 1.5f;
    public bool hasPuck = false;
    public float holdPuckTime = 1.5f;
    private float holdTimer = 0f;
    public float puckStopCooldown = 1.0f;
    private float puckStopTimer = 0f;

    [Header("Team Logic")]
    public static List<EnemyAiTutorial> allBots = new List<EnemyAiTutorial>();
    public List<EnemyAiTutorial> passTargets = new List<EnemyAiTutorial>();
    public enum Team { TeamA, TeamB }
    public Team botTeam;

    [Header("Player Settings")]
    public Team playerTeam = Team.TeamA;
    private static Transform humanPlayerTransform;
    [Header("Animation")]
    public AnimationController animController;

    private void Awake()
    {
        player = GameObject.Find("hockey_puck").transform;
        agent = GetComponent<NavMeshAgent>();
        staminaTimer = Random.Range(minMoveTime, maxMoveTime);
        hesitationTimer = playerPickupHesitation;

        if (humanPlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) humanPlayerTransform = playerObj.transform;
        }

        if (animController == null) animController = GetComponentInChildren<AnimationController>();
    }

    private void OnEnable() => allBots.Add(this);
    private void OnDisable() => allBots.Remove(this);

    private float randomMoveTimer = 0f;
    private float randomMoveInterval = 3f;
    private bool isRandomChasing = false;

    private void Update()
    {
        HandleStamina();
        if (currentState == AiState.Resting) return;

        if (puckStopTimer > 0f) puckStopTimer -= Time.deltaTime;

        hasPuck = IsPuckClose();

        if (animController != null)
        {
            animController.SetPushing(agent.velocity.magnitude > 0.1f);
        }

        if (hasPuck)
        {
            HandlePuckPossession();
            return;
        }

        HandleTeamMovement();
    }

    private void HandleStamina()
    {
        AiState prev = currentState;
        staminaTimer -= Time.deltaTime;
        if (currentState == AiState.Moving && staminaTimer <= 0f)
        {
            currentState = AiState.Resting;
            staminaTimer = restTime;
            agent.isStopped = true;
        }
        else if (currentState == AiState.Resting && staminaTimer <= 0f)
        {
            currentState = AiState.Moving;
            staminaTimer = Random.Range(minMoveTime, maxMoveTime);
            agent.isStopped = false;
        }

        if (prev != currentState && animController != null)
        {
            animController.SetTired(currentState == AiState.Resting);
        }
    }

    private void HandlePuckPossession()
    {
        agent.SetDestination(transform.position);

        if (puckStopTimer <= 0f)
        {
            Rigidbody puckRb = player.GetComponent<Rigidbody>();
            if (puckRb != null) { puckRb.linearVelocity = puckRb.angularVelocity = Vector3.zero; }
        }

        holdTimer += Time.deltaTime;

        if (holdTimer >= holdPuckTime && !alreadyAttacked)
        {
            if (!TryPass()) // If no better pass option is available
            {
                TryShootAtGoal(); // Then try to shoot
            }
            holdTimer = 0f;
        }
    }

    private void HandleTeamMovement()
    {
        // --- Offensive Logic: Check if my team (AI or human) has the puck ---
        EnemyAiTutorial aiTeammateWithPuck = GetTeammateWithPuck();
        bool humanTeammateHasPuck = (PlayerPuckHandler.Instance != null && PlayerPuckHandler.Instance.hasPuck && this.botTeam == playerTeam);

        if (aiTeammateWithPuck != null || humanTeammateHasPuck)
        {
            GetOpenPosition();
            return;
        }

        // --- Defensive Logic ---
        bool humanOpponentHasPuck = (PlayerPuckHandler.Instance != null && PlayerPuckHandler.Instance.hasPuck && this.botTeam != playerTeam);

        // If human opponent has the puck, check if we should hesitate
        if (humanOpponentHasPuck)
        {
            if (hesitationTimer > 0)
            {
                hesitationTimer -= Time.deltaTime;
                Patroling(); // Hesitate by patrolling randomly
                return;      // Skip the rest of the logic for this frame
            }
        }
        else
        {
            // If player doesn't have puck, reset our hesitation timer so we're ready for their next pickup
            hesitationTimer = playerPickupHesitation;
        }

        // Check if an AI opponent has the puck
        EnemyAiTutorial aiOpponentWithPuck = GetEnemyWithPuck();

        Transform puckCarrier = null;
        if (aiOpponentWithPuck != null) puckCarrier = aiOpponentWithPuck.transform;
        else if (humanOpponentHasPuck) puckCarrier = PlayerPuckHandler.Instance.transform;

        if (puckCarrier != null)
        {
            // An opponent has the puck. Decide whether to chase them or patrol.
            randomMoveTimer += Time.deltaTime;
            if (randomMoveTimer >= randomMoveInterval)
            {
                isRandomChasing = Random.value > 0.5f;
                randomMoveTimer = 0f;
                randomMoveInterval = Random.Range(2f, 5f);
            }

            if (isRandomChasing)
            {
                agent.SetDestination(puckCarrier.position);
            }
            else
            {
                Patroling();
            }
        }
        else
        {
            // --- Loose Puck Logic ---
            // No one has the puck, so everyone should chase it.
            ChasePuck();
        }
    }

    private bool TryPass()
    {
        if (IsEnemyTooClose()) return false;

        Transform bestTarget = null;
        float bestScore = Vector3.Distance(transform.position, goal.position);

        // 1. Evaluate AI teammates
        foreach (var bot in passTargets)
        {
            if (bot == this || bot.botTeam != this.botTeam) continue;
            float botScore = Vector3.Distance(bot.transform.position, goal.position);
            if (botScore < bestScore)
            {
                bestScore = botScore;
                bestTarget = bot.transform;
            }
        }

        // 2. Evaluate human player
        if (this.botTeam == playerTeam && humanPlayerTransform != null)
        {
            float playerScore = Vector3.Distance(humanPlayerTransform.position, goal.position);
            if (playerScore < bestScore)
            {
                bestScore = playerScore;
                bestTarget = humanPlayerTransform;
            }
        }

        // 3. If a better target was found, pass to them
        if (bestTarget != null)
        {
            PassPuckTo(bestTarget);
            return true;
        }

        return false;
    }

    private void GetOpenPosition()
    {
        float offensiveZoneStart = (minBounds.z + maxBounds.z) / 2f;
        float randomZ = Random.Range(offensiveZoneStart, maxBounds.z);
        float randomX = Random.Range(minBounds.x, maxBounds.x);
        Vector3 openPos = new Vector3(randomX, transform.position.y, randomZ);
        agent.SetDestination(ClampToBounds(openPos));
    }

    private void TryShootAtGoal()
    {
        if (alreadyAttacked) return;
        Rigidbody puckRb = player.GetComponent<Rigidbody>();
        if (puckRb == null) return;

        Vector3 perfectDirection = (goal.position - player.position).normalized;
        Vector3 finalDirection = perfectDirection;

        if (Random.Range(0f, 100f) > shotAccuracyPercentage)
        {
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-shotInaccuracyAngle, shotInaccuracyAngle), 0);
            finalDirection = randomRotation * perfectDirection;
        }

        puckRb.AddForce(finalDirection * meleeForce, ForceMode.Impulse);
        PostAttackCooldown();
        if (animController != null) animController.TriggerRandomHit();
    }

    private void PassPuckTo(Transform target)
    {
        Rigidbody puckRb = player.GetComponent<Rigidbody>();
        if (puckRb == null) return;

        Vector3 direction = (target.position - player.position).normalized;
        puckRb.AddForce(direction * meleeForce, ForceMode.Impulse);
        PostAttackCooldown();
        if (animController != null) animController.TriggerRandomHit();
    }

    private void PostAttackCooldown()
    {
        alreadyAttacked = true;
        puckStopTimer = puckStopCooldown;
        Invoke(nameof(ResetAttack), timeBetweenAttacks);
    }

    private void ChasePuck()
    {
        Vector3 puckPosition = player.position;
        Vector3 goalDirection = (goal.position - puckPosition).normalized;
        Vector3 targetPosition = puckPosition - goalDirection * pushOffset;
        agent.SetDestination(ClampToBounds(targetPosition));
    }

    private void Patroling()
    {
        if (!walkPointSet || Vector3.Distance(transform.position, walkPoint) < 1f)
        {
            float randomZ = Random.Range(-walkPointRange, walkPointRange);
            float randomX = Random.Range(-walkPointRange, walkPointRange);
            walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);
            if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround)) { walkPointSet = true; }
        }
        if (walkPointSet) agent.SetDestination(ClampToBounds(walkPoint));
    }

    // --- Helper Methods ---
    private bool IsPuckClose() => Vector3.Distance(transform.position, player.position) < meleeRange;
    private bool IsEnemyTooClose(float radius = 4f) => allBots.Any(bot => bot != this && bot.botTeam != botTeam && Vector3.Distance(transform.position, bot.transform.position) < radius);
    private void ResetAttack() => alreadyAttacked = false;
    private Vector3 ClampToBounds(Vector3 pos) => new Vector3(Mathf.Clamp(pos.x, minBounds.x, maxBounds.x), pos.y, Mathf.Clamp(pos.z, minBounds.z, maxBounds.z));
    private static bool AnyBotHasPuck() => allBots.Any(bot => bot.hasPuck);
    private EnemyAiTutorial GetTeammateWithPuck() => allBots.FirstOrDefault(bot => bot != this && bot.botTeam == botTeam && bot.hasPuck);
    private EnemyAiTutorial GetEnemyWithPuck() => allBots.FirstOrDefault(bot => bot.botTeam != botTeam && bot.hasPuck);

    // --- Gizmos ---
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);
    }
}



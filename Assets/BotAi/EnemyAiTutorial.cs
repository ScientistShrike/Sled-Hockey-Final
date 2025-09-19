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

    // Patroling
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange = 10f;

    // Attacking
    public float timeBetweenAttacks = 2f;
    bool alreadyAttacked;
    public GameObject projectile;
    public float meleeRange = 2f;
    public float meleeForce = 20f;

    // States
    public float sightRange = 10f, attackRange = 3f;
    public bool playerInSightRange, playerInAttackRange;

    // AI movement zone restriction
    public Vector3 minBounds = new Vector3(-10, 0, -10);
    public Vector3 maxBounds = new Vector3(10, 0, 10);

    // Puck pushing logic
    public float pushOffset = 1.5f;

    public bool hasPuck = false;

    public static List<EnemyAiTutorial> allBots = new List<EnemyAiTutorial>();
    public List<EnemyAiTutorial> passTargets = new List<EnemyAiTutorial>(); // Only pass to these bots

    public float holdPuckTime = 1.5f; // How long to hold puck before action
    private float holdTimer = 0f;

    public float puckStopCooldown = 1.0f; // seconds after shot/pass before puck can be stopped again
    private float puckStopTimer = 0f;

    public enum Team { TeamA, TeamB }
    public Team botTeam;

    private void Awake()
    {
        player = GameObject.Find("hockey_puck").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        allBots.Add(this);
    }

    private void OnDisable()
    {
        allBots.Remove(this);
    }

    // Random movement logic
    private float randomMoveTimer = 0f;
    private float randomMoveInterval = 3f; // How often to change behavior
    private bool isRandomChasing = false;

    private void Update()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Update cooldown timer
        if (puckStopTimer > 0f)
            puckStopTimer -= Time.deltaTime;

        // Check if this bot has the puck
        hasPuck = IsPuckClose();

        // If bot has puck, stop and shoot/pass before moving again
        if (hasPuck)
        {
            // Freeze movement
            agent.SetDestination(transform.position);

            // Only stop the puck if cooldown expired
            if (puckStopTimer <= 0f)
            {
                Rigidbody puckRb = player.GetComponent<Rigidbody>();
                if (puckRb != null)
                {
                    puckRb.linearVelocity = Vector3.zero;
                    puckRb.angularVelocity = Vector3.zero;
                }
            }

            holdTimer += Time.deltaTime;

            // Only allow shooting/passing after hold time
            if (holdTimer >= holdPuckTime && !alreadyAttacked)
            {
                TryPassToBetterBot();
                TryShootAtGoal();
                holdTimer = 0f; // Reset after action
            }

            // Return early so no other movement logic runs
            return;
        }
        else
        {
            holdTimer = 0f; // Reset if puck lost
        }

        // Check if any bot has the puck
        bool puckIsOwned = AnyBotHasPuck();
        EnemyAiTutorial teammateWithPuck = GetTeammateWithPuck();

        if (puckIsOwned)
        {
            // If a teammate has the puck, keep distance from puck
            if (teammateWithPuck != null)
            {
                float minTeammateDistance = 4f; // Minimum distance to keep from puck
                float distToPuck = Vector3.Distance(transform.position, player.position);
                if (distToPuck < minTeammateDistance)
                {
                    // Move away from puck
                    Vector3 awayDir = (transform.position - player.position).normalized;
                    Vector3 targetPos = transform.position + awayDir * minTeammateDistance;
                    targetPos = ClampToBounds(targetPos);
                    agent.SetDestination(targetPos);
                }
                else
                {
                    Patroling();
                }
            }
            else
            {
                // Randomize movement every interval
                randomMoveTimer += Time.deltaTime;
                if (randomMoveTimer >= randomMoveInterval)
                {
                    // 50% chance to chase puck, 50% to patrol
                    isRandomChasing = Random.value > 0.5f;
                    randomMoveTimer = 0f;
                    randomMoveInterval = Random.Range(2f, 5f);
                }

                if (isRandomChasing)
                    ChasePlayer();
                else
                    Patroling();
            }
        }
        else
        {
            // If no one has the puck, always chase it
            ChasePlayer();
        }

        // Remove or comment out the old state logic below if you want only random behavior:
        /*
        if (!playerInSightRange && !playerInAttackRange)
            Patroling();
        else if (playerInSightRange && !playerInAttackRange)
            ChasePlayer();
        else if (playerInAttackRange && playerInSightRange)
        {
            if (distanceToPlayer <= meleeRange)
                MeleeAttack();
            else
                AttackPlayer();
        }
        */
    }

    // Helper to check if puck is close enough to be considered "owned"
    private bool IsPuckClose()
    {
        return Vector3.Distance(transform.position, player.position) < meleeRange;
    }

    private bool IsEnemyTooClose(float dangerRadius = 4f)
    {
        foreach (var bot in allBots)
        {
            if (bot == this) continue;
            if (bot.botTeam != this.botTeam)
            {
                float dist = Vector3.Distance(transform.position, bot.transform.position);
                if (dist < dangerRadius)
                    return true;
            }
        }
        return false;
    }

    // Find the best bot to pass to and pass the puck
    private void TryPassToBetterBot()
    {
        // If enemy is too close, don't pass, try to get closer to goal
        if (IsEnemyTooClose())
        {
            // Move toward the goal instead of passing
            Vector3 moveTarget = goal.position;
            moveTarget.y = transform.position.y;
            agent.SetDestination(moveTarget);
            return;
        }

        EnemyAiTutorial bestBot = null;
        float bestScore = float.MaxValue;

        foreach (var bot in passTargets) // Only consider allowed targets
        {
            if (bot == this) continue;
            if (bot.botTeam != this.botTeam) continue; // Only pass to allies

            // Score: distance to goal minus distance to puck (lower is better)
            float score = Vector3.Distance(bot.transform.position, goal.position)
                        - Vector3.Distance(bot.transform.position, player.position);

            // Optional: Add more logic (e.g., line of sight, not obstructed)
            if (score < bestScore)
            {
                bestScore = score;
                bestBot = bot;
            }
        }

        // If another bot is better positioned, pass the puck
        if (bestBot != null && bestScore < Vector3.Distance(transform.position, goal.position))
        {
            PassPuckToBot(bestBot);
        }
    }

    // Pass the puck by applying force toward the target bot
    private void PassPuckToBot(EnemyAiTutorial targetBot)
    {
        Rigidbody puckRb = player.GetComponent<Rigidbody>();
        if (puckRb != null)
        {
            Vector3 direction = (targetBot.transform.position - player.position).normalized;
            puckRb.AddForce(direction * meleeForce, ForceMode.Impulse);
            alreadyAttacked = true;
            puckStopTimer = puckStopCooldown; // Start cooldown
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void Patroling()
    {
        if (!walkPointSet)
            SearchWalkPoint();

        if (walkPointSet)
        {
            Vector3 clamped = ClampToBounds(walkPoint);
            agent.SetDestination(clamped);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
            walkPointSet = true;
    }

    private void ChasePlayer()
    {
        Vector3 puckPosition = player.position;
        Vector3 goalDirection = (goal.position - puckPosition).normalized;

        Vector3 targetPosition = puckPosition - goalDirection * pushOffset;
        targetPosition.y = transform.position.y;

        Vector3 clamped = ClampToBounds(targetPosition);
        agent.SetDestination(clamped);
    }

    private void AttackPlayer()
    {
        agent.SetDestination(transform.position);
        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            Rigidbody rb = Instantiate(projectile, transform.position, Quaternion.identity).GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
            rb.AddForce(transform.up * 2f, ForceMode.Impulse);

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void MeleeAttack()
    {
        agent.SetDestination(transform.position);
        transform.LookAt(player);

        // Only hit the puck if NOT holding it (not the owner)
        if (!alreadyAttacked && !hasPuck)
        {
            Rigidbody puckRb = player.GetComponent<Rigidbody>();
            if (puckRb != null)
            {
                Vector3 direction = (player.position - transform.position).normalized;
                puckRb.AddForce(direction * meleeForce, ForceMode.Impulse);
            }

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minBounds.x, maxBounds.x),
            position.y,
            Mathf.Clamp(position.z, minBounds.z, maxBounds.z)
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);
    }

    // Add this method to shoot at the goal if no pass is made
    private void TryShootAtGoal()
    {
        if (!alreadyAttacked && hasPuck)
        {
            Rigidbody puckRb = player.GetComponent<Rigidbody>();
            if (puckRb != null)
            {
                // Shoot directly toward the goal
                Vector3 direction = (goal.position - player.position).normalized;
                puckRb.AddForce(direction * meleeForce, ForceMode.Impulse);

                alreadyAttacked = true;
                puckStopTimer = puckStopCooldown; // Start cooldown
                Invoke(nameof(ResetAttack), timeBetweenAttacks);
            }
        }
    }

    private static bool AnyBotHasPuck()
    {
        return allBots.Any(bot => bot.hasPuck);
    }

    private EnemyAiTutorial GetTeammateWithPuck()
    {
        return allBots.FirstOrDefault(bot => bot != this && bot.botTeam == this.botTeam && bot.hasPuck);
    }
}



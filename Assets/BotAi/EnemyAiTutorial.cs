using System.Linq; // Add at the top for LINQ
using System.Collections.Generic; // Add this line
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
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

    private AudioSource audioSource;

    [Header("Audio (GameObject with AudioSource Prefered)")]
    public GameObject hitSoundObject;
    public GameObject tiredSoundObject;
    public GameObject sadSoundObject;
    public GameObject cheerSoundObject;
    public GameObject movingSoundObject;

    private Coroutine playingAudioRoutine;

    // Stop all local audio sources for this character to avoid overlapping sounds.
    private void StopAllLocalAudio()
    {
        // Stop coroutine-driven audio
        if (playingAudioRoutine != null)
        {
            StopCoroutine(playingAudioRoutine);
            playingAudioRoutine = null;
        }

        // Stop the central audio source
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.loop = false;
            audioSource.Stop();
        }

        // Stop any assigned per-sound GameObject AudioSources
        StopAudioObject(cheerSoundObject);
        StopAudioObject(sadSoundObject);
        StopAudioObject(tiredSoundObject);
        StopAudioObject(hitSoundObject);
        StopAudioObject(movingSoundObject);
    }

    private void StopAudioObject(GameObject audioObject)
    {
        if (audioObject == null) return;
        var src = audioObject.GetComponentInChildren<AudioSource>();
        if (src != null && src.isPlaying)
        {
            src.loop = false;
            src.Stop();
        }
    }

    private System.Collections.IEnumerator PlayAndStop(AudioSource src, float duration, bool loop = false)
    {
        if (src == null) yield break;

        src.loop = loop;
        src.Play();

        if (float.IsInfinity(duration) || duration <= 0f)
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(duration);

        if (!loop && src.isPlaying) {
            src.Stop();
        }
    }

    private void Awake()
    {
        // Find the puck by name; provide fallbacks and null protection.
        GameObject puckObj = GameObject.Find("hockey_puck");
        if (puckObj == null)
        {
            puckObj = GameObject.Find("hockey_pck"); // try the old name if present
        }
        if (puckObj == null)
        {
            // Try common tags if the object uses them.
            puckObj = GameObject.FindWithTag("Puck");
            if (puckObj == null) puckObj = GameObject.FindWithTag("Player");
        }
        if (puckObj != null) player = puckObj.transform; else Debug.LogError($"[EnemyAiTutorial] Awake: Puck not found (tried 'hockey_puck','hockey_pck',tag:'Puck','Player'). AI relying on puck will be disabled until puck appears.");
        
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"[EnemyAiTutorial] Awake: NavMeshAgent not found on {gameObject.name}; disabling AI.");
            this.enabled = false;
            return;
        }

        // Get or add the central AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        staminaTimer = Random.Range(minMoveTime, maxMoveTime);
        hesitationTimer = playerPickupHesitation;

        if (humanPlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) humanPlayerTransform = playerObj.transform;
        }

        if (animController == null) animController = GetComponentInChildren<AnimationController>();
        if (animController == null)
        {
            Debug.LogWarning($"[EnemyAiTutorial] Awake: AnimationController not found in children for {gameObject.name}. Animations (tired, hits, cheer) will not play unless an AnimationController is attached as child.");
        }
        // Log missing per-bot audio objects so the developer can assign them in the prefab.
        if (hitSoundObject == null) Debug.LogWarning($"[EnemyAiTutorial] Awake: hitSoundObject not configured on '{gameObject.name}'.");
        if (tiredSoundObject == null) Debug.LogWarning($"[EnemyAiTutorial] Awake: tiredSoundObject not configured on '{gameObject.name}'.");
        if (sadSoundObject == null) Debug.LogWarning($"[EnemyAiTutorial] Awake: sadSoundObject not configured on '{gameObject.name}'. Will attempt fallback to global SoundEffects if available.");
        if (cheerSoundObject == null) Debug.LogWarning($"[EnemyAiTutorial] Awake: cheerSoundObject not configured on '{gameObject.name}'. Will attempt fallback to global SoundEffects if available.");
    }

    private void OnEnable() => allBots.Add(this);
    private void OnDisable()
    {
        allBots.Remove(this);
        // Stop any coroutines and playing audio when the bot is disabled
        if (playingAudioRoutine != null)
        {
            StopCoroutine(playingAudioRoutine);
            playingAudioRoutine = null;
        }
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private float randomMoveTimer = 0f;
    private float randomMoveInterval = 3f;
    private bool isRandomChasing = false;

    private void Update()
    {
        HandleStamina();
        // Resolve moving sound audio source (prefers assigned external GameObject, falls back to local audioSource).
        AudioSource resolvedMovingSrc = ResolveAudioSource(movingSoundObject) ?? audioSource;
        if (currentState == AiState.Resting)
        {
            // Stop movement sound when resting
            if (resolvedMovingSrc != null && resolvedMovingSrc.isPlaying)
            {
                resolvedMovingSrc.Stop();
            }
            return;
        }

        if (puckStopTimer > 0f) puckStopTimer -= Time.deltaTime;

        // If we couldn't find a puck, avoid calling into it and keep patrolling.
        if (player == null)
        {
            Debug.LogWarning($"[EnemyAiTutorial] Update: No puck Transform found for {gameObject.name}; patrolling without puck.");
            hasPuck = false;
            Patroling();
            return;
        }

        hasPuck = IsPuckClose();

        if (animController != null)
        {
            animController.SetPushing(agent.velocity.magnitude > 0.1f);
        }

        // Handle movement sound
        // movingSoundObject could be an external GameObject; resolve an AudioSource to play on (use previously resolvedMovingSrc)
        resolvedMovingSrc = ResolveAudioSource(movingSoundObject) ?? audioSource;
        if (resolvedMovingSrc != null)
        {
            if (agent.velocity.magnitude > 0.1f && !(resolvedMovingSrc != null && resolvedMovingSrc.isPlaying))
            {
                if (!resolvedMovingSrc.isPlaying)
                {
                    resolvedMovingSrc.loop = true;
                    resolvedMovingSrc.Play();
                }
            }
            else if (agent.velocity.magnitude <= 0.1f && resolvedMovingSrc != null && resolvedMovingSrc.isPlaying)
            {
                if (resolvedMovingSrc.isPlaying) resolvedMovingSrc.Stop();
            }
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
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} is now Resting; agent.isStopped={agent.isStopped}");
            StartTiredSound();
            // Ensure animator transitions to resting/tired state by toggling tired only
            if (animController != null)
            {
                animController.SetPushing(false);
                animController.SetIdle(false);
                animController.SetTired(true);
            }
        }
        else if (currentState == AiState.Resting && staminaTimer <= 0f)
        {
            currentState = AiState.Moving;
            staminaTimer = Random.Range(minMoveTime, maxMoveTime);
            agent.isStopped = false;
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} is now Moving; agent.isStopped={agent.isStopped}");
            StopTiredSound();
            if (animController != null)
            {
                animController.SetTired(false);
            }
        }

        if (prev != currentState)
        {
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} state change {prev} -> {currentState} (staminaTimer={staminaTimer})");
            if (animController != null)
                animController.SetTired(currentState == AiState.Resting);
        }
    }

    private void HandlePuckPossession()
    {
        if (player == null) { Debug.LogWarning($"[EnemyAiTutorial] HandlePuckPossession: player (puck) is null for {gameObject.name}. Cancelling possession handling."); return; }
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
        if (player == null) { Debug.LogWarning($"[EnemyAiTutorial] TryShootAtGoal: player (puck) is null for {gameObject.name}; cannot shoot."); return; }
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
        PlayHitSound();
        PostAttackCooldown();
        if (animController != null) animController.TriggerRandomHit();
    }

    private void PassPuckTo(Transform target)
    {
        if (player == null) { Debug.LogWarning($"[EnemyAiTutorial] PassPuckTo: player (puck) is null for {gameObject.name}; cannot pass."); return; }
        Rigidbody puckRb = player.GetComponent<Rigidbody>();
        if (puckRb == null) return;

        Vector3 direction = (target.position - player.position).normalized;
        puckRb.AddForce(direction * meleeForce, ForceMode.Impulse);
        PlayHitSound();
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
        if (player == null) { Patroling(); return; }
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
    private bool IsPuckClose() => (player != null) && Vector3.Distance(transform.position, player.position) < meleeRange;
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

    public void PlayCheerSound(float duration = -1f)
    {
        // Ensure any currently playing local sounds on this character stop before starting the cheer.
        StopAllLocalAudio();
        // Prefer per-bot cheer Audio GameObject if available, otherwise fall back to the global SoundEffects singleton.
        AudioSource cheerSrc = ResolveAudioSource(cheerSoundObject) ?? audioSource;
        if (cheerSrc != null)
        {
            if (playingAudioRoutine != null) StopCoroutine(playingAudioRoutine);
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.victoryDuration;
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} PlayCheerSound using local AudioSource for duration={dur}");
            playingAudioRoutine = StartCoroutine(PlayAndStop(cheerSrc, dur, loop: false));
            return;
        }

        // Fallback to global SoundEffects singleton if present
        if (global::SoundEffects.Instance != null)
        {
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.victoryDuration;
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} PlayCheerSound falling back to SoundEffects.Instance for duration={dur}");
            global::SoundEffects.Instance.PlayCheerSound(dur);
            return;
        }

        Debug.LogWarning($"[EnemyAiTutorial] PlayCheerSound: No cheerSound configured and global SoundEffects.Instance not present on '{gameObject.name}'.");
    }

    public void StartTiredSound()
    {
        // Ensure other sounds stop before starting the looping tired audio for this character.
        StopAllLocalAudio();
        AudioSource tiredSrc = ResolveAudioSource(tiredSoundObject) ?? audioSource;
        if (tiredSrc != null && !tiredSrc.isPlaying)
        {
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} StartTiredSound: Playing tired sound (local AudioSource)");
            tiredSrc.loop = true;
            tiredSrc.Play();
        }
    }

    public void StopTiredSound()
    {
        AudioSource tiredSrc2 = ResolveAudioSource(tiredSoundObject) ?? audioSource;
        if (tiredSrc2 != null && tiredSrc2.isPlaying)
        {
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} StopTiredSound: Stopped tired sound (local AudioSource)");
            tiredSrc2.loop = false;
            tiredSrc2.Stop();
        }
    }

    public void PlaySadSound(float duration = -1f)
    {
        // Ensure any currently playing local sounds on this character stop before starting the sad sound.
        StopAllLocalAudio();
        // 1. Prefer the bot's own "sad" clip
        AudioSource sadSrc = ResolveAudioSource(sadSoundObject) ?? ResolveAudioSource(tiredSoundObject) ?? audioSource;
        if (sadSrc != null)
        {
            if (playingAudioRoutine != null) StopCoroutine(playingAudioRoutine);
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.lossDuration;
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} PlaySadSound using local AudioSource for duration={dur}");
            playingAudioRoutine = StartCoroutine(PlayAndStop(sadSrc, dur, loop: false));
            return;
        }

        // 2. If no sad clip, fall back to the bot's "tired" clip
        // 2. If no sad clip, fall back to the bot's "tired" audio object
        if (tiredSoundObject != null)
        {
            if (playingAudioRoutine != null) StopCoroutine(playingAudioRoutine);
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.lossDuration;
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} PlaySadSound falling back to local tired AudioSource for duration={dur}");
            AudioSource fallbackTiredSrc = ResolveAudioSource(tiredSoundObject) ?? audioSource;
            playingAudioRoutine = StartCoroutine(PlayAndStop(fallbackTiredSrc, dur, loop: false));
            return;
        }

        // 3. Finally, fallback to the global SoundEffects singleton
        if (global::SoundEffects.Instance != null)
        {
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.lossDuration;
            Debug.Log($"[EnemyAiTutorial] {gameObject.name} PlaySadSound falling back to SoundEffects.Instance for duration={dur}");
            global::SoundEffects.Instance.PlaySadSound(dur);
            return;
        }

        Debug.LogWarning($"[EnemyAiTutorial] PlaySadSound: No sadSound, tiredSound, or global SoundEffects instance configured on '{gameObject.name}'.");
    }

    public void PlayHitSound(float duration = -1f)
    {
        // Stop other local audio to avoid overlapping with the hit sound.
        StopAllLocalAudio();
        AudioSource hitSrc = ResolveAudioSource(hitSoundObject) ?? audioSource;
        if (hitSrc == null) return;
        if (playingAudioRoutine != null) StopCoroutine(playingAudioRoutine);
        float dur = duration;
        if (dur <= 0f && animController != null) dur = animController.hitDuration;
        Debug.Log($"[EnemyAiTutorial] {gameObject.name} PlayHitSound: Playing hit sound for duration={dur}");
        playingAudioRoutine = StartCoroutine(PlayAndStop(hitSrc, dur, loop: false));
    }

    private AudioSource ResolveAudioSource(GameObject audioObject)
    {
        if (audioObject == null) return null;
        var src = audioObject.GetComponentInChildren<AudioSource>();
        if (src == null)
        {
            Debug.LogWarning($"[EnemyAiTutorial] ResolveAudioSource: GameObject '{audioObject.name}' has no AudioSource component.");
        }
        return src;
    }
}



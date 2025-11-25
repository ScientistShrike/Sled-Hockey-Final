using System.Collections;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;
    // Internal state flags used to determine the `idle` parameter
    private bool isBusy = false;    // true when AI is performing some action
    private bool isPushing = false; // mirrors the pushing param

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on " + gameObject.name);
        }
        else
        {
            Debug.Log("Animator found on " + animator.gameObject.name);
        }
    }
            // Public durations so other systems can synchronize audio playback to animation lengths.
            [Header("Animation Durations (seconds)")]
            public float hitDuration = 0.8f;
            public float victoryDuration = 2.0f;
            public float lossDuration = 2.0f;

    // ----- MOVEMENT / IDLE -----
    public void SetPushing(bool value)
    {
        if (animator == null) return;
        isPushing = value;
        animator.SetBool("pushing", value);
        UpdateIdleState();
    }

    public void SetIdle(bool value)
    {
        if (animator == null) return;
        animator.SetBool("idle", value);
    }

    // Sets a general "busy" flag. When busy, the idle parameter will be false.
    public void SetBusy(bool busy)
    {
        if (animator == null) return;
        isBusy = busy;
        UpdateIdleState();
    }

    // Decide whether the character should be considered idle.
    // Idle is true only when not pushing and not busy (i.e. simply standing around).
    private void UpdateIdleState()
    {
        if (animator == null) return;
        bool idle = !(isPushing || isBusy);
        animator.SetBool("idle", idle);
    }

    public void SetTired(bool value)
    {
        if (animator == null) return;
        animator.SetBool("tired", value);
        // Keep SetTired simple to avoid conflicting inputs; idle is updated by SetPushing/SetBusy.
    }

    // ----- HITTING -----
    public void TriggerRandomHit()
    {
        if (animator == null) return;

        // 50/50 chance
        bool left = Random.value < 0.5f;

        // Mark briefly busy while the hit animation plays
        StartCoroutine(TemporaryBusy(hitDuration));

        if (left)
            animator.SetTrigger("hittingleft");
        else
            animator.SetTrigger("hitting");
    }

    // ----- SCORING EVENTS -----
    public void PlayVictory()
    {
        if (animator == null) return;
        // Mark busy for the duration of the victory animation
        StartCoroutine(TemporaryBusy(victoryDuration));
        animator.SetTrigger("Victory");
        // Play cheer sound if an AI or player-specific audio component is present
        var enemyAI = GetComponentInParent<EnemyAiTutorial>();
        if (enemyAI != null)
        {
            Debug.Log($"[AnimationController] Found EnemyAiTutorial on '{enemyAI.gameObject.name}', playing cheer for '{enemyAI.gameObject.name}'");
            // Pass the animation duration so audio syncs with the animation
            enemyAI.PlayCheerSound(victoryDuration);
            return;
        }

        var playerAudio = GetComponentInParent<PlayerAudio>();
        if (playerAudio != null)
        {
            Debug.Log($"[AnimationController] Found PlayerAudio on '{playerAudio.gameObject.name}', playing cheer for player");
            playerAudio.PlayCheerSound(victoryDuration);
            return;
        }
    }

    public void PlayLoss()
    {
        if (animator == null) return;
        // Mark busy for the duration of the loss animation
        StartCoroutine(TemporaryBusy(lossDuration));
        animator.SetTrigger("Loss");
        // Play sad sound if an AI or player-specific audio component is present
        var enemyAI = GetComponentInParent<EnemyAiTutorial>();
        if (enemyAI != null)
        {
            Debug.Log($"[AnimationController] Found EnemyAiTutorial on '{enemyAI.gameObject.name}', playing sad for '{enemyAI.gameObject.name}'");
            // Pass the animation duration so audio syncs with the animation
            enemyAI.PlaySadSound(lossDuration);
            return;
        }

        var playerAudio = GetComponentInParent<PlayerAudio>();
        if (playerAudio != null)
        {
            Debug.Log($"[AnimationController] Found PlayerAudio on '{playerAudio.gameObject.name}', playing sad for player");
            playerAudio.PlaySadSound(lossDuration);
            return;
        }
    }

    // Temporarily mark the controller as busy for a short duration.
    private System.Collections.IEnumerator TemporaryBusy(float seconds)
    {
        SetBusy(true);
        yield return new WaitForSeconds(seconds);
        SetBusy(false);
    }

    // ----- GAME END SEQUENCE -----
    public void OnMatchEnded(int aiTeamScore, int enemyTeamScore)
    {
        if (aiTeamScore > enemyTeamScore)
            PlayVictory();
        else
            PlayLoss();
    }

    // ----- GOAL SCORE EVENT -----
    // Called when someone scores mid-game
    public void OnGoalScored(bool aiTeamScored)
    {
        if (aiTeamScored)
            PlayVictory();
        else
            PlayLoss();
    }
}

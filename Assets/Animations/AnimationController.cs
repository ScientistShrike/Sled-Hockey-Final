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
    }

    // ----- HITTING -----
    public void TriggerRandomHit()
    {
        if (animator == null) return;

        // 50/50 chance
        bool left = Random.value < 0.5f;

        // Mark briefly busy while the hit animation plays
        StartCoroutine(TemporaryBusy(0.8f));

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
        StartCoroutine(TemporaryBusy(2.0f));
        animator.SetTrigger("Victory");
    }

    public void PlayLoss()
    {
        if (animator == null) return;
        // Mark busy for the duration of the loss animation
        StartCoroutine(TemporaryBusy(2.0f));
        animator.SetTrigger("Loss");
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

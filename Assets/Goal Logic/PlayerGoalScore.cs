using UnityEngine;

public class PlayerGoalScore : MonoBehaviour
{
    public string scorer = "player";
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("hockey_puck"))
        {
            // Check if the ScoreManager exists and if a reset is not already in progress
            if (ScoreManager.sManager != null && !ScoreManager.sManager.IsResetting)
            {
                ScoreManager.sManager.IncreaseScore(scorer);
            }
        }
    }
}

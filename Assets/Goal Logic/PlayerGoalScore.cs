using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerGoalScore : MonoBehaviour
{
    public string scorer = "player";
    [Tooltip("If true, this goal counts towards the tutorial 4-goal unlock. If false, tutorial detection falls back to scene name or GameSession flags.")]
    public bool countForTutorial = false;
    private const string TutorialGoalsKey = "TutorialGoals";
    private const string DuckPuckUnlockedKey = "DuckPuckUnlocked";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("hockey_puck"))
        {
            if (ScoreManager.sManager != null && !ScoreManager.sManager.IsResetting)
            {
                ScoreManager.sManager.IncreaseScore(scorer);

                bool isInTutorialContext = false;
                // Explicit inspector override for tutorial goals
                if (countForTutorial)
                {
                    isInTutorialContext = true;
                }
                // Old-style scene name check (keeps compatibility with earlier project setup)
                else if (SceneManager.GetActiveScene().name == "Tutorial")
                {
                    isInTutorialContext = true;
                }
                // If the NewTutorialPanelController indicates the tutorial is active, count goals as part of the tutorial.
                else if (NewTutorialPanelController.isTutorialActive)
                {
                    isInTutorialContext = true;
                }

                if (scorer == "player" && isInTutorialContext)
                {
                    int tutorialGoals = PlayerPrefs.GetInt(TutorialGoalsKey, 0);
                    tutorialGoals++;
                    PlayerPrefs.SetInt(TutorialGoalsKey, tutorialGoals);
                    PlayerPrefs.Save();
                    Debug.Log("Tutorial goals updated: " + tutorialGoals);

                    if (tutorialGoals >= 4)
                    {
                        if (PlayerPrefs.GetInt(DuckPuckUnlockedKey, 0) != 1)
                        {
                            PlayerPrefs.SetInt(DuckPuckUnlockedKey, 1);
                            PlayerPrefs.Save();
                            Debug.Log("Duck puck unlocked via tutorial progress!\nTotal tutorial goals: " + tutorialGoals);
                        }
                    }
                }
            }
        }
    }
}

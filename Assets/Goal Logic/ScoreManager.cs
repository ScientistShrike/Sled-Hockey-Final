using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum GameResult { Win, Lose, Draw }

[System.Serializable]
public class BotResetInfo
{
    public GameObject botSled;
    public Transform spawnPoint;
}

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager sManager;
    public int playerScore = 0;
    public int botScore = 0;
    public int scoreToWin = 5;

    [Header("UI")]
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI botScoreText;

    [Header("Timer")]
    [SerializeField]
    private float _timeRemaining = 120;
    public float timeRemaining 
    { 
        get { return _timeRemaining; } 
        set { _timeRemaining = value; } 
    }
    public bool timerIsRunning = false;
    public TextMeshProUGUI timeText;

    [Header("Game Over")]
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject drawPanel;

    [Header("Game Reset")]
    public GameObject puck;
    public Transform playerSpawnPoint;
    public float resetCooldown = 3.0f;

    [Header("Goal Celebration")]
    public GoalCelebration goalCelebration;

    private bool isGameOver = false;
    private bool isResetting = false;

    public bool IsResetting => isResetting;

    void Start()
    {
        sManager = this;

        // If the session is NOT initialized, it means this is a brand new game.
        // We need to reset all session values to their defaults.
        if (!GameSession.IsInitialized)
        {
            GameSession.PlayerScore = 0;
            GameSession.BotScore = 0;
            // Use the inspector-assigned value instead of a hard-coded constant
            GameSession.TimeRemaining = timeRemaining; // Preserve inspector value
            GameSession.IsInitialized = true;
            GameSession.TutorialHasBeenShown = false; // Reset tutorial flag for new game
        }
        else
        {
            // If the session IS initialized, it means we are reloading the scene after a goal.
            // The score and time should persist. We just need to reset player positions.
            if (PlayerSingleton.Instance != null && playerSpawnPoint != null) 
            {
                PlayerSingleton.Instance.ResetToSpawnPoint(playerSpawnPoint);
            }
        }

        // Regardless of whether it's a new game or a mid-game reload,
        // this instance of ScoreManager needs to sync its local variables with the session state.
        playerScore = GameSession.PlayerScore;
        botScore = GameSession.BotScore;
        
        // Only use persisted time if already initialized, otherwise use inspector value
        if (GameSession.IsInitialized)
        {
            timeRemaining = GameSession.TimeRemaining;
        }
        else
        {
            // First time - use the inspector value, then save it
            GameSession.TimeRemaining = timeRemaining;
        }

        timerIsRunning = true;
        isGameOver = false;
        isResetting = false;
        Time.timeScale = 1f;

        // Clear the global game-over flag at the start of a new or resumed session
        GameSession.IsGameOver = false;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);

        UpdateScoreDisplay();
    }

    void Update()
    {
        if (isGameOver)
        {
            return;
        }

        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                GameSession.TimeRemaining = timeRemaining; // Persist time
                DisplayTime(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                timerIsRunning = false;
                DisplayTime(timeRemaining);

                if (playerScore > botScore) EndGame(GameResult.Win);
                else if (botScore > playerScore) EndGame(GameResult.Lose);
                else EndGame(GameResult.Draw);
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timeText == null)
        {
            return;
        }

        // To prevent displaying a negative number
        if (timeToDisplay < 0)
        {
            timeToDisplay = 0;
        }

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void UpdateScoreDisplay()
    {
        if (playerScoreText != null)
        {
            playerScoreText.text = "Player: " + playerScore;
        }

        if (botScoreText != null)
        {
            botScoreText.text = "Bot: " + botScore;
        }
    }

    public void IncreaseScore(string scorer)
    {
        if (isGameOver || isResetting)
        {
            return;
        }

        if (scorer == "player")
        {
            playerScore++;
            GameSession.PlayerScore = playerScore;
        }
        else if (scorer == "bot")
        {
            botScore++;
            GameSession.BotScore = botScore;
        }
        UpdateScoreDisplay();

        // Trigger goal celebration
        if (goalCelebration != null)
        {
            goalCelebration.Play();
        }

        // Trigger victory/loss animations for teams immediately after a goal
        TriggerGoalAnimations(scorer);

        // CheckForWin now returns true if the game ended
        if (!CheckForWin())
        {
            StartCoroutine(ResetSceneAfterGoal());
        }
    }

    private IEnumerator ResetSceneAfterGoal()
    {
        isResetting = true;
        timerIsRunning = false; // Pause the main timer

        // --- FREEZE BOTS AND PUCK ---

        // Freeze all bots by disabling their AI and physics.
        // We iterate over a copy because disabling the bot's AI script modifies the original list.
        if (EnemyAiTutorial.allBots != null)
        {
            var botsToFreeze = new List<EnemyAiTutorial>(EnemyAiTutorial.allBots);
            foreach (var botAI in botsToFreeze)
            {
                if (botAI != null)
                {
                    // Disable the AI script to stop it from issuing commands
                    botAI.enabled = false;

                    // Disable NavMeshAgent to stop it from moving the transform
                    NavMeshAgent navAgent = botAI.GetComponent<NavMeshAgent>();
                    if (navAgent != null)
                        navAgent.enabled = false;

                    // Freeze Rigidbody to stop physics movement
                    Rigidbody botRb = botAI.GetComponent<Rigidbody>();
                    if (botRb != null)
                    {
                        botRb.linearVelocity = Vector3.zero;
                        botRb.angularVelocity = Vector3.zero;
                        botRb.isKinematic = true;
                    }
                }
            }
        }

        // Freeze the puck
        if (puck != null)
        {
            Rigidbody puckRb = puck.GetComponent<Rigidbody>();
            if (puckRb != null)
            {
                puckRb.linearVelocity = Vector3.zero;
                puckRb.angularVelocity = Vector3.zero;
                puckRb.isKinematic = true;
            }
        }

        // Wait for the cooldown period.
        yield return new WaitForSeconds(resetCooldown);

        // Reload the current scene to reset everything.
        // The GameSession will preserve the score and time.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    bool CheckForWin()
    {
        if (playerScore >= scoreToWin) {
            EndGame(GameResult.Win);
            return true;
        }
        if (botScore >= scoreToWin) {
            EndGame(GameResult.Lose);
            return true;
        }
        return false;
    }

    void EndGame(GameResult result)
    {
        Time.timeScale = 0f; // Ensure the game freezes immediately upon game over.
        isGameOver = true;
        timerIsRunning = false;

        // Reset the session so the next game starts fresh.
        GameSession.IsInitialized = false;
        GameSession.IsGameOver = true;
        GameSession.TutorialHasBeenShown = false; // Reset tutorial flag for new game
        
        GameObject panelToShow = null;
        switch(result)
        {
            case GameResult.Win:
                panelToShow = winPanel;
                break;
            case GameResult.Lose:
                panelToShow = losePanel;
                break;
            case GameResult.Draw:
                panelToShow = drawPanel;
                break;
        }

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }
            
            // Ensure XR/UI interactors are enabled so the player can click buttons while the game is paused.
            // Use the appropriate FindObjects API depending on Unity version to avoid deprecation warnings.
#if UNITY_2023_1_OR_NEWER
            var uiActivators = UnityEngine.Object.FindObjectsByType<UIRayActivator>(UnityEngine.FindObjectsSortMode.None);
#else
            var uiActivators = FindObjectsOfType<UIRayActivator>();
#endif
            if (uiActivators != null && uiActivators.Length > 0)
            {
                foreach (var a in uiActivators)
                {
                    if (a != null)
                        a.SetInteractorsActive(true);
                }
            }
            
            // If there is no EventSystem present, warn the developer.
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
            }

        // Prefer using the PauseManager's pause logic so Game Over behaves like Pause.
    #if UNITY_2023_1_OR_NEWER
        PauseManager pauseManager = UnityEngine.Object.FindFirstObjectByType<PauseManager>();
    #else
        PauseManager pauseManager = FindObjectOfType<PauseManager>();
    #endif
        if (pauseManager != null)
        {
            // Do not show the regular pause menu; just enable interactor and set timescale.
            pauseManager.SetPaused(true, false);

            // Also freeze actors so visuals stop regardless of unscaled updates.
            FreezeActorsForEndGame();
        }
        else
        {
            // Fallback: freeze actors and set timescale directly.
            FreezeActorsForEndGame();
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// Stop bots, puck physics and player movement so the scene visually halts on game over.
    /// Mirrors the freezing logic used after goals but does not reload the scene.
    /// </summary>
    private void FreezeActorsForEndGame()
    {
        // Disable AI and NavMeshAgents for all bots
        if (EnemyAiTutorial.allBots != null)
        {
            // Create a copy to avoid InvalidOperationException if bots unregister themselves
            // in OnDisable() during the iteration (disabling the script removes them).
            var botsToFreeze = new List<EnemyAiTutorial>(EnemyAiTutorial.allBots);
            foreach (var botAI in botsToFreeze)
            {
                if (botAI == null) continue;

                botAI.enabled = false;

                NavMeshAgent navAgent = botAI.GetComponent<NavMeshAgent>();
                if (navAgent != null)
                    navAgent.enabled = false;

                Rigidbody botRb = botAI.GetComponent<Rigidbody>();
                if (botRb != null)
                {
                    botRb.linearVelocity = Vector3.zero;
                    botRb.angularVelocity = Vector3.zero;
                    botRb.isKinematic = true;
                }
            }
        }

        // Freeze the puck
        if (puck != null)
        {
            Rigidbody puckRb = puck.GetComponent<Rigidbody>();
            if (puckRb != null)
            {
                puckRb.linearVelocity = Vector3.zero;
                puckRb.angularVelocity = Vector3.zero;
                puckRb.isKinematic = true;
            }
        }

        // Disable player movement script if present
        try
        {
            PlayerPuckHandler puckHandler = FindFirstObjectByType<PlayerPuckHandler>();
            if (puckHandler != null)
            {
                var movement = puckHandler.playerMovement;
                if (movement != null)
                    movement.enabled = false;
            }
        }
        catch
        {
            // Ignore if API not available in current Unity version
        }
    }

    private void TriggerGoalAnimations(string scorer)
    {
        // Determine which EnemyAiTutorial.Team corresponds to the human player at runtime.
        // Some scenes may set the human player to TeamB in the inspector, so don't assume TeamA.
        EnemyAiTutorial.Team humanTeam = EnemyAiTutorial.Team.TeamA;
        if (EnemyAiTutorial.allBots != null && EnemyAiTutorial.allBots.Count > 0)
        {
            // Use the first bot's configured playerTeam as the canonical human team assignment.
            var firstBot = EnemyAiTutorial.allBots[0];
            if (firstBot != null)
                humanTeam = firstBot.playerTeam;
        }

        // Map the scorer string to the correct scoring team using the detected human team.
        EnemyAiTutorial.Team scoringTeam;
        if (scorer == "player")
        {
            scoringTeam = humanTeam;
        }
        else
        {
            // If a bot scores, the scoring team is the opposite of the human team.
            scoringTeam = (humanTeam == EnemyAiTutorial.Team.TeamA) ? EnemyAiTutorial.Team.TeamB : EnemyAiTutorial.Team.TeamA;
        }
        // --- Animate All Bots Based on Their Team ---
        if (EnemyAiTutorial.allBots != null)
        {
            // Copy list to avoid modification during iteration (e.g. disabling scripts removes bots)
            var botsToAnimate = new List<EnemyAiTutorial>(EnemyAiTutorial.allBots);
            foreach (var bot in botsToAnimate)
            {
                if (bot == null) continue;

                // Use GetComponentInChildren as the controller might not be on the root object.
                var ac = bot.GetComponentInChildren<AnimationController>();
                if (ac == null) continue;

                if (bot.botTeam == scoringTeam)
                {
                    ac.PlayVictory();
                }
                else
                {
                    ac.PlayLoss();
                }
            }
        }

        // --- Animate The Human Player ---
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var playerAnimationController = playerObj.GetComponentInChildren<AnimationController>();
            if (playerAnimationController != null)
            {
                // Assuming the player is on TeamA.
                if (scoringTeam == EnemyAiTutorial.Team.TeamA)
                {
                    playerAnimationController.PlayVictory();
                }
                else
                {
                    playerAnimationController.PlayLoss();
                }
            }
        }
    }
}

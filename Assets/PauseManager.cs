using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Interactors")]
    [Tooltip("The Near-Far interactor GameObject to toggle with the pause menu.")]
    public GameObject nearFarInteractor;

    [Header("UI Panels")]
    [Tooltip("The main pause menu UI.")]
    public GameObject pauseMenu;
    [Tooltip("The settings menu UI panel.")]
    public GameObject settingsMenu;

    [Header("Configuration")]
    [Tooltip("The name of the main menu scene to load.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Input")]
    [Tooltip("The Input Action to trigger the pause menu. Should be a Button type action.")]
    public InputActionReference pauseAction;

    private bool isPaused = false;
    private PauseAndOptionsMenu pauseAndOptionsMenu;

    private void Awake()
    {
        // Ensure time is running when the scene starts, in case it was left paused
        Time.timeScale = 1f;

        // Ensure UI is hidden at the start.
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (settingsMenu != null) settingsMenu.SetActive(false);

        if (pauseMenu == null)
        {
            Debug.LogError("Pause Menu is not assigned in the PauseManager.", this);
        }

        // Find PauseAndOptionsMenu if it exists
        pauseAndOptionsMenu = FindFirstObjectByType<PauseAndOptionsMenu>();

        if (nearFarInteractor == null)
        {
            Debug.LogWarning("PauseManager: Near-Far interactor GameObject is not assigned!");
        }
        else
        {
            nearFarInteractor.SetActive(false);
            Debug.Log("PauseManager: Near-Far interactor GameObject disabled on Awake.");
        }
    }

    private void OnEnable()
    {
        if (pauseAction == null)
        {
            Debug.LogError("Pause Action Reference is not set on the PauseManager.", this);
            return;
        }
        
        pauseAction.action.Enable();
        pauseAction.action.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (pauseAction == null) return;

        pauseAction.action.performed -= OnPausePerformed;
        pauseAction.action.Disable();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        // Ignore pause input if the game is over (so Game Over UI is not toggled accidentally)
        if (GameSession.IsGameOver)
        {
            Debug.Log("PauseManager: Ignoring pause input because game is over.");
            return;
        }
        // If PauseAndOptionsMenu is handling pause, delegate to it
        if (pauseAndOptionsMenu != null)
        {
            bool isCurrentlyPaused = pauseAndOptionsMenu.IsPaused();
            if (isCurrentlyPaused)
            {
                pauseAndOptionsMenu.ClosePauseMenu();
            }
            else
            {
                pauseAndOptionsMenu.OpenPauseMenu();
            }

            // Toggle the interactor state based on the new pause state
            if (nearFarInteractor != null)
            {
                nearFarInteractor.SetActive(!isCurrentlyPaused);
                Debug.Log("PauseManager: Interactor GameObject active state set to " + nearFarInteractor.activeSelf);
            }
            Debug.Log("PauseManager: Delegated pause to PauseAndOptionsMenu");
        }
        else
        {
            // Fallback to regular toggle if PauseAndOptionsMenu doesn't exist
            Debug.Log("PauseManager: PauseAndOptionsMenu not found, using fallback TogglePause");
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        
        // Show/hide the main pause menu
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }

        // If we are unpausing, make sure the settings menu is also hidden
        if (!isPaused && settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }

        Time.timeScale = isPaused ? 0f : 1f;

        // Toggle the interactor
        if (nearFarInteractor != null)
        {
            nearFarInteractor.SetActive(isPaused);
            Debug.Log("PauseManager: Toggled interactor GameObject active state to " + nearFarInteractor.activeSelf);
        }
    }

    /// <summary>
    /// Explicitly set paused/unpaused state. Use this to force pause from other systems
    /// (for example when Game Over appears). `showPauseMenu` controls whether the
    /// regular pause menu is shown; pass false when showing a different UI (game over).
    /// </summary>
    public void SetPaused(bool paused, bool showPauseMenu = true)
    {
        isPaused = paused;

        // Show/hide pause and settings UI
        if (pauseMenu != null)
            pauseMenu.SetActive(isPaused && showPauseMenu);

        if (!isPaused)
        {
            if (settingsMenu != null) settingsMenu.SetActive(false);
        }

        // Apply timescale
        Time.timeScale = isPaused ? 0f : 1f;

        // Toggle the interactor
        if (nearFarInteractor != null)
        {
            nearFarInteractor.SetActive(isPaused);
            Debug.Log("PauseManager: SetPaused changed interactor active state to " + nearFarInteractor.activeSelf);
        }
    }

    public void ResumeGame()
    {
        Debug.Log("PauseManager.ResumeGame() called");
        
        // If PauseAndOptionsMenu exists, delegate to it
        if (pauseAndOptionsMenu != null)
        {
            pauseAndOptionsMenu.ClosePauseMenu();
            Debug.Log("PauseManager: Delegated resume to PauseAndOptionsMenu");

            // Also handle the interactor here when delegating
            if (nearFarInteractor != null)
            {
                nearFarInteractor.SetActive(false);
                Debug.Log("PauseManager: ResumeGame disabled interactor GameObject via delegation.");
            }
            return;
        }

        // Fallback to our own resume logic
        if (!isPaused) return;

        isPaused = false;
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (settingsMenu != null) settingsMenu.SetActive(false); // Also hide settings on resume
        Time.timeScale = 1f;
        
        // Disable the interactor
        if (nearFarInteractor != null)
        {
            nearFarInteractor.SetActive(false);
            Debug.Log("PauseManager: ResumeGame disabled interactor GameObject via fallback.");
        }

        // Safety: Re-enable player movement in case it was disabled
        try
        {
            PlayerPuckHandler puckHandler = FindFirstObjectByType<PlayerPuckHandler>();
            if (puckHandler != null && !puckHandler.hasPuck)
            {
                XRStickMovement movement = puckHandler.playerMovement;
                if (movement != null && !movement.enabled)
                {
                    movement.enabled = true;
                    Debug.Log("PauseManager: Re-enabled XRStickMovement on resume");
                }
            }
        }
        catch
        {
            // Fallback if FindFirstObjectByType is not available in this Unity version
            Debug.LogWarning("Could not re-enable movement after pause");
        }
    }

    // Call this from your Settings button
    public void ToggleSettings(bool show)
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(show);
        }
        else
        {
            Debug.LogWarning("Settings Menu panel is not assigned in the Inspector.");
        }

        // Hide the main pause menu when settings are shown, and vice-versa
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(!show);
        }
    }

    public void QuitToMenu()
    {
        Debug.Log("QuitToMenu called: Setting GameSession.IsInitialized to false.");

        // Ensure the game is unpaused when we quit.
        Time.timeScale = 1f;

        // By setting IsInitialized to false, we tell the ScoreManager to start a fresh game
        // the next time the game scene is loaded.
        GameSession.IsInitialized = false;

        // Load the main menu scene.
        SceneManager.LoadScene(mainMenuSceneName);
    }
}


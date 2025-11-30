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
        }

        // Find PauseAndOptionsMenu if it exists
        pauseAndOptionsMenu = FindFirstObjectByType<PauseAndOptionsMenu>();

        if (nearFarInteractor == null)
        {
        }
        else
        {
            nearFarInteractor.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (pauseAction == null)
        {
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
        if (NewTutorialPanelController.isTutorialActive)
        {
            return;
        }
        // Ignore pause input if the game is over (so Game Over UI is not toggled accidentally)
        if (GameSession.IsGameOver)
        {
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
            }
        }
        else
        {
            // Fallback to regular toggle if PauseAndOptionsMenu doesn't exist
            
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
        }
    }

    public void ResumeGame()
    {
        
        
        // If PauseAndOptionsMenu exists, delegate to it
        if (pauseAndOptionsMenu != null)
        {
            pauseAndOptionsMenu.ClosePauseMenu();
            

            // Also handle the interactor here when delegating
                if (nearFarInteractor != null)
                {
                    nearFarInteractor.SetActive(false);
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
                }
            }
        }
        catch
        {
            // Fallback if FindFirstObjectByType is not available in this Unity version
            
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
            
        }

        // Hide the main pause menu when settings are shown, and vice-versa
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(!show);
        }
    }

    public void QuitToMenu()
    {
        

        // Ensure the game is unpaused when we quit.
        Time.timeScale = 1f;

        // By setting IsInitialized to false, we tell the ScoreManager to start a fresh game
        // the next time the game scene is loaded.
        GameSession.IsInitialized = false;

        // Load the main menu scene.
        SceneManager.LoadScene(mainMenuSceneName);
    }
}


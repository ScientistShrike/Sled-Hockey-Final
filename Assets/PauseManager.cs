using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
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
        TogglePause();
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
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (settingsMenu != null) settingsMenu.SetActive(false); // Also hide settings on resume
        Time.timeScale = 1f;
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

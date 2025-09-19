using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles UI logic for the game over screen, such as restarting the game or returning to the main menu.
/// Attach this script to your game over canvas or a manager object within it.
/// </summary>
public class GameOverMenu : MonoBehaviour
{
    [Tooltip("The name of your main menu scene. Make sure it's added to your Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    /// <summary>
    /// Restarts the current game by reloading the active scene.
    /// This method should be linked to a "Restart" button's OnClick event.
    /// </summary>
    public void RestartGame()
    {
        // Ensure the game is unpaused before reloading the scene.
        Time.timeScale = 1f;

        // Reset the game session state for a fresh start.
        GameSession.IsInitialized = false;

        // Reload the current scene.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Loads the main menu scene.
    /// This method should be linked to a "Main Menu" button's OnClick event.
    /// </summary>
    public void GoToMainMenu()
    {
        // Ensure the game is unpaused before changing scenes.
        Time.timeScale = 1f;

        // Reset the game session state.
        GameSession.IsInitialized = false;

        // Load the main menu scene by name.
        SceneManager.LoadScene(mainMenuSceneName);
    }

    
}

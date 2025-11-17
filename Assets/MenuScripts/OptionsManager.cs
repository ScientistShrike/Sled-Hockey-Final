using UnityEngine;

public static class OptionsManager
{
    // Open options from the main menu (shows options panel, hides main menu)
    public static void OpenOptionsFromMain(GameObject mainMenuPanel, GameObject optionsPanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        // Ensure settings UI is initialized when opened
        if (optionsPanel != null)
        {
            SettingsMenu settings = optionsPanel.GetComponentInChildren<SettingsMenu>(true);
            if (settings != null) settings.Initialize();
        }
    }

    // Close options and return to main menu
    public static void CloseOptionsForMain(GameObject mainMenuPanel, GameObject optionsPanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    // Open options from the pause menu (hide pause panel, show options)
    public static void OpenOptionsFromPause(GameObject pauseMenuPanel, GameObject optionsPanel)
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        // Ensure settings UI is initialized when opened from pause
        if (optionsPanel != null)
        {
            SettingsMenu settings = optionsPanel.GetComponentInChildren<SettingsMenu>(true);
            if (settings != null) settings.Initialize();
        }
    }

    // Return from options back to the pause menu
    public static void BackToPauseFromOptions(GameObject pauseMenuPanel, GameObject optionsPanel)
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }
}

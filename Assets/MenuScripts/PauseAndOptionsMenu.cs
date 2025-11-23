using UnityEngine;

public class PauseAndOptionsMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject pauseMenu;
    public GameObject optionsMenu;
    
    [Header("Interactors")]
    [Tooltip("Drag the near-far interactor GameObject (under the right controller) here.")]
    public GameObject nearFarInteractor;

    private bool isPaused = false;

    void Start()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
        if (nearFarInteractor != null)
            nearFarInteractor.SetActive(false);
    }

    public void OpenPauseMenu()
    {
        pauseMenu.SetActive(true);
        optionsMenu.SetActive(false);
        Time.timeScale = 0f;  // freeze game
        isPaused = true;
        if (nearFarInteractor != null)
            nearFarInteractor.SetActive(true);
        Debug.Log("PauseAndOptionsMenu: Pause menu opened and near-far interactor shown.");
    }

    public void ClosePauseMenu()
    {
        Debug.Log("[RESUME] ClosePauseMenu() called - isPaused was: " + isPaused);
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
        Time.timeScale = 1f;  // unfreeze
        isPaused = false;
        if (nearFarInteractor != null)
            nearFarInteractor.SetActive(false);
        
        Debug.Log("[RESUME] TimeScale set to: " + Time.timeScale);
        Debug.Log("[RESUME] Pause menu active: " + pauseMenu.activeSelf);
        
        // Safety: Re-enable player movement if it was disabled
        try
        {
            PlayerPuckHandler puckHandler = FindFirstObjectByType<PlayerPuckHandler>();
            if (puckHandler != null && !puckHandler.hasPuck)
            {
                XRStickMovement movement = puckHandler.playerMovement;
                if (movement != null && !movement.enabled)
                {
                    movement.enabled = true;
                    Debug.Log("PauseAndOptionsMenu: Re-enabled XRStickMovement on close");
                }
            }
        }
        catch
        {
            // Fallback if FindFirstObjectByType is not available in this Unity version
            Debug.LogWarning("Could not re-enable movement after pause");
        }
        
        Debug.Log("PauseAndOptionsMenu: Pause menu closed and game resumed");
    }

    public void OpenOptionsMenu()
    {
        OptionsManager.OpenOptionsFromPause(pauseMenu, optionsMenu);
        Debug.Log("PauseAndOptionsMenu: Options menu opened via OptionsManager");
    }

    public void BackToPause()
    {
        OptionsManager.BackToPauseFromOptions(pauseMenu, optionsMenu);
        Debug.Log("PauseAndOptionsMenu: Returned to pause menu via OptionsManager");
    }
    
    public bool IsPaused()
    {
        return isPaused;
    }

    public void ResumeGameButton()
    {
        Debug.Log("PauseAndOptionsMenu.ResumeGameButton() called");
        ClosePauseMenu();
        Debug.Log("PauseAndOptionsMenu: Resume button pressed");
    }

    public void Resume()
    {
        Debug.Log("PauseAndOptionsMenu.Resume() called");
        ClosePauseMenu();
    }
}

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
        if (nearFarInteractor != null && !NewTutorialPanelController.isTutorialActive)
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
    }

    public void ClosePauseMenu()
    {
        
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
        Time.timeScale = 1f;  // unfreeze
        isPaused = false;
        if (nearFarInteractor != null && !NewTutorialPanelController.isTutorialActive)
            nearFarInteractor.SetActive(false);
        
        
        
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
                    }
            }
        }
        catch
        {
            // Fallback if FindFirstObjectByType is not available in this Unity version
            
        }
        
        
    }

    public void OpenOptionsMenu()
    {
        OptionsManager.OpenOptionsFromPause(pauseMenu, optionsMenu);
        
    }

    public void BackToPause()
    {
        OptionsManager.BackToPauseFromOptions(pauseMenu, optionsMenu);
        
    }
    
    public bool IsPaused()
    {
        return isPaused;
    }

    public void ResumeGameButton()
    {
        
        ClosePauseMenu();
        
    }

    public void Resume()
    {
        
        ClosePauseMenu();
    }
}

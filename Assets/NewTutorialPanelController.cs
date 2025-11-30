using UnityEngine;

public class NewTutorialPanelController : MonoBehaviour
{
    [Header("Tutorial Panels")]
    [Tooltip("Add your tutorial panel GameObjects here in the order you want them to appear.")]
    public GameObject[] tutorialPanels;

    [Header("XR Interaction")]
    [Tooltip("The Near-Far interactor GameObject to enable for the tutorial UI.")]
    public GameObject nearFarInteractor;

    private int currentPanelIndex = 0;
    public static bool isTutorialActive = false;

    void Start()
    {
        // If the tutorial has already been shown in this session, disable this controller.
        if (GameSession.TutorialHasBeenShown)
        {
            // We disable the whole GameObject to hide all child panels.
            gameObject.SetActive(false);
            return;
        }

        // Mark that the tutorial is now being shown.
        GameSession.TutorialHasBeenShown = true;
        isTutorialActive = true;
        
        // Pause the game, mute audio, and enable UI interaction
        Time.timeScale = 0f;
        AudioListener.volume = 0f; // Mute all audio
        if (nearFarInteractor != null)
        {
            nearFarInteractor.SetActive(true);
        }

        // Show the first panel and hide the rest
        for (int i = 0; i < tutorialPanels.Length; i++)
        {
            if (tutorialPanels[i] != null)
            {
                tutorialPanels[i].SetActive(i == currentPanelIndex);
            }
        }

        // If there are no panels, end the tutorial immediately
        if (tutorialPanels.Length == 0)
        {
            EndTutorial();
        }
    }

    /// <summary>
    /// Shows the next tutorial panel.
    /// This should be called by a button on each tutorial panel.
    /// </summary>
    public void NextPanel()
    {
        // Hide the current panel
        if (currentPanelIndex < tutorialPanels.Length && tutorialPanels[currentPanelIndex] != null)
        {
            tutorialPanels[currentPanelIndex].SetActive(false);
        }

        currentPanelIndex++;

        // Show the next panel if it exists
        if (currentPanelIndex < tutorialPanels.Length && tutorialPanels[currentPanelIndex] != null)
        {
            tutorialPanels[currentPanelIndex].SetActive(true);
        }
        else
        {
            // If there are no more panels, treat it as the end of the tutorial
            EndTutorial();
        }
    }

    /// <summary>
    /// Ends the tutorial, resumes the game, and hides the UI.
    /// This should be called by the "Play" or "Start" button on the last panel.
    /// </summary>
    public void EndTutorial()
    {
        isTutorialActive = false;
        // Hide the current panel
        if (currentPanelIndex < tutorialPanels.Length && tutorialPanels[currentPanelIndex] != null)
        {
            tutorialPanels[currentPanelIndex].SetActive(false);
        }

        // Resume the game, unmute audio, and disable UI interaction
        Time.timeScale = 1f;
        AudioListener.volume = 1f; // Unmute all audio
        if (nearFarInteractor != null)
        {
            nearFarInteractor.SetActive(false);
        }

        // Optional: Disable this script to prevent it from running again
        this.enabled = false;
    }
}

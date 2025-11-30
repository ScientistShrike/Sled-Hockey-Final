using UnityEngine;
using UnityEngine.UI;

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

    void Awake()
    {
        // If the tutorial has already been shown in this session, disable this controller.
        if (GameSessionManager.Instance.TutorialHasBeenShown)
        {
            // We disable the whole GameObject to hide all child panels.
            gameObject.SetActive(false);
            return;
        }

        // Indicate the tutorial is currently active
        isTutorialActive = true;
        
        // Pause the game, mute audio, and enable UI interaction
        Time.timeScale = 0f;
        AudioListener.volume = 0f; // Mute all audio
        if (nearFarInteractor != null)
        {
            nearFarInteractor.SetActive(true);
            Debug.Log("Tutorial started: enabled nearFarInteractor");
        }
        else
        {
            Debug.LogWarning("NewTutorialPanelController: nearFarInteractor is not assigned in the Inspector. Tutorial UI may not be interactable.");
        }

        // Show the first panel and hide the rest
        for (int i = 0; i < tutorialPanels.Length; i++)
        {
            if (tutorialPanels[i] != null)
            {
                tutorialPanels[i].SetActive(i == currentPanelIndex);
            }
        }

        // Quick sanity checks on the UI panels to help debug interaction issues
        for (int i = 0; i < tutorialPanels.Length; i++)
        {
            var panel = tutorialPanels[i];
            if (panel != null)
            {
                var canvas = panel.GetComponentInChildren<Canvas>();
                var gr = panel.GetComponentInChildren<UnityEngine.UI.GraphicRaycaster>();
                if (canvas == null)
                {
                    Debug.LogWarning($"Tutorial panel '{panel.name}' has no Canvas component. Buttons may not be interactable via XR UI.");
                }
                else
                {
                    if (canvas.renderMode != RenderMode.WorldSpace)
                    {
                        Debug.LogWarning($"Tutorial panel '{panel.name}' Canvas is not set to World Space. Set it to World Space for XR UI interaction.");
                    }
                }
                if (gr == null)
                {
                    Debug.LogWarning($"Tutorial panel '{panel.name}' missing GraphicRaycaster. Add a GraphicRaycaster so XR UI can be interacted with.");
                }
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
        Debug.Log("NewTutorialPanelController.NextPanel called");
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
        Debug.Log("NewTutorialPanelController.EndTutorial called");
        isTutorialActive = false;
        // Hide the current panel
        if (currentPanelIndex < tutorialPanels.Length && tutorialPanels[currentPanelIndex] != null)
        {
            tutorialPanels[currentPanelIndex].SetActive(false);
        }

        // Mark that the tutorial has been shown this session. We do this after
        // enabling the interactor to avoid other scripts disabling it during
        // their Awake/Start methods when they're checking for this flag.
        GameSessionManager.Instance.TutorialHasBeenShown = true;

        // Resume the game, unmute audio, and disable UI interaction
        Time.timeScale = 1f;
        AudioListener.volume = 1f; // Unmute all audio
        if (nearFarInteractor != null)
        {
            nearFarInteractor.SetActive(false);
            Debug.Log("Tutorial ended: disabled nearFarInteractor");
        }

        // Optional: Disable this script to prevent it from running again
        this.enabled = false;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsMenuPanel;

    void Start()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        if (optionsMenuPanel != null)
            optionsMenuPanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    public void Tutorial()
    {
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.TutorialHasBeenShown = false;
        }
        SceneManager.LoadScene(2);
    }

    public void OpenOptions()
    {
        OptionsManager.OpenOptionsFromMain(mainMenuPanel, optionsMenuPanel);
    }

    public void CloseOptions()
    {
        OptionsManager.CloseOptionsForMain(mainMenuPanel, optionsMenuPanel);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

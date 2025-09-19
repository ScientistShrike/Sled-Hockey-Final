using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Replace with your actual scene index or name
        SceneManager.LoadScene(1); 
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}


using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    public int PlayerScore { get; set; }
    public int BotScore { get; set; }
    public float TimeRemaining { get; set; }
    public bool IsInitialized { get; set; }
    public bool IsGameOver { get; set; }
    public bool TutorialHasBeenShown { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Initialize default values
            TimeRemaining = 120f;
            IsInitialized = false;
            IsGameOver = false;
            TutorialHasBeenShown = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

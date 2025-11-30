
using UnityEngine;

public class GameSessionInitializer : MonoBehaviour
{
    void Awake()
    {
        if (GameSessionManager.Instance == null)
        {
            GameObject go = new GameObject("GameSessionManager");
            go.AddComponent<GameSessionManager>();
        }
    }
}

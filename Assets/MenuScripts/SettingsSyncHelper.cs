using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SettingsSyncHelper ensures that GameSettings are applied when a new scene loads.
/// This is useful for ensuring settings persist across scene transitions.
/// </summary>
public class SettingsSyncHelper : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If GameSettings exists, reload its settings to apply them to the new scene
        if (GameSettings.Instance != null)
        {
            Debug.Log("[SettingsSyncHelper] Syncing settings for scene: " + scene.name);
            GameSettings.Instance.LoadSettings();
        }
    }
}

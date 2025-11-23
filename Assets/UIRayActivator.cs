using UnityEngine;

// Deprecated stub: UIRayActivator behavior has been removed in favor of menu-driven toggles.
// This file is intentionally a no-op to avoid accidental behavior. Remove this component
// from any GameObjects to fully delete it from the project.
[DisallowMultipleComponent]
[AddComponentMenu("")]
public class UIRayActivator : MonoBehaviour
{
    void Awake()
    {
        Debug.LogWarning("UIRayActivator is deprecated and present as a stub. Remove this component if you want it deleted.");
    }

    /// <summary>
    /// Backwards-compatible no-op. ScoreManager calls this to ensure UI interactors
    /// are enabled when Game Over/UI appears. We now handle interactor toggling
    /// via menu scripts, so this method intentionally does nothing except log.
    /// </summary>
    public void SetInteractorsActive(bool active)
    {
        Debug.Log("UIRayActivator.SetInteractorsActive called (deprecated stub). active=" + active);
        // Intentionally left blank — menu scripts now control interactors.
    }
}

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
    }

    /// <summary>
    /// Backwards-compatible no-op. ScoreManager calls this to ensure UI interactors
    /// are enabled when Game Over/UI appears. We now handle interactor toggling
    /// via menu scripts, so this method intentionally does nothing except log.
    /// </summary>
    public void SetInteractorsActive(bool active)
    {
        // Intentionally left blank — menu scripts now control interactors.
    }
}

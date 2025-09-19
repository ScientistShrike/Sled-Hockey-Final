using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Activates ray interactors when specific UI panels are visible.
/// This is useful for enabling UI interaction only when a menu is open.
/// Attach this to the player's root object (e.g., the XR Origin).
/// </summary>
public class UIRayActivator : MonoBehaviour
{
    [Header("Ray Interactors")]
    [Tooltip("The ray interactor GameObjects to activate/deactivate.")]
    public List<GameObject> rayInteractors;

    [Header("UI Panels to Monitor")]
    [Tooltip("The UI panels that, when active, will enable the ray interactors.")]
    public List<GameObject> uiPanels;

    void Start()
    {
        // Set the initial state of the ray interactors when the game starts.
        UpdateRayInteractorState();
        StartCoroutine(UpdateRayInteractorStateCoroutine());
    }

    private IEnumerator UpdateRayInteractorStateCoroutine()
    {
        while (true)
        {
            UpdateRayInteractorState();
            yield return new WaitForSecondsRealtime(0.1f); // Check every 100ms
        }
    }

    private void UpdateRayInteractorState()
    {
        bool shouldBeActive = false;

        // Check if any of the specified UI panels are currently active.
        foreach (var panel in uiPanels)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                shouldBeActive = true;
                break;
            }
        }

        // Set the active state of all ray interactors to match.
        foreach (var interactorGO in rayInteractors)
        {
            if (interactorGO == null)
                continue;

            interactorGO.SetActive(shouldBeActive);
        }
    }
}

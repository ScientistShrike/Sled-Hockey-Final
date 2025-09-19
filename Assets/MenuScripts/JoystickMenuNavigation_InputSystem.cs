using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class JoystickMenuNavigation_InputSystem : MonoBehaviour
{
    [Header("UI Setup")]
    [Tooltip("The first UI element to be selected when this menu is enabled.")]
    public GameObject firstSelected;

    private void OnEnable()
    {
        // The InputSystemUIInputModule handles enabling/disabling the actions it uses.
        // We just need to set the initial selected button.
        if (firstSelected != null)
        {
            // A small delay is sometimes necessary to ensure the EventSystem is ready,
            // especially on scene loads.
            StartCoroutine(SelectFirstButtonAfterFrame());
        }
    }

    private System.Collections.IEnumerator SelectFirstButtonAfterFrame()
    {
        // We need to wait for the end of the frame for the UI to be properly initialized.
        yield return new WaitForEndOfFrame();
        
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }
}

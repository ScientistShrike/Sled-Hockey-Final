using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickMenuNavigation : MonoBehaviour
{
    public string verticalAxis = "Vertical";            // default Unity input axis
    public KeyCode selectKey = KeyCode.JoystickButton14; // Quest trigger = button 14
    public float inputDelay = 0.35f;  // time between navigation moves

    private float nextInputTime = 0f;

    private void Update()
    {
        if (Time.time < nextInputTime) return;        // wait between moves

        float v = Input.GetAxis(verticalAxis);

        if (v > 0.5f)
        {
            MoveSelection(-1); // up
        }
        else if (v < -0.5f)
        {
            MoveSelection(1);  // down
        }

        if (Mathf.Abs(v) > 0.5f)
        {
            nextInputTime = Time.time + inputDelay;  // cooldown
        }

        // Select / Submit current UI element
        if (Input.GetKeyDown(selectKey))
        {
            var current = EventSystem.current.currentSelectedGameObject;
            if (current != null)
            {
                var btn = current.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.Invoke();
                    Debug.Log("Pressed: " + current.name);
                }
            }
        }
    }

    private void MoveSelection(int direction)
    {
        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null) return;

        Selectable sel = current.GetComponent<Selectable>();
        Selectable next = (direction < 0) ? sel.FindSelectableOnUp() : sel.FindSelectableOnDown();

        if (next != null)
            next.Select();
    }
}

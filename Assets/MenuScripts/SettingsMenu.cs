using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown qualityDropdown;
    public Slider volumeSlider;
    public Slider turnSpeedSlider;
    public Toggle duckPuckToggle;
    // (snap turn toggle removed)

    private const string DuckPuckUnlockedKey = "DuckPuckUnlocked";
    private const string DuckPuckEnabledKey = "DuckPuckEnabled";

    void OnEnable()
    {
        Initialize();
    }

    // Public initializer so other scripts (like OptionsManager) can ensure the UI is synced
    public void Initialize()
    {
        // If GameSettings isn't ready yet (different scene load order), wait a few frames for it
        if (GameSettings.Instance == null)
        {
            StopAllCoroutines();
            StartCoroutine(WaitForGameSettingsAndInit());
            return;
        }

        InitializeInternal();
    }

    private System.Collections.IEnumerator WaitForGameSettingsAndInit()
    {
        float timeout = 2f; // seconds
        float start = Time.realtimeSinceStartup;

        while (GameSettings.Instance == null && Time.realtimeSinceStartup - start < timeout)
        {
            yield return null;
        }

        if (GameSettings.Instance == null)
        {
            yield break;
        }

        InitializeInternal();
    }

    // The actual initialization logic separated so it can be called after waiting
    private void InitializeInternal()
    {
        // If any UI references are missing, try to find them in children (more robust across different option panels)
        if (qualityDropdown == null) qualityDropdown = GetComponentInChildren<TMP_Dropdown>(true);
        if (volumeSlider == null) volumeSlider = GetComponentInChildren<Slider>(true);
        if (turnSpeedSlider == null) turnSpeedSlider = GetComponentInChildren<Slider>(true);
        if (duckPuckToggle == null)
        {
            // Attempt to find a Toggle specifically named for the duck puck to avoid capturing the wrong Toggle when multiple Toggles exist.
            var toggles = GetComponentsInChildren<Toggle>(true);
            Toggle candidate = null;

            foreach (var t in toggles)
            {
                // Check the GameObject name first (fast)
                var name = t.gameObject.name ?? string.Empty;
                if (name.IndexOf("duck", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("duckpuck", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("duck_puck", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("duck-puck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    candidate = t;
                    break;
                }

                // If the GameObject name didn't match, inspect child label text (TextMeshProUGUI or legacy Text)
                var tmpro = t.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (tmpro != null && tmpro.text.IndexOf("duck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    candidate = t;
                    break;
                }

                var legacyText = t.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (legacyText != null && legacyText.text.IndexOf("duck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    candidate = t;
                    break;
                }
            }

            // Fallback: just take the first toggle found if no candidate matched explicitly
            if (candidate == null && toggles.Length > 0) candidate = toggles[0];
            duckPuckToggle = candidate;

            // If still not found, attempt to search the entire root parent (options panel or canvas)
            if (duckPuckToggle == null)
            {
                var root = transform.root;
                if (root != null)
                {
                    var rootToggles = root.GetComponentsInChildren<Toggle>(true);
                    Toggle rootCandidate = null;
                    foreach (var rt in rootToggles)
                    {
                        var name = rt.gameObject.name ?? string.Empty;
                        if (name.IndexOf("duck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            rootCandidate = rt;
                            break;
                        }
                        var tmpro = rt.GetComponentInChildren<TMPro.TMP_Text>(true);
                        if (tmpro != null && tmpro.text.IndexOf("duck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            rootCandidate = rt;
                            break;
                        }
                        var legacyText = rt.GetComponentInChildren<UnityEngine.UI.Text>(true);
                        if (legacyText != null && legacyText.text.IndexOf("duck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            rootCandidate = rt;
                            break;
                        }
                    }
                    if (rootCandidate != null)
                    {
                        duckPuckToggle = rootCandidate;
                        Debug.Log($"SettingsMenu: Found duckPuckToggle on parent root '{root.name}' (component '{duckPuckToggle.gameObject.name}').");
                    }
                }
            }
        }

        // If we found a toggle by fallback, warn that it may not be the correct toggle (helpful for debugging)
        if (duckPuckToggle != null)
        {
            var check = duckPuckToggle.gameObject.name.ToLower();
            var label = duckPuckToggle.GetComponentInChildren<TMPro.TMP_Text>(true)?.text?.ToLower() ?? duckPuckToggle.GetComponentInChildren<UnityEngine.UI.Text>(true)?.text?.ToLower();
            if (!check.Contains("duck") && (label == null || !label.Contains("duck")))
            {
                Debug.LogWarning($"SettingsMenu: duckPuckToggle ({duckPuckToggle.gameObject.name}) was auto-selected by fallback - rename or explicitly assign the toggle to SettingsMenu.duckPuckToggle in the inspector to avoid ambiguity.");
            }
        }


        // Detach listeners to prevent multiple subscriptions if Initialize is called more than once.
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (turnSpeedSlider != null) turnSpeedSlider.onValueChanged.RemoveListener(OnTurnSpeedChanged);
        if (duckPuckToggle != null) duckPuckToggle.onValueChanged.RemoveListener(OnDuckPuckToggleChanged);


        // Initialize and subscribe to UI events
        InitializeQualityDropdown();
        InitializeVolumeSlider();
        InitializeTurnSpeedSlider();
        InitializeDuckPuckToggle();
    }

    // snap toggle removed

    private void InitializeDuckPuckToggle()
    {
        if (duckPuckToggle == null) {
            Debug.Log($"SettingsMenu: Could not find duckPuckToggle in children of '{gameObject.name}'. Please assign it in the inspector (or ensure a Toggle with name or label contains 'duck').");
            return;
        }

        bool isUnlocked = PlayerPrefs.GetInt(DuckPuckUnlockedKey, 0) == 1;
        duckPuckToggle.gameObject.SetActive(isUnlocked);

        if (isUnlocked)
        {
            duckPuckToggle.isOn = PlayerPrefs.GetInt(DuckPuckEnabledKey, 0) == 1;
            duckPuckToggle.onValueChanged.AddListener(OnDuckPuckToggleChanged);
            Debug.Log($"SettingsMenu: bound duckPuckToggle component '{duckPuckToggle.gameObject.name}' in '{duckPuckToggle.gameObject.scene.name}' (parent frame '{gameObject.name}')");
            Debug.Log($"Duck puck toggle initialized. Unlocked: {isUnlocked}, Enabled: {duckPuckToggle.isOn}");
        }
    }

    public void OnDuckPuckToggleChanged(bool isEnabled)
    {
        PlayerPrefs.SetInt(DuckPuckEnabledKey, isEnabled ? 1 : 0);
        PlayerPrefs.Save();

        // Update any existing puck skin managers in the active scenes so the change applies immediately
        Debug.Log($"SettingsMenu: Duck Puck toggle changed. Enabled={isEnabled}");
        try
        {
            PuckSkinManager.RefreshAllPucks();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("SettingsMenu: Failed to refresh all pucks: " + ex.Message);
        }
    }

    private void InitializeQualityDropdown()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(GameSettings.Instance.GetQualityNames()));
        qualityDropdown.value = GameSettings.Instance.GetQualityLevel();
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    public void OnQualityChanged(int index)
    {
        GameSettings.Instance.SetQualityLevel(index);
    }

    private void InitializeTurnSpeedSlider()
    {
        if (turnSpeedSlider == null) return;

        turnSpeedSlider.minValue = 30f;
        turnSpeedSlider.maxValue = 150f;
        turnSpeedSlider.value = GameSettings.Instance.GetTurnSpeed();
        turnSpeedSlider.onValueChanged.AddListener(OnTurnSpeedChanged);
    }

    public void OnTurnSpeedChanged(float speed)
    {
        GameSettings.Instance.SetTurnSpeed(speed);
    }

    private void InitializeVolumeSlider()
    {
        if (volumeSlider == null) return;

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = GameSettings.Instance.GetVolume();
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    public void OnVolumeChanged(float volumeValue)
    {
        GameSettings.Instance.SetVolume(volumeValue);
    }
}

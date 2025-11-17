using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown qualityDropdown;
    public Slider volumeSlider;
    public Slider turnSpeedSlider;
    // (snap turn toggle removed)

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
            Debug.LogError("SettingsMenu: GameSettings singleton not found after waiting. Make sure a GameObject with GameSettings is present in a bootstrap scene or marked DontDestroyOnLoad.");
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

        // Detach listeners to prevent multiple subscriptions if Initialize is called more than once.
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (turnSpeedSlider != null) turnSpeedSlider.onValueChanged.RemoveListener(OnTurnSpeedChanged);

        // Initialize and subscribe to UI events
        InitializeQualityDropdown();
        InitializeVolumeSlider();
        InitializeTurnSpeedSlider();
    }

    // snap toggle removed

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

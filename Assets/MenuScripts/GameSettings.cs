using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("Audio")]
    public AudioMixer masterMixer;
    public string volumeParameter = "MasterVolume";

    [Header("Player Movement")]
    public XRStickMovement playerMovement;

    // Settings data
    private int qualityLevel = 0;
    private float volume = 0.8f;
    private float turnSpeed = 90f;

    // Settings keys for PlayerPrefs
    private const string QUALITY_KEY = "GameSettings_Quality";
    private const string VOLUME_KEY = "GameSettings_Volume";
    private const string TURN_SPEED_KEY = "GameSettings_TurnSpeed";

    // Default values
    private const int DEFAULT_QUALITY = 2;
    private const float DEFAULT_VOLUME = 0.8f;
    private const float DEFAULT_TURN_SPEED = 90f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    public void LoadSettings()
    {
        qualityLevel = PlayerPrefs.GetInt(QUALITY_KEY, DEFAULT_QUALITY);
        QualitySettings.SetQualityLevel(qualityLevel, true);

        volume = PlayerPrefs.GetFloat(VOLUME_KEY, DEFAULT_VOLUME);
        ApplyVolumeSettings(volume);

        turnSpeed = PlayerPrefs.GetFloat(TURN_SPEED_KEY, DEFAULT_TURN_SPEED);
        ApplyTurnSpeedSettings(turnSpeed);
    }

    // Quality
    public int GetQualityLevel() => qualityLevel;

    public void SetQualityLevel(int index)
    {
        qualityLevel = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(qualityLevel, true);
        PlayerPrefs.SetInt(QUALITY_KEY, qualityLevel);
        PlayerPrefs.Save();
    }

    public string[] GetQualityNames() => QualitySettings.names;

    // Volume
    public float GetVolume() => volume;

    public void SetVolume(float sliderValue)
    {
        volume = Mathf.Clamp01(sliderValue);
        ApplyVolumeSettings(volume);
        PlayerPrefs.SetFloat(VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    private void ApplyVolumeSettings(float volumeValue)
    {
        if (masterMixer == null) return;
        float dB = Mathf.Log10(Mathf.Max(volumeValue, 0.0001f)) * 20f;
        masterMixer.SetFloat(volumeParameter, dB);
    }

    // Turn Speed
    public float GetTurnSpeed() => turnSpeed;

    public void SetTurnSpeed(float speed)
    {
        turnSpeed = Mathf.Clamp(speed, 30f, 150f);
        ApplyTurnSpeedSettings(turnSpeed);
        PlayerPrefs.SetFloat(TURN_SPEED_KEY, turnSpeed);
        PlayerPrefs.Save();
    }

    private void ApplyTurnSpeedSettings(float speedValue)
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<XRStickMovement>();
        }

        if (playerMovement != null)
        {
            playerMovement.SetTurnSpeed(speedValue);
        }
    }

    public void ResetToDefaults()
    {
        SetQualityLevel(DEFAULT_QUALITY);
        SetVolume(DEFAULT_VOLUME);
        SetTurnSpeed(DEFAULT_TURN_SPEED);
        // reset uses only existing settings
    }
}

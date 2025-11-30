using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundEffects : MonoBehaviour
{
    public static SoundEffects Instance { get; private set; }

    [Header("Audio Objects (GameObjects with AudioSource)")]
    public GameObject hitSoundObject;
    public GameObject cheerSoundObject;
    public GameObject sadSoundObject;
    public GameObject tiredSoundObject;
    [Header("Default durations (seconds)")]
    public float hitDuration = 0.8f;
    public float cheerDuration = 2.0f;
    public float sadDuration = 1.5f;
    public float tiredDuration = 2.0f;

    private Coroutine playingRoutine;

    private AudioSource audioSource;

    /// <summary>
    /// Returns the primary audio source used by the SoundEffects singleton.
    /// </summary>
    public AudioSource GetPrimaryAudioSource() => audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Provide a clear warning that a duplicate component was added to a GameObject.
            // We will destroy the component instead of the GameObject to avoid deleting
            // AI actors that may have the component attached accidentally.
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
        }

        // Ensure the singleton persists across scenes if this is the intended global audio manager.
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    public void PlayHitSound()
    {
        PlayHitSound(hitDuration);
    }

    private AudioSource ResolveAudioSource(GameObject audioObject)
    {
        if (audioObject == null) return null;
        var src = audioObject.GetComponentInChildren<AudioSource>();
        return src;
    }

    private System.Collections.IEnumerator PlayAudioSourceAndStop(AudioSource src, float duration, bool loop = false)
    {
        if (src == null) yield break;
        src.loop = loop;
        src.Play();
        if (duration <= 0f || float.IsInfinity(duration)) yield break;
        yield return new WaitForSecondsRealtime(duration);
        if (!loop && src.isPlaying) src.Stop();
    }

    public void PlayHitSound(float duration)
    {
        AudioSource src = ResolveAudioSource(hitSoundObject) ?? audioSource;
        if (src == null) return;
        if (playingRoutine != null) StopCoroutine(playingRoutine);
        playingRoutine = StartCoroutine(PlayAudioSourceAndStop(src, duration, loop: false));
    }

    public void PlayCheerSound()
    {
        PlayCheerSound(cheerDuration);
    }

    public void PlayCheerSound(float duration)
    {
        AudioSource src = ResolveAudioSource(cheerSoundObject) ?? audioSource;
        if (src == null) return;
        if (playingRoutine != null) StopCoroutine(playingRoutine);
        playingRoutine = StartCoroutine(PlayAudioSourceAndStop(src, duration, loop: false));
    }

    public void PlaySadSound()
    {
        PlaySadSound(sadDuration);
    }

    public void PlaySadSound(float duration)
    {
        AudioSource src = ResolveAudioSource(sadSoundObject) ?? audioSource;
        if (src == null) return;
        if (playingRoutine != null) StopCoroutine(playingRoutine);
        playingRoutine = StartCoroutine(PlayAudioSourceAndStop(src, duration, loop: false));
    }

    public void PlayTiredSound()
    {
        PlayTiredSound(tiredDuration);
    }

    public void PlayTiredSound(float duration)
    {
        AudioSource src = ResolveAudioSource(tiredSoundObject) ?? audioSource;
        if (src == null) return;
        if (playingRoutine != null) StopCoroutine(playingRoutine);
        playingRoutine = StartCoroutine(PlayAudioSourceAndStop(src, duration, loop: duration <= 0f));
    }

    private System.Collections.IEnumerator PlayAndStop(float duration)
    {
        // Backwards-compat: support for previously existing callers that didn't provide a source
        if (audioSource == null) yield break;
        audioSource.Play();
        if (duration <= 0f || float.IsInfinity(duration)) yield break;
        yield return new WaitForSecondsRealtime(duration);
        if (audioSource.isPlaying) audioSource.Stop();
    }
}
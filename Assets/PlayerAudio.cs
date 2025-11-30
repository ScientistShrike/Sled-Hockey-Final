using System.Collections;
using UnityEngine;

/// <summary>
/// Simple audio helper for the Player so ScoreManager can invoke cheer/sad/etc. on the player.
/// Mirrors the fallback behavior used in EnemyAiTutorial: prefer a local AudioSource, else fall back to SoundEffects.Instance.
/// Attach this to the player GameObject (or a child) to enable per-player audio.
/// </summary>
public class PlayerAudio : MonoBehaviour
{
    [Header("Audio (per-player GameObject with AudioSource preferred)")]
    public GameObject hitSoundObject;
    public GameObject cheerSoundObject;
    public GameObject sadSoundObject;
    public GameObject tiredSoundObject;

    private Coroutine playingRoutine;
    private AnimationController animController;

    private void StopAllLocalAudio()
    {
        if (playingRoutine != null)
        {
            StopCoroutine(playingRoutine);
            playingRoutine = null;
        }

        // Stop audio sources on assigned GameObjects
        StopAudioObject(cheerSoundObject);
        StopAudioObject(sadSoundObject);
        StopAudioObject(tiredSoundObject);
        StopAudioObject(hitSoundObject);
    }

    private void StopAudioObject(GameObject audioObject)
    {
        if (audioObject == null) return;
        var src = audioObject.GetComponentInChildren<AudioSource>();
        if (src != null && src.isPlaying)
        {
            src.loop = false;
            src.Stop();
        }
    }

    private void Awake()
    {
        animController = GetComponentInChildren<AnimationController>();
    }

    private IEnumerator PlayAndStop(AudioSource source, float duration, bool loop = false)
    {
        if (source == null) yield break;
        source.loop = loop;
        source.Play();
        if (float.IsInfinity(duration) || duration <= 0f)
        {
            if (!loop) yield break;
            else yield break;
        }
        yield return new WaitForSecondsRealtime(duration);
        if (!loop && source.isPlaying) source.Stop();
    }

    public void PlayCheerSound(float duration = -1f)
    {
        // Stop any character-local audio before playing the cheer
        StopAllLocalAudio();
        AudioSource cheerSrc = cheerSoundObject != null ? cheerSoundObject.GetComponentInChildren<AudioSource>() : null;
        if (cheerSrc == null && SoundEffects.Instance != null) cheerSrc = SoundEffects.Instance.GetPrimaryAudioSource();
        if (cheerSrc != null)
        {
            if (playingRoutine != null) StopCoroutine(playingRoutine);
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.victoryDuration;
            playingRoutine = StartCoroutine(PlayAndStop(cheerSrc, dur, loop: false));
            return;
        }

        if (SoundEffects.Instance != null)
        {
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.victoryDuration;
            
            SoundEffects.Instance.PlayCheerSound(dur);
            return;
        }

        
    }

    public void PlaySadSound(float duration = -1f)
    {
        // Stop any character-local audio before playing the sad sound
        StopAllLocalAudio();
        AudioSource sadSrc = sadSoundObject != null ? sadSoundObject.GetComponentInChildren<AudioSource>() : null;
        if (sadSrc == null && tiredSoundObject != null) sadSrc = tiredSoundObject.GetComponentInChildren<AudioSource>();
        if (sadSrc == null && SoundEffects.Instance != null) sadSrc = SoundEffects.Instance.GetPrimaryAudioSource();
        if (sadSrc != null)
        {
            if (playingRoutine != null) StopCoroutine(playingRoutine);
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.lossDuration;
            playingRoutine = StartCoroutine(PlayAndStop(sadSrc, dur, loop: false));
            return;
        }

        // Fallback to tiredSound if configured
        if (tiredSoundObject != null)
        {
            if (playingRoutine != null) StopCoroutine(playingRoutine);
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.lossDuration;
            AudioSource tsrc = tiredSoundObject.GetComponentInChildren<AudioSource>();
            if (tsrc == null && SoundEffects.Instance != null) tsrc = SoundEffects.Instance.GetPrimaryAudioSource();
            playingRoutine = StartCoroutine(PlayAndStop(tsrc, dur, loop: false));
            return;
        }

        if (SoundEffects.Instance != null)
        {
            float dur = duration;
            if (dur <= 0f && animController != null) dur = animController.lossDuration;
            
            SoundEffects.Instance.PlaySadSound(dur);
            return;
        }

        
    }

    public void PlayHitSound(float duration = -1f)
    {
        // Stop any character-local audio before playing the hit sound
        StopAllLocalAudio();
        AudioSource hitSrc = hitSoundObject != null ? hitSoundObject.GetComponentInChildren<AudioSource>() : null;
        if (hitSrc == null && SoundEffects.Instance != null) hitSrc = SoundEffects.Instance.GetPrimaryAudioSource();
        if (hitSrc == null) return;
        if (playingRoutine != null) StopCoroutine(playingRoutine);
        float dur = duration;
        if (dur <= 0f && animController != null) dur = animController.hitDuration;
        playingRoutine = StartCoroutine(PlayAndStop(hitSrc, dur, loop: false));
    }

    public void StartTiredSound()
    {
        // Stop other audio for this character before starting the looping tired sound
        StopAllLocalAudio();
        AudioSource tiredSrc = tiredSoundObject != null ? tiredSoundObject.GetComponentInChildren<AudioSource>() : null;
        if (tiredSrc == null && SoundEffects.Instance != null) tiredSrc = SoundEffects.Instance.GetPrimaryAudioSource();
        if (tiredSrc != null && !tiredSrc.isPlaying)
        {
            tiredSrc.loop = true;
            tiredSrc.Play();
        }
    }

    public void StopTiredSound()
    {
        AudioSource tiredSrc = tiredSoundObject != null ? tiredSoundObject.GetComponentInChildren<AudioSource>() : null;
        if (tiredSrc == null && SoundEffects.Instance != null) tiredSrc = SoundEffects.Instance.GetPrimaryAudioSource();
        if (tiredSrc != null && tiredSrc.isPlaying)
        {
            tiredSrc.loop = false;
            tiredSrc.Stop();
        }
    }
}

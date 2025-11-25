using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GoalCelebration : MonoBehaviour
{
    public ParticleSystem[] confettiEffects;
    public GameObject goalHornObject; // GameObject which has an AudioSource to play the horn from

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Ensure the particle systems don't play on awake or loop.
        if (confettiEffects != null)
        {
            foreach (var effect in confettiEffects)
            {
                if (effect != null)
                {
                    var main = effect.main;
                    main.playOnAwake = false;
                    main.loop = false;
                }
            }
        }
    }

    private void Start()
    {
        // Stop the particle systems when the scene loads to prevent them from playing at the start.
        if (confettiEffects != null)
        {
            foreach (var effect in confettiEffects)
            {
                if (effect != null)
                {
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }

    public void Play()
    {
        if (confettiEffects != null)
        {
            foreach (var effect in confettiEffects)
            {
                if (effect != null)
                {
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    effect.Play();
                }
            }
        }

        AudioSource goalSrc = goalHornObject != null ? goalHornObject.GetComponentInChildren<AudioSource>() : audioSource;
        if (goalSrc != null)
        {
            // Play the horn using Play so we can stop it when the particle effects finish.
            goalSrc.loop = false;
            goalSrc.Play();

            // If we have confetti particle systems, stop the audio when the longest particle duration finishes
            float maxParticleDuration = 0f;
            if (confettiEffects != null)
            {
                foreach (var effect in confettiEffects)
                {
                    if (effect == null) continue;
                    var main = effect.main;
                    if (main.duration > maxParticleDuration) maxParticleDuration = main.duration;
                }
            }

            if (maxParticleDuration > 0f)
            {
                StartCoroutine(StopAudioAfter(maxParticleDuration, goalSrc));
            }
        }
    }

    private System.Collections.IEnumerator StopAudioAfter(float seconds, AudioSource source)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (source != null && source.isPlaying) source.Stop();
    }
}

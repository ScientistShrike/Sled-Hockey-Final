using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GoalCelebration : MonoBehaviour
{
    public ParticleSystem[] confettiEffects;
    public AudioClip goalHornSound;

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

        if (goalHornSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(goalHornSound);
        }
    }
}

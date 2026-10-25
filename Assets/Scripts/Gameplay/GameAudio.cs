
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [Header("Hit Sounds")]
    [SerializeField] private AudioClip[] beeHitSounds;
    [SerializeField] private AudioClip[] bombHitSounds;

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f)]
    private float sfxVolume = 0.75f;

    [SerializeField, Range(0f, 0.2f)]
    private float pitchVariation = 0.05f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlayHit(int points)
    {
        AudioClip[] clips =
            points >= 0 ? beeHitSounds : bombHitSounds;

        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip =
            clips[Random.Range(0, clips.Length)];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(
            1f - pitchVariation,
            1f + pitchVariation
        );

        audioSource.PlayOneShot(clip, sfxVolume);
    }
}

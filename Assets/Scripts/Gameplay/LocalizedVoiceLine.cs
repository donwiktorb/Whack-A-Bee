
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LocalizedVoiceLine : MonoBehaviour
{
    public static LocalizedVoiceLine Instance { get; private set; }

    [Header("Round Start Voice Clips")]
    [SerializeField] private AudioClip english;
    [SerializeField] private AudioClip polish;
    [SerializeField] private AudioClip german;
    [SerializeField] private AudioClip japanese;

    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlayRoundStart()
    {
        string language =
            LocalizationManager.Instance?.CurrentLanguage ?? "en";

        AudioClip selected = language switch
        {
            "pl" => polish,
            "de" => german,
            "ja" => japanese,
            _ => english
        };

        if (selected == null)
            selected = english;

        if (selected == null)
        {
            Debug.LogWarning(
                "[Voice] No audio clip assigned."
            );
            return;
        }

        audioSource.Stop();
        audioSource.PlayOneShot(selected);

        Debug.Log(
            "[Voice] Playing round-start announcement: " +
            language
        );
    }

    [ContextMenu("Voice / Test Current Language")]
    private void TestVoice()
    {
        if (Application.isPlaying)
            PlayRoundStart();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

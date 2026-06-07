using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Channels")]
    [Tooltip("The audio source dedicated to playing the background music loop")]
    public AudioSource musicSource;

    [Tooltip("The audio source dedicated to playing narrative officer dialogue lines")]
    public AudioSource dialogueSource;

    [Header("SFX Pooling System")]
    [Tooltip("How many sound effects can physically overlap at the exact same time")]
    public int sfxPoolSize = 12;
    private List<AudioSource> sfxPool = new List<AudioSource>();

    [Header("Musou Sound Library")]
    [Tooltip("Add your audio clips here. Give them recognizable names in the inspector.")]
    public List<NamedAudioClip> soundLibrary;
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

    [System.Serializable]
    public struct NamedAudioClip
    {
        public string soundName;
        public AudioClip clip;
    }

    void Awake()
    {
        // Establish the global Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeSFXPool();
        BuildLibraryDictionary();
    }

    private void InitializeSFXPool()
    {
        // Pre-warm a grid of audio channels so we never have to run Instantiate during combat
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource newChannel = gameObject.AddComponent<AudioSource>();
            newChannel.playOnAwake = false;
            newChannel.spatialBlend = 0f; // 2D Sound

            // FIXED LINE: Changed lowercase '.add' to capital '.Add'
            sfxPool.Add(newChannel);
        }
    }

    private void BuildLibraryDictionary()
    {
        // Convert the list into a fast string dictionary lookup table
        foreach (var entry in soundLibrary)
        {
            if (!string.IsNullOrEmpty(entry.soundName) && entry.clip != null)
            {
                sfxDictionary[entry.soundName.ToLower()] = entry.clip;
            }
        }
    }

    /// <summary>
    /// Finds a free audio channel and plays a sound effect. 
    /// If multiple sounds match (e.g., 'hitimpact_0', 'hitimpact_1'), it picks one randomly.
    /// </summary>
    public void PlaySFX(string soundName, float volume = 1f, float pitchRandomness = 0.05f)
    {
        string lookupKey = soundName.ToLower();
        List<AudioClip> matchingClips = new List<AudioClip>();

        // 1. Scan the library to see if we have a pool of sounds (e.g., hitimpact_0, hitimpact_1)
        foreach (var key in sfxDictionary.Keys)
        {
            if (key == lookupKey || key.StartsWith(lookupKey + "_"))
            {
                matchingClips.Add(sfxDictionary[key]);
            }
        }

        if (matchingClips.Count == 0)
        {
            Debug.LogWarning($"[SOUND] Audio clip or pool named '{soundName}' was not found.");
            return;
        }

        // 2. Pick a random clip from the matching pool
        AudioClip clipToPlay = matchingClips[Random.Range(0, matchingClips.Count)];
        AudioSource freeChannel = GetAvailableChannel();

        if (freeChannel != null)
        {
            freeChannel.clip = clipToPlay;
            freeChannel.volume = volume;
            freeChannel.pitch = Random.Range(1f - pitchRandomness, 1f + pitchRandomness);
            freeChannel.Play();
        }
    }

    /// <summary>
    /// Plays or swaps the background battle music tracks.
    /// </summary>
    public void PlayBGM(AudioClip musicClip, bool loop = true)
    {
        if (musicSource == null || musicClip == null) return;

        musicSource.clip = musicClip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    /// <summary>
    /// Plays narrative officer voicelines, overriding any current speaking line.
    /// </summary>
    public void PlayDialogue(AudioClip voiceClip)
    {
        if (dialogueSource == null || voiceClip == null) return;

        dialogueSource.clip = voiceClip;
        dialogueSource.Play();
    }

    private AudioSource GetAvailableChannel()
    {
        // 1. Look for a channel that is completely quiet right now
        foreach (var channel in sfxPool)
        {
            if (!channel.isPlaying) return channel;
        }

        // FIXED LINE: Emergency fallback grabs index 0 if the pool is full
        return sfxPool[0];
    }
}
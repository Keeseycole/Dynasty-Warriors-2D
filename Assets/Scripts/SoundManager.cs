using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Channels")]
    public AudioSource musicSource;
    public AudioSource dialogueSource;

    [Header("SFX Pooling System")]
    public int sfxPoolSize = 12;
    private List<AudioSource> sfxPool = new List<AudioSource>();

    [Header("Musou Sound Library")]
    public List<NamedAudioClip> soundLibrary;
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

    [Header("Footstep Surface Pools")]
    [Tooltip("Drag and drop your Grass footstep audio clips here!")]
    public List<AudioClip> grassFootsteps = new List<AudioClip>();

    [Tooltip("Drag and drop your Dirt footstep audio clips here!")]
    public List<AudioClip> dirtFootsteps = new List<AudioClip>();

    [Tooltip("Drag and drop your Stone footstep audio clips here!")]
    public List<AudioClip> stoneFootsteps = new List<AudioClip>();

    [Header("🟩 GLOBAL MIX VOLUMES")]
    [Range(0f, 1f)]
    [Tooltip("Global master slider for all background music tracks.")]
    public float musicMasterVolume = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Global master slider for all sword slices, hits, and impact sound effects.")]
    public float sfxMasterVolume = 0.8f;

    [Range(0f, 1f)]
    [Tooltip("Global master multiplier specifically for character footstep levels.")]
    public float footstepMasterVolume = 0.3f;

    [System.Serializable]
    public struct NamedAudioClip
    {
        public string soundName;
        public AudioClip clip;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeSFXPool();
        BuildLibraryDictionary();
    }

    private void Update()
    {
        // Dynamically recalculates active channel levels on every frame update step!
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.volume = musicMasterVolume;
        }

        if (dialogueSource != null && dialogueSource.isPlaying)
        {
            dialogueSource.volume = sfxMasterVolume; // Dialogue scales alongside Master SFX channels
        }
    }

    private void InitializeSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource newChannel = gameObject.AddComponent<AudioSource>();
            newChannel.playOnAwake = false;
            newChannel.spatialBlend = 0f;
            sfxPool.Add(newChannel);
        }
    }

    private void BuildLibraryDictionary()
    {
        foreach (var entry in soundLibrary)
        {
            if (!string.IsNullOrEmpty(entry.soundName) && entry.clip != null)
            {
                sfxDictionary[entry.soundName] = entry.clip;

                string lowerKey = entry.soundName.ToLower();
                if (!sfxDictionary.ContainsKey(lowerKey))
                {
                    sfxDictionary.Add(lowerKey, entry.clip);
                }
            }
        }
    }

    public void PlaySFX(AudioClip clipToPlay, float volume = 1f, float pitchVariance = 0f)
    {
        if (clipToPlay == null) return;

        AudioSource sourceToUse = null;

        if (sfxPool != null && sfxPool.Count > 0)
        {
            for (int i = 0; i < sfxPool.Count; i++)
            {
                if (sfxPool[i] != null && !sfxPool[i].isPlaying)
                {
                    sourceToUse = sfxPool[i];
                    break;
                }
            }
            if (sourceToUse == null) sourceToUse = sfxPool[0];
            
        }

        if (sourceToUse == null) sourceToUse = GetComponent<AudioSource>();

        if (sourceToUse != null)
        {
            sourceToUse.pitch = 1.0f + pitchVariance;

            // ========================================================================
            // 🟩 SFX MASTER VOLUME SCALING:
            // Multiplies the incoming volume flag by your global master slider coefficient!
            // ========================================================================
            float finalScaledSFXVolume = volume * sfxMasterVolume;

            sourceToUse.PlayOneShot(clipToPlay, finalScaledSFXVolume);
        }
    }

    public void PlaySFX(string sfxName, float volume = 1f, float pitchVariance = 0f)
    {
        AudioClip clipToPlay = null;

        if (sfxDictionary != null && sfxDictionary.TryGetValue(sfxName, out AudioClip foundClip))
        {
            clipToPlay = foundClip;
        }

        if (clipToPlay == null && sfxDictionary != null)
        {
            sfxDictionary.TryGetValue(sfxName.ToLower(), out clipToPlay);
        }

        if (clipToPlay == null && soundLibrary != null)
        {
            for (int i = 0; i < soundLibrary.Count; i++)
            {
                if (soundLibrary[i].soundName == sfxName || soundLibrary[i].soundName.ToLower() == sfxName.ToLower())
                {
                    clipToPlay = soundLibrary[i].clip;
                    break;
                }
            }
        }

        if (clipToPlay != null)
        {
            PlaySFX(clipToPlay, volume, pitchVariance);
        }
        else
        {
            Debug.LogWarning($"[SOUND MANAGER] PlaySFX requested string name '{sfxName}', but no matching asset row exists in your Sound Library!");
        }
    }

    public void PlayBGM(AudioClip musicClip, bool loop = true)
    {
        if (musicSource == null || musicClip == null) return;

        musicSource.volume = musicMasterVolume;

        musicSource.clip = musicClip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlayDialogue(AudioClip voiceClip)
    {
        if (dialogueSource == null || voiceClip == null) return;

        dialogueSource.clip = voiceClip;
        dialogueSource.Play();
    }

    private AudioSource GetAvailableChannel()
    {
        foreach (var channel in sfxPool)
        {
            if (!channel.isPlaying) return channel;
        }
        return sfxPool[0];
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

  

    // Tracking toggle to cleanly switch between left and right foot assets on every step!
    private bool isLeftFootstep = false;

    public void PlayFootstepSFX(string surfaceTag, float volume = 0.4f)
    {
        List<AudioClip> selectedPool = null;

        switch (surfaceTag)
        {
            case "Grass": selectedPool = grassFootsteps; break;
            case "Dirt": selectedPool = dirtFootsteps; break;
            case "Stone": selectedPool = stoneFootsteps; break;
            default: selectedPool = dirtFootsteps; break;
        }

        if (selectedPool == null || selectedPool.Count == 0) return;

        AudioClip clipToPlay = null;

        if (selectedPool.Count >= 2)
        {
            isLeftFootstep = !isLeftFootstep;
            clipToPlay = isLeftFootstep ? selectedPool[0] : selectedPool[1]; // Left gets Element 0, Right gets Element 1
        }
        else
        {
            clipToPlay = selectedPool[0]; // Fallback baseline if only 1 sound is in the list
        }

        if (clipToPlay != null)
        {
            float randomPitchVariance = Random.Range(-0.04f, 0.04f);

            // ========================================================================
            // 🟩 FOOTSTEP MIX MUTLIPLIER:
            // Combines your incoming step volume baseline with your dedicated master footstep slider!
            // ========================================================================
            float finalScaledStepVolume = volume * footstepMasterVolume;

            PlaySFX(clipToPlay, finalScaledStepVolume, randomPitchVariance);
        }
    }
}
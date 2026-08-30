using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageMusicTrigger : MonoBehaviour
{
    // Static instance allows your TriggerOnEnter script to lock onto this fade method instantly!
    public static StageMusicTrigger Instance { get; private set; }

    [Header("Battle Theme Track")]
    [Tooltip("Drag the background music AudioClip you want to play for this stage here")]
    public AudioClip stageBattleMusic;

    private Coroutine activeFadeRoutine;

    private void Awake()
    {
        // Singleton pattern registration
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Give the scene a split second to initialize, then blast the music
        Invoke("StartStageBGM", 0.1f);
    }

    private void StartStageBGM()
    {
        // 🔥 THE INITIALIZATION INTERLOCK GUARD (FIXED):
        // If a physical volume trigger zone has already requested a fade-out pass
        // before this boot timer finished its 0.1s delay, cancel the startup call completely!
        if (activeFadeRoutine != null)
        {
            Debug.Log("[AUDIO GUARD]: Aborted initial stage music blast because a zone fade is actively running!");
            return;
        }

        if (SoundManager.Instance != null && stageBattleMusic != null)
        {
            SoundManager.Instance.PlayBGM(stageBattleMusic, true);
            Debug.Log($"[AUDIO] Successfully playing battle theme: {stageBattleMusic.name}");
        }
        else
        {
            Debug.LogWarning("[AUDIO] Failed to play BGM. Missing SoundManager instance or AudioClip asset.");
        }
    }

    // ========================================================================
    // 🟩 THE GRADUAL AUDIO ACCELERATION INTERFACE (NEW):
    // Public access hook that any physical trigger zone can call to smoothly 
    // fade your stage audio track down to absolute zero!
    // ========================================================================
    public void FadeOutCurrentBGM(float duration)
    {
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(SmoothBGMFadeRoutine(duration));
    }

    private IEnumerator SmoothBGMFadeRoutine(float duration)
    {
        // Safety Fallback: Build an array list to catch EVERY source found on the manager
        List<AudioSource> musicSourcesToFade = new List<AudioSource>();

        if (SoundManager.Instance != null)
        {
            // 🟩 THE SYSTEM AUDIO SEARCH MOTOR (FIXED):
            // 1. Snaps any source sitting on the root object
            // 2. Grabs any hidden sources tucked into child layers (e.g., BGM objects)
            // 3. Loops through them to find which one is actively playing your music clip!
            AudioSource[] allManagerSources = SoundManager.Instance.GetComponentsInChildren<AudioSource>(true);

            for (int i = 0; i < allManagerSources.Length; i++)
            {
                if (allManagerSources[i] != null && allManagerSources[i].isPlaying)
                {
                    musicSourcesToFade.Add(allManagerSources[i]);
                }
            }
        }

        // Ultimate Catch-All: If the manager list layout returned empty, sweep your scene camera 
        if (musicSourcesToFade.Count == 0)
        {
            AudioSource[] activeSceneAudio = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (int i = 0; i < activeSceneAudio.Length; i++)
            {
                // Only target sources playing music, skipping short sound effect spikes (SFX)
                if (activeSceneAudio[i].isPlaying && activeSceneAudio[i].loop)
                {
                    musicSourcesToFade.Add(activeSceneAudio[i]);
                }
            }
        }

        if (musicSourcesToFade.Count == 0)
        {
            Debug.LogWarning("[AUDIO FADE BREAK]: Could not find any active playing background AudioSource channels!");
            yield break;
        }

        // Cache the initial starting volume configuration states of all target channels
        List<float> startingVolumes = new List<float>();
        for (int i = 0; i < musicSourcesToFade.Count; i++)
        {
            startingVolumes.Add(musicSourcesToFade[i].volume);
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float timeRatio = elapsed / duration;

            // Fade EVERY matched active background music source simultaneously
            for (int i = 0; i < musicSourcesToFade.Count; i++)
            {
                if (musicSourcesToFade[i] != null)
                {
                    musicSourcesToFade[i].volume = Mathf.Lerp(startingVolumes[i], 0f, timeRatio);
                }
            }
            yield return null;
        }

        // 🔥 THE HARD LOCKDOWN: Cleanly clamp values to 0 and halt the physical tracks completely!
        for (int i = 0; i < musicSourcesToFade.Count; i++)
        {
            if (musicSourcesToFade[i] != null)
            {
                musicSourcesToFade[i].volume = 0f;
                musicSourcesToFade[i].Stop(); // This guarantees the music completely halts!
            }
        }

        Debug.Log($"[AUDIO] Successfully faded out and stopped ({musicSourcesToFade.Count}) active background track channels.");
        activeFadeRoutine = null;
    }

    // ========================================================================
    // 🟩 THE GRADUAL AUDIO RE-ACCELERATION INTERFACE (NEW):
    // Public access hook called automatically when exiting a silent zone!
    // Ensures the music starts playing again and scales up cleanly over time.
    // ========================================================================
    public void FadeInCurrentBGM(float duration, float targetVolume = 1.0f)
    {
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(SmoothBGMFadeInRoutine(duration, targetVolume));
    }

    private IEnumerator SmoothBGMFadeInRoutine(float duration, float targetVolume)
    {
        // Scan the scene to capture your playing audio device components directly
        AudioSource targetMusicSource = null;
        AudioSource[] activeSceneAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        for (int i = 0; i < activeSceneAudioSources.Length; i++)
        {
            if (activeSceneAudioSources[i] != null && activeSceneAudioSources[i].loop)
            {
                targetMusicSource = activeSceneAudioSources[i];
                break;
            }
        }

        if (targetMusicSource == null) yield break;

        // 🔥 THE WAKE UP GATE: If the source was stopped by a trigger zone, restore it cleanly!
        if (!targetMusicSource.isPlaying && stageBattleMusic != null)
        {
            targetMusicSource.clip = stageBattleMusic;
            targetMusicSource.volume = 0f;
            targetMusicSource.Play(); // ◄── Rewind and restart cleanly on exit!
        }

        float startVolume = targetMusicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            targetMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        targetMusicSource.volume = targetVolume;
        activeFadeRoutine = null;
    }
}
using UnityEngine;

public class StageMusicTrigger : MonoBehaviour
{
    [Header("Battle Theme Track")]
    [Tooltip("Drag the background music AudioClip you want to play for this stage here")]
    public AudioClip stageBattleMusic;

    void Start()
    {
        // Give the scene a split second to initialize, then blast the music
        Invoke("StartStageBGM", 0.1f);
    }

    private void StartStageBGM()
    {
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
}
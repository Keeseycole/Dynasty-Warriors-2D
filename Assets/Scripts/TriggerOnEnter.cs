using UnityEngine;
using System.Collections;

public class TriggerOnEnter : MonoBehaviour
{
    [Header("Target UI / Object Assignment")]
    [Tooltip("Drag the GameObject you want to turn ON or OFF here!")]
    public GameObject objToTrigger;

    [Header("Action Configuration Matrices")]
    public bool isTrue = true;
    public bool isFalse = false;
    [Tooltip("If checked, entering fades music volume to 0, and exiting fades it back to 1.")]
    public bool isVolume = false;

    [Header("Fade Tuning")]
    [Tooltip("How many seconds the fade in or out should take")]
    public float fadeDuration = 2.0f;

    private Coroutine activeFadeRoutine;

    public void OnTriggerEnter2D(Collider2D other)
    {
        // Tag Gate: Ensure only your main hero character activates this zone
        if (!other.CompareTag("Player")) return;

        if (objToTrigger != null)
        {
            if (isTrue) objToTrigger.SetActive(true);
            if (isFalse) objToTrigger.SetActive(false);
        }

        if (isVolume)
        {
            // Stop any fade that is currently running so they don't fight each other
            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = StartCoroutine(FadeMusicVolumeRoutine(0.0001f)); // Fade down to whisper quiet!
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (objToTrigger != null)
        {
            if (isTrue) objToTrigger.SetActive(false);
            if (isFalse) objToTrigger.SetActive(true);
        }

        if (isVolume)
        {
            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = StartCoroutine(FadeMusicVolumeRoutine(1.0f)); // Fade straight back up!
        }
    }

    // ========================================================================
    // 🟩 THE UNIFIED ARCADE CROSS-FADE SYSTEM (FIXED):
    // Finds whatever looping track is actively playing in the scene and smoothly
    // shifts its volume slider toward your target value without using .Stop()!
    // ========================================================================
    private IEnumerator FadeMusicVolumeRoutine(float targetVolume)
    {
        AudioSource activeMusicSource = null;
        AudioSource[] allSceneSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        // Scan the scene to capture the true looping music channel speaker
        for (int i = 0; i < allSceneSources.Length; i++)
        {
            if (allSceneSources[i] != null && allSceneSources[i].isPlaying && allSceneSources[i].loop)
            {
                activeMusicSource = allSceneSources[i];
                break;
            }
        }

        if (activeMusicSource == null)
        {
            Debug.LogWarning("[AUDIO FADE]: Could not locate an active, looping background music source.");
            yield break;
        }

        float startVolume = activeMusicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            // Smoothly calculate our frame-by-frame interpolation step
            activeMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / fadeDuration);
            yield return null;
        }

        // Hard lock the volume to the target value at the end of the timeline
        activeMusicSource.volume = targetVolume;
        activeFadeRoutine = null;
    }
}
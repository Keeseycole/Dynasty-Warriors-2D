using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BattleEndManager : MonoBehaviour
{
    public static BattleEndManager Instance { get; private set; }

    [Header("UI Cinematic Overlays")]
    [Tooltip("Drag and drop your full-screen 'ScreenFadeOverlay' Image component here.")]
    [SerializeField] private Image screenFadeOverlay;

    [Tooltip("Drag and drop your 'VictoryScreenPanel' GameObject container layer here.")]
    [SerializeField] private GameObject victoryScreenPanel;

    [Header("Cinematic Timing Tuning")]
    [Tooltip("How many seconds the game waits after the final commander dies before beginning the background fade-out.")]
    [SerializeField] private float cinematicDelayBeforeFade = 3.0f;

    [Tooltip("How many seconds it takes for the background screen to transition to complete black.")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Tooltip("How many seconds the Victory Card stays visible on screen in absolute darkness.")]
    [SerializeField] private float victoryCardHoldDuration = 3.0f;

    [Tooltip("How many seconds it takes for the STAGE CLEAR text card to fade out into absolute black.")]
    [SerializeField] private float textFadeOutDuration = 1.0f;

    private List<Health> activeStageCommanders = new List<Health>();
    private bool matchIsOver = false;
    private CanvasGroup victoryCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        matchIsOver = false;
        if (victoryScreenPanel != null)
        {
            victoryScreenPanel.SetActive(false);
            victoryCanvasGroup = victoryScreenPanel.GetComponent<CanvasGroup>();

            // 🔥 THE ALPHA INITIALIZATION SHIELD:
            // Forcefully lock the text alpha layers to 0 immediately on startup.
            // This guarantees it stays hidden until the background is pitch black!
            if (victoryCanvasGroup != null) victoryCanvasGroup.alpha = 0f;
        }

        if (screenFadeOverlay != null)
        {
            screenFadeOverlay.gameObject.SetActive(true);
            Color c = screenFadeOverlay.color;
            c.a = 0f;
            screenFadeOverlay.color = c;
        }

        Invoke("FindAllStageCommanders", 0.1f);
    }
    private void FindAllStageCommanders()
    {
        activeStageCommanders.Clear();
        Health[] allHealthScripts = FindObjectsByType<Health>(FindObjectsSortMode.None);

        foreach (Health h in allHealthScripts)
        {
            MusouUnit unit = h.GetComponent<MusouUnit>();
            if (unit != null && unit.isStageCommander && unit.unitTeam == MusouUnit.Team.EnemySide)
            {
                activeStageCommanders.Add(h);
            }
        }
    }

    public void NotifyCommanderDefeated(Health defeatedCommander)
    {
        if (matchIsOver) return;

        if (activeStageCommanders.Contains(defeatedCommander))
        {
            activeStageCommanders.Remove(defeatedCommander);
        }

        if (activeStageCommanders.Count <= 0)
        {
            StartCoroutine(AutomatedVictorySequenceCo());
        }
    }

    private IEnumerator AutomatedVictorySequenceCo()
    {
        if (matchIsOver) yield break;
        matchIsOver = true;

        // 1. SILENCE THE MUSIC IMMEDIATELY
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMusic();
            SoundManager.Instance.PlaySFX("FinalImpactClash", 1f);
        }

        // 2. RETRO SLOW-MOTION IMPACT
        Time.timeScale = 0.25f;
     
        // 3. POST-ATTACK DELAY: Let the death animations play out in slow-mo
        yield return new WaitForSecondsRealtime(cinematicDelayBeforeFade);

        victoryScreenPanel.SetActive(true);

        // 4. THE SMOOTH BACKGROUND BLACKOUT TRANSITION
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alphaProgress = Mathf.Clamp01(elapsedTime / fadeDuration);

            if (screenFadeOverlay != null)
            {
                Color c = screenFadeOverlay.color;
                c.a = alphaProgress;
                screenFadeOverlay.color = c;
            }

            yield return null;
        }

        if (screenFadeOverlay != null)
        {
            Color finalColor = screenFadeOverlay.color;
            finalColor.a = 1f;
            screenFadeOverlay.color = finalColor;
        }

        // =========================================================================
        // 🔥 5. THE PROGRESSIVE TEXT FADE-IN LOOP:
        // The background is now 100% black. Safely activate the panel container,
        // and softly fade the STAGE CLEAR text in from 0 to 1 over 0.5 seconds!
        // =========================================================================
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("StageClearTrumpet", 1f);
        }
    
         
      
        float textInTime = 0f;
        float textInDuration = 0.5f; // Fast, elegant pop-in
        while (textInTime < textInDuration)
        {
            textInTime += Time.unscaledDeltaTime;
            if (victoryCanvasGroup != null)
            {
                victoryCanvasGroup.alpha = Mathf.Clamp01(textInTime / textInDuration);
            }
            yield return null;
        }
        if (victoryCanvasGroup != null) victoryCanvasGroup.alpha = 1f; // Lock visible
        // =========================================================================

        // Hard freeze all remaining active action threads behind the text card panel
        Time.timeScale = 0f;

        // 6. HOLD THE CARD VISIBLE: Wait the normal victory screen wait time
        yield return new WaitForSecondsRealtime(victoryCardHoldDuration);

        // 7. THE PROGRESSIVE TEXT FADE-OUT LOOP:
        float textElapsedTime = 0f;
        while (textElapsedTime < textFadeOutDuration)
        {
            textElapsedTime += Time.unscaledDeltaTime;
            float textAlphaProgress = Mathf.Clamp01(textElapsedTime / textFadeOutDuration);

            if (victoryCanvasGroup != null)
            {
                victoryCanvasGroup.alpha = 1f - textAlphaProgress;
            }

            yield return null;
        }

        if (victoryCanvasGroup != null) victoryCanvasGroup.alpha = 0f;

        // Brief extra beat of pure cinematic silence in complete blackness before shifting scenes
        yield return new WaitForSecondsRealtime(0.6f);

        // 8. RESTORE PHYSICS TIME AND RETURN TO SELECT SCREEN ROSTER
        Time.timeScale = 1f;

        if (CharacterSelectManager.Instance != null)
        {
            CharacterSelectManager.Instance.LoadGameScene("CharacterSelect");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelect");
        }
    }
}
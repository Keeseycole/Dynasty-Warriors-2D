using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BattleEndManager : MonoBehaviour
{
    public static BattleEndManager Instance { get; private set; }

    [SerializeField] private Image screenFadeOverlay;

    [SerializeField] private GameObject victoryScreenPanel;

    [SerializeField] private GameObject defeatScreenPanel;

    [SerializeField] private float cinematicDelayBeforeFade = 3.0f;

    [SerializeField] private float fadeDuration = 1.5f;

    [SerializeField] private float victoryCardHoldDuration = 3.0f;

    [SerializeField] private float textFadeOutDuration = 1.0f;

    private List<Health> activeStageCommanders = new List<Health>();

    private bool matchIsOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        matchIsOver = false;
     
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

        // 5. REVEAL THE VICTORY TEXT CARD AND PLAY THE FANFARE
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("StageClearTrumpet", 1f);
        }
      
        // Hard freeze all remaining active action threads behind the text card panel
        Time.timeScale = .25f;
    

        // 6. HOLD THE CARD VISIBLE: Wait the normal victory screen wait time
        yield return new WaitForSecondsRealtime(victoryCardHoldDuration);


        // 🔥 7. THE PROGRESSIVE TEXT FADE-OUT LOOP:
        // Automatically fades out your text canvas elements while remaining in absolute background darkness!
        float textElapsedTime = 0f;
        while (textElapsedTime < textFadeOutDuration)
        {
            textElapsedTime += Time.unscaledDeltaTime; // Uses unscaledDeltaTime because timeScale is completely frozen (0f)
            float textAlphaProgress = Mathf.Clamp01(textElapsedTime / textFadeOutDuration);

            yield return null;
        }

        victoryScreenPanel.SetActive(false);

        // Brief extra beat of pure cinematic silence in complete blackness before shifting scenes
        yield return new WaitForSecondsRealtime(1f);

        // 8. RESTORE PHYSICS TIME AND RETURN TO SELECT SCREEN ROSTER
        Time.timeScale = 1f;

        if (CharacterSelectManager.Instance != null)
        {
            CharacterSelectManager.Instance.LoadGameScene("CharecterSelect");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CharecterSelect");
        }
    }

    public IEnumerator DefeatSequenceCo()
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

        defeatScreenPanel.SetActive(true);

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

        // 5. REVEAL THE VICTORY TEXT CARD AND PLAY THE FANFARE
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("StageClearTrumpet", 1f);
        }

        // Hard freeze all remaining active action threads behind the text card panel
        Time.timeScale = .25f;


        // 6. HOLD THE CARD VISIBLE: Wait the normal victory screen wait time
        yield return new WaitForSecondsRealtime(victoryCardHoldDuration);


        // 🔥 7. THE PROGRESSIVE TEXT FADE-OUT LOOP:
        // Automatically fades out your text canvas elements while remaining in absolute background darkness!
        float textElapsedTime = 0f;
        while (textElapsedTime < textFadeOutDuration)
        {
            textElapsedTime += Time.unscaledDeltaTime; // Uses unscaledDeltaTime because timeScale is completely frozen (0f)
            float textAlphaProgress = Mathf.Clamp01(textElapsedTime / textFadeOutDuration);

            yield return null;
        }

        defeatScreenPanel.SetActive(false);

        // Brief extra beat of pure cinematic silence in complete blackness before shifting scenes
        yield return new WaitForSecondsRealtime(1f);

        // 8. RESTORE PHYSICS TIME AND RETURN TO SELECT SCREEN ROSTER
        Time.timeScale = 1f;

        if (CharacterSelectManager.Instance != null)
        {
            CharacterSelectManager.Instance.LoadGameScene("CharecterSelect");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CharecterSelect");
        }
    }
}
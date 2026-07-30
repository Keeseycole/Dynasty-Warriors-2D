using System; // REQUIRED: Adds Action event support to communicate with reinforcement triggers
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusouDialogManager : MonoBehaviour
{
    public static MusouDialogManager Instance { get; private set; }

    // --- GLOBAL DIALOGUE COMPLETION BROADCASTER ---
    // Broadcasts the specific conversation asset that finished so unit spawners can listen for it!
    public static event Action<DialogConversation> OnConversationEnded;

    [Header("UI Elements")]
    public GameObject dialogPanel;
    public Text nameText;
    public Text messageText;
    public Image portraitImage;
    public Image borderHighlight;

    [Header("Settings")]
    public float displayDurationPerLine = 4.0f;
    public float textSpeedMultiplier = 0.02f;

    [Header("Faction Themes")]
    public Color playerSideColor = Color.blue;
    public Color enemySideColor = Color.red;
    public Color neutralColor = Color.gray;

    private Queue<DialogConversation.DialogLine> dialogQueue = new Queue<DialogConversation.DialogLine>();
    private bool isDisplayingLine = false;
    private Coroutine activeTypingCoroutine;

    // Track the active script asset currently running through the canvas queue layers
    private DialogConversation currentActiveConversationAsset;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dialogPanel != null) dialogPanel.SetActive(false);
    }

    // --- PUBLIC CALL TRIGGER FOR A WHOLE CONVERSATION ---
    public void PlayConversation(DialogConversation conversation)
    {
        if (conversation == null || conversation.lines.Count == 0) return;

        // Cache the reference before running line separation breakdowns
        currentActiveConversationAsset = conversation;

        // Load every line from the list into our execution queue buffer
        foreach (var line in conversation.lines)
        {
            dialogQueue.Enqueue(line);
        }

        if (!isDisplayingLine)
        {
            StartCoroutine(ProcessDialogQueueRoutine());
        }
    }

    private IEnumerator ProcessDialogQueueRoutine()
    {
        isDisplayingLine = true;
        if (dialogPanel != null) dialogPanel.SetActive(true);

        while (dialogQueue.Count > 0)
        {
            DialogConversation.DialogLine currentLine = dialogQueue.Dequeue();
            SetupUIForSpeaker(currentLine);

            if (!string.IsNullOrEmpty(currentLine.voiceSFXName) && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(currentLine.voiceSFXName, 1.0f);
            }
            if (activeTypingCoroutine != null) StopCoroutine(activeTypingCoroutine);
            activeTypingCoroutine = StartCoroutine(TypeTextRoutine(currentLine.dialogText));

            // ========================================================================
            // 🔥 FIXED DELAY CONTROLLER & PACE TRACKER: 
            // Evaluates custom read durations, falling back to global base timing if left at 0
            // ========================================================================
            float finalWaitTime = currentLine.customDuration > 0 ? currentLine.customDuration : displayDurationPerLine;
            yield return new WaitForSeconds(finalWaitTime); // BUG FIX: Removed second duplicate line wait statement right beneath this!
        }

        if (dialogPanel != null) dialogPanel.SetActive(false);
        isDisplayingLine = false;

        // ========================================================================
        // 🔥 BROADCAST END SIGNAL FRAME: 
        // Notifies all listening systems (like UnitReinforcementTrigger) instantly!
        // ========================================================================
        if (currentActiveConversationAsset != null)
        {
            OnConversationEnded?.Invoke(currentActiveConversationAsset);
            currentActiveConversationAsset = null; // Flush reference clear tracker
        }
    }

    private void SetupUIForSpeaker(DialogConversation.DialogLine data)
    {
        if (nameText != null) nameText.text = data.speakerName;
        if (portraitImage != null && data.characterPortrait != null)
        {
            portraitImage.sprite = data.characterPortrait;
            portraitImage.enabled = true;
        }
        else if (portraitImage != null)
        {
            portraitImage.enabled = false;
        }

        if (borderHighlight != null)
        {
            switch (data.alignment)
            {
                case DialogConversation.DialogLine.SpeakerSide.PlayerSide: borderHighlight.color = playerSideColor; break;
                case DialogConversation.DialogLine.SpeakerSide.EnemySide: borderHighlight.color = enemySideColor; break;
                default: borderHighlight.color = neutralColor; break;
            }
        }
    }

    private IEnumerator TypeTextRoutine(string textToType)
    {
        if (messageText == null) yield break;

        messageText.text = "";
        foreach (char letter in textToType.ToCharArray())
        {
            messageText.text += letter;
            yield return new WaitForSeconds(textSpeedMultiplier);
        }
    }
}
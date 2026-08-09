using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusouDialogManager : MonoBehaviour
{
    public static MusouDialogManager Instance;

    [Header("UI Canvas Component Anchors")]
    public GameObject dialogueUIPanel; // Main graphic panel background
    public UnityEngine.UI.Text speakerNameText;
    public UnityEngine.UI.Text dialogueBodyText;
    public UnityEngine.UI.Image portraitImage;

    private Coroutine activeDialogueThread;

    public static System.Action<DialogConversation> OnConversationEnded;

    private void Awake()
    {
        // ========================================================================
        // 🔥 THE DOMINANT SINGLETON SHIELD (FIXED):
        // Prevents your Dialogue Manager from accidentally killing itself on start!
        // It overrides any old data links to stay active in the scene hierarchy.
        // ========================================================================
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[DIALOG WARNING]: Detected a duplicate or ghost instance on object '{Instance.gameObject.name}'. Forcibly re-assigning instance authority to '{gameObject.name}'!");

            // Overwrite the bad reference link with this true child object component
            Instance = this;
        }

        // Safely hide the master UI backdrop graphic so it doesn't block the screen on load
        if (dialogueUIPanel != null)
        {
            dialogueUIPanel.SetActive(false);
        }
    }
    // ========================================================================
    // 🟩 CHANNEL A: THE MODERN NODE SYSTEM (Used by Gate Breached GameObject)
    // ========================================================================
    public void StartActiveNodeConversation(MusouConversationNode conversationNode)
    {
        if (activeDialogueThread != null) StopCoroutine(activeDialogueThread);
        activeDialogueThread = StartCoroutine(ProcessNodeSequence(conversationNode));
    }

    private IEnumerator ProcessNodeSequence(MusouConversationNode node)
    {
        if (dialogueUIPanel != null)
        {
            dialogueUIPanel.SetActive(true);
        }

        // Loop through each text line stored on the active game object container
        for (int i = 0; i < node.conversationScript.Count; i++)
        {
            MusouConversationNode.DialogueLine currentLine = node.conversationScript[i];

            // ========================================================================
            // 🔥 FIXED STRING PATHWAYS:
            // Ensure these properties match the variable names declared inside your
            // MusouConversationNode.cs DialogueLine struct exactly!
            // ========================================================================
            if (speakerNameText != null)
                speakerNameText.text = currentLine.speakerName;

            if (dialogueBodyText != null)
                dialogueBodyText.text = currentLine.dialogueText; // ◄── Verify this name matches your Node script!

            if (portraitImage != null && currentLine.speakerPortrait != null)
            {
                portraitImage.sprite = currentLine.speakerPortrait;
                portraitImage.enabled = true;
            }
            else if (portraitImage != null)
            {
                portraitImage.enabled = false;
            }

            float waitTime = currentLine.displayDuration > 0.5f ? currentLine.displayDuration : 3.5f;
            yield return new WaitForSeconds(waitTime);
        }

        if (dialogueUIPanel != null)
        {
            dialogueUIPanel.SetActive(false);
        }

        DialogConversation completedAsset = node.legacyDialogueAsset;
        InvokeLegacyEndEvent(completedAsset);

        node.CompleteConversation();
        yield return null;
    }

    public void PlayConversation(DialogConversation oldConversationData)
    {
        if (oldConversationData == null) return;

        Debug.Log($"[DIALOG SYSTEM]: Initiating Direct ScriptableObject Stream for '{oldConversationData.name}'...");

        // 1. Kill any background threads to prevent overlap text conflicts
        if (activeDialogueThread != null) StopCoroutine(activeDialogueThread);

        // 2. Launch our safe, rock-solid asset stream coroutine
        activeDialogueThread = StartCoroutine(ProcessDirectAssetSequence(oldConversationData));
    }

    private IEnumerator ProcessDirectAssetSequence(DialogConversation assetData)
    {
        // Safety verification check to protect from empty assets crashing the build
        if (assetData == null || assetData.lines == null || assetData.lines.Count == 0)
        {
            Debug.LogError($"[DIALOG CRITICAL ERROR]: '{assetData.name}' contains 0 active text lines inside its script file!");
            yield break;
        }

        // Force the UI backdrop container to snap open on screen instantly!
        if (dialogueUIPanel != null)
        {
            dialogueUIPanel.SetActive(true);
        }

        // Loop directly through your pre-saved ScriptableObject line structures
        for (int i = 0; i < assetData.lines.Count; i++)
        {
            DialogConversation.DialogLine currentLine = assetData.lines[i];

            // Assign characters directly to UI text meshes safely
            if (speakerNameText != null) speakerNameText.text = currentLine.speakerName;
            if (dialogueBodyText != null) dialogueBodyText.text = currentLine.dialogText; // Maps to dialogText

            // Handle portrait assignments cleanly
            if (portraitImage != null && currentLine.characterPortrait != null)
            {
                portraitImage.sprite = currentLine.characterPortrait;
                portraitImage.enabled = true;
            }
            else if (portraitImage != null)
            {
                portraitImage.enabled = false;
            }

            // 🔥 THE SOLID VISIBILITY LOCK:
            // Evaluate custom duration data. If left at 0, hold on screen for 4.0 seconds!
            float waitTime = currentLine.customDuration > 0.1f ? currentLine.customDuration : 4.0f;

            Debug.Log($"[DIALOGUE TIMING]: Displaying line {i + 1}/{assetData.lines.Count}. Waiting for {waitTime} seconds...");

            // Forcibly freeze this coroutine thread right here on screen!
            yield return new WaitForSeconds(waitTime);
        }

        // Close the panel frame graphics now that all rows finished displaying cleanly
        if (dialogueUIPanel != null)
        {
            dialogueUIPanel.SetActive(false);
        }

        // Wake up your older legacy listeners passing this asset payload
        InvokeLegacyEndEvent(assetData);

        yield return null;
    }

    public void InvokeLegacyEndEvent(DialogConversation assetPayload)
    {
        if (OnConversationEnded != null)
        {
            OnConversationEnded.Invoke(assetPayload);
        }
    }
}
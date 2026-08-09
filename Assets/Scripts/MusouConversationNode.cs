using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MusouConversationNode : MonoBehaviour
{
    [Header("Scriptable Object Source")]
    [Tooltip("Drag your pre-made DialogConversation Scriptable Object asset here!")]
    public DialogConversation legacyDialogueAsset;

    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        [TextArea(2, 5)] public string dialogueText;
        public Sprite speakerPortrait;
        public float displayDuration;
    }

    [HideInInspector]
    public List<DialogueLine> conversationScript = new List<DialogueLine>();

    [Header("Automation Settings")]
    public bool playOnEnable = true;

    [Header("Post-Dialogue Event Hooks")]
    public UnityEvent onConversationComplete;

    // ❌ REMOVE THE AWAKE FUNCTION ENTIRELY!
    // Processing this in Awake means it runs when the object is initialized asleep,
    // which results in empty data buffers on load.

    private void OnEnable()
    {
        // 🔥 FORCE THE FRESH CONVERSION NOW:
        // Build out our subtitle lines the exact split second this object is activated!
        ConvertAssetToRuntimeScript();

        if (playOnEnable && conversationScript.Count > 0)
        {
            TriggerConversationSequence();
        }
        else
        {
            Debug.LogWarning($"[CONVERSATION WARNING]: '{gameObject.name}' turned on, but conversationScript lines count is 0!");
        }
    }

    private void ConvertAssetToRuntimeScript()
    {
        if (legacyDialogueAsset == null) return;
        conversationScript.Clear();

        foreach (var oldLine in legacyDialogueAsset.lines)
        {
            DialogueLine newLine = new DialogueLine();

            newLine.speakerName = oldLine.speakerName;
            newLine.dialogueText = oldLine.dialogText; // ◄── Grabs "dialogText" from SO and saves it to "dialogueText"
            newLine.speakerPortrait = oldLine.characterPortrait;
            newLine.displayDuration = oldLine.customDuration > 0.1f ? oldLine.customDuration : 3.5f;

            conversationScript.Add(newLine);
        }
    }

    public void TriggerConversationSequence()
    {
        // ========================================================================
        // 🔥 THE SPECIFIC MANAGER CHILD RADAR (FIXED):
        // Searches directly for the 'Dialogue Manager' child object sitting inside
        // your separate 'Managers' container folder to extract the component script!
        // ========================================================================
        if (MusouDialogManager.Instance == null)
        {
            // 1. Direct path search down the folder hierarchy lines
            GameObject managersContainer = GameObject.Find("Managers");
            if (managersContainer != null)
            {
                // Find the exact child object named "Dialogue Manager" inside the folder!
                Transform childTransform = managersContainer.transform.Find("Dialogue Manager");
                if (childTransform != null)
                {
                    MusouDialogManager.Instance = childTransform.GetComponent<MusouDialogManager>();
                }
            }

            // 2. Fallback search if the parent container name changes
            if (MusouDialogManager.Instance == null)
            {
                GameObject standaloneObj = GameObject.Find("Dialogue Manager");
                if (standaloneObj != null)
                {
                    MusouDialogManager.Instance = standaloneObj.GetComponent<MusouDialogManager>();
                }
            }

            // 3. Ultimate broad search fail-safe
            if (MusouDialogManager.Instance == null)
            {
                MusouDialogManager.Instance = FindFirstObjectByType<MusouDialogManager>(FindObjectsInactive.Include);
            }
        }

        if (MusouDialogManager.Instance != null)
        {
            // Connection verified! Stream your new fire attack text fields onto the screen canvas
            MusouDialogManager.Instance.StartActiveNodeConversation(this);
        }
        else
        {
            Debug.LogError("[MANAGER CRITICAL BREAK]: Could not find your 'MusouDialogManager' script component anywhere on the 'Dialogue Manager' child object!");
        }
    }

    public void CompleteConversation()
    {
        if (MusouDialogManager.OnConversationEnded != null && legacyDialogueAsset != null)
        {
            MusouDialogManager.OnConversationEnded.Invoke(legacyDialogueAsset);
        }

        if (onConversationComplete != null)
        {
            onConversationComplete.Invoke();
        }

        gameObject.SetActive(false);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitReinforcementTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Which dialogue conversation must finish to unlock this object?")]
    [SerializeField] private DialogConversation targetedConversation;

    [Header("Simple Activation Target")]
    [Tooltip("Drag the game object (or parent folder container) from your Hierarchy that should turn true.")]
    [SerializeField] private GameObject objectToActivate;

    [Header("Timing")]
    [Tooltip("How many seconds to wait after the conversation ends before turning the object on.")]
    [SerializeField] private float activationDelay = 0.5f;

    private void OnEnable()
    {
        MusouDialogManager.OnConversationEnded += CheckDialogueTrigger;
    }

    private void OnDisable()
    {
        MusouDialogManager.OnConversationEnded -= CheckDialogueTrigger;
    }

    private void CheckDialogueTrigger(DialogConversation completedConversation)
    {
        // Only fire if the conversation that just ended matches our targeted slot
        if (completedConversation == targetedConversation)
        {
            StartCoroutine(ExecuteActivationSequence());
        }
    }
    private IEnumerator ExecuteActivationSequence()
    {
        if (objectToActivate == null) yield break;

        // Wait out your preferred breathing room delay window
        yield return new WaitForSeconds(activationDelay);

        // 🔥 THE SINGLE POINT OF ACTION:
        // Turns your hierarchy asset on cleanly with no extra overhead or tracking attachments!
        objectToActivate.SetActive(true);

        Debug.Log($"[ACTIVATOR] Successfully set {objectToActivate.name} to true following dialogue completion!");

        // Safely turn off this trigger object since its single hand-off task is finished
        gameObject.SetActive(false);
    }
} // Final closing class bracket
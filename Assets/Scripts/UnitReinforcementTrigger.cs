using UnityEngine;

public class UnitReinforcementTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Which dialogue asset must finish before these units activate?")]
    [SerializeField] private DialogConversation targetedConversation;

    [Header("Target Units to Activate")]
    [Tooltip("Drag the game objects (or parent folders of units) here that start deactivated.")]
    [SerializeField] private GameObject[] unitsToActivate;

    private void OnEnable()
    {
        // Subscribe to the global dialogue manager completion signal
        MusouDialogManager.OnConversationEnded += CheckDialogueTrigger;
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks and dangling pointer crashes
        MusouDialogManager.OnConversationEnded -= CheckDialogueTrigger;
    }

    private void CheckDialogueTrigger(DialogConversation completedConversation)
    {
        // If the conversation that just ended matches our targeted script asset...
        if (completedConversation == targetedConversation)
        {
            ActivateReinforcements();
        }
    }

    private void ActivateReinforcements()
    {
        if (unitsToActivate == null || unitsToActivate.Length == 0) return;

        foreach (GameObject unit in unitsToActivate)
        {
            if (unit != null)
            {
                unit.SetActive(true); // Wake them up physically on the map layout!

                // Optional: Play a spawn particle effect, sound effect, or command 
                // their MusouUnit AI component to instantly march to a new position.
            }
        }

        Debug.Log($"<color=cyan>[REINFORCEMENTS]:</color> Successfully activated {unitsToActivate.Length} units following conversation completion!");

        // Disable this trigger object since its ambush sequence is complete
        gameObject.SetActive(false);
    }
}
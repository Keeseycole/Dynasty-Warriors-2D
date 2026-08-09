using UnityEngine;
using UnityEngine.Events;

public class DialogZoneTrigger : MonoBehaviour
{
    [Header("Target Activation Settings")]
    [Tooltip("Drag and drop your 'Gate Breached' (or any other conversation node) GameObject here from the hierarchy!")]
    // 🔥 This matches your gate system exactly by pointing directly to the working scene GameObject!
    public GameObject dialogueObjectToActivate;

    [Header("Automation Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Dynamic Sensor Settings")]
    [TagProperty]
    public string targetTag = "Player";

    [Header("Post-Trigger Automation")]
    [Tooltip("Any extra game events you want to run the exact frame you step into this zone.")]
    public UnityEvent onZoneTriggered;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnlyOnce) return;

        // Check if the character stepping inside matches our selected target tag dropdown selection
        if (other.CompareTag(targetTag) || other.transform.root.CompareTag(targetTag))
        {
            if (dialogueObjectToActivate != null)
            {
                // 🔥 THE EXACT ARCHITECTURE REPLICATOR:
                // This turns ON your modern dialogue node GameObject inside the hierarchy!
                // It will act EXACTLY like your gate script just blew open.
                dialogueObjectToActivate.SetActive(true);

                hasTriggered = true;

                // Fire off any secondary custom actions wired in this specific trigger box
                if (onZoneTriggered != null)
                {
                    onZoneTriggered.Invoke();
                }

                if (triggerOnlyOnce)
                {
                    // Shut down this trigger zone box instantly so you can't step in it twice
                    gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogError($"[TRIGGER ERROR]: '{gameObject.name}' was stepped in, but 'dialogueObjectToActivate' is completely empty inside the Inspector!");
            }
        }
    }
}
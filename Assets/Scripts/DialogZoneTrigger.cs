using UnityEngine;

public class DialogZoneTrigger : MonoBehaviour
{
    // UPDATED: Now uses DialogConversation to support full lists of back-and-forth lines!
    [SerializeField] private DialogConversation conversationToTrigger;
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnlyOnce) return;

        if (other.CompareTag("Player"))
        {
            // FIXED: Calls PlayConversation instead of EnqueueDialog to match your system upgrade!
            if (conversationToTrigger != null && MusouDialogManager.Instance != null)
            {
                MusouDialogManager.Instance.PlayConversation(conversationToTrigger);
                hasTriggered = true;

                if (triggerOnlyOnce)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}

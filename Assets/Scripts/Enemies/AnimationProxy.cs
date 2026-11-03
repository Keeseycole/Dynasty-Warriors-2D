using UnityEngine;

public class AnimationProxy : MonoBehaviour
{
    private MusouUnit parentUnit;
    private PlayerController parentPlayer;

    public void UnlockComboWindow() => GetComponentInParent<PlayerCombo>()?.UnlockComboWindow();
    public void ClearComboLockOnAnimationEnd() => GetComponentInParent<PlayerCombo>()?.ClearComboLockOnAnimationEnd();

    void Awake()
    {
        parentUnit = GetComponentInParent<MusouUnit>();

        if (parentUnit == null)
        {
            parentPlayer = GetComponentInParent<PlayerController>();
        }
    }

    public void ApplyDamageToTarget()
    {
        if (parentUnit != null)
        {
            parentUnit.ApplyDamageToTarget();
        }
        else if (parentPlayer != null)
        {
            parentPlayer.ApplyDamageToTarget();
        }
        else
        {
            Debug.LogWarning($"AnimationProxy on {gameObject.name} has no valid Player or AI parent script component attached!");
        }
    }

    public void TriggerFootstepFromAnimationEvent()
    {
        if (SoundManager.Instance == null) return;

        // Use a slight negative vertical offset to check the ground directly below the character pivot anchor point
        Vector2 checkOrigin = (Vector2)transform.position + new Vector2(0f, -0.2f);
        float sensorRadius = 0.2f;

   
        Collider2D hitFloor = Physics2D.OverlapCircle(checkOrigin, sensorRadius);

        string activeSurfaceTag = "Dirt"; // Fallback baseline

        if (hitFloor != null)
        {
            activeSurfaceTag = hitFloor.gameObject.tag;

            // OPTIONAL DIAGNOSTIC DEBUG TELEMETRY: 
            // Un-comment this next line if you want your console to print out exactly what it senses!
            // Debug.Log($"<color=yellow>[FOOTSTEP] Physics stamped collider '{hitFloor.gameObject.name}' with Tag: '{activeSurfaceTag}'</color>");
        }

        SoundManager.Instance.PlayFootstepSFX(activeSurfaceTag);
    }
}

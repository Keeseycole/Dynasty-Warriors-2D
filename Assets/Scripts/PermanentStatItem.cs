using UnityEngine;

public class PermanentStatItem : MonoBehaviour
{
    public enum BoostType { MaxHealth, AttackPower, DefensePower }

    [Header("Stat Boost Configurations")]
    [Tooltip("Select what stat this specific item asset will permanently upgrade!")]
    public BoostType itemType = BoostType.MaxHealth;

    [Tooltip("How much this item adds to the character's stats permanently (e.g., +10 HP, +2 Attack)")]
    public float upgradeAmount = 10f;

    [Header("Visual Effects")]
    [SerializeField] private string pickupSFXName = "statBoost";
    [SerializeField] private GameObject pickupParticlePrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 🔥 THE HERO DETECTOR GATEWAY:
        // Verify if the object overlapping our collider box is the playable hero!
        if (collision.CompareTag("Player") || collision.transform.root.CompareTag("Player"))
        {
            // 1. Fetch the persistent character data profile file
            CharacterData activeHeroData = CharacterSelectManager.Instance?.GetSelectedCharacter();
            if (activeHeroData == null) return;

            // 2. Dispatch the permanent upgrade based on the selected enum type
            switch (itemType)
            {
                case BoostType.MaxHealth:
                    PlayerHealth pHealth = collision.GetComponentInParent<PlayerHealth>();
                    if (pHealth == null) pHealth = collision.GetComponent<PlayerHealth>();

                    if (pHealth != null)
                    {
                        // 🟢 SAFEGUARD: Accumulate the health bonus in the ScriptableObject asset file too!
                        activeHeroData.permanentHealthBonus += upgradeAmount;

                        float universalMaxMenuCap = 500f;
                        pHealth.PermanentHealthUpgrade(upgradeAmount, universalMaxMenuCap);
                    }
                    break;

                case BoostType.AttackPower:
                    // 🟢 FIXED: Add the upgrade points directly to the persistent bonus holder variable!
                    activeHeroData.permanentAttackBonus += upgradeAmount;

                    // Sync up live stats with the controller on the active scene frame
                    PlayerController pControl = collision.GetComponentInParent<PlayerController>();
                    if (pControl == null) pControl = collision.GetComponent<PlayerController>();

                    if (pControl != null)
                    {
                        // Force the live damage variable to update using the newly calculated property total
                        pControl.playerAttackDamage = activeHeroData.attackPower;
                    }

                    Debug.Log($"<color=#00FF7F>[PERMANENT UPGRADE]:</color> Attack power permanently boosted! New Total: {activeHeroData.attackPower}");
                    break;

                case BoostType.DefensePower:
                    // 🟢 FIXED: Add the upgrade points directly to the persistent bonus holder variable!
                    activeHeroData.permanentDefenseBonus += upgradeAmount;

                    Debug.Log($"<color=#00FF7F>[PERMANENT UPGRADE]:</color> Defense power permanently boosted! New Total: {activeHeroData.defensePower}");
                    break;
            }

            // 3. Audio-Visual Feedback Effects
            if (SoundManager.Instance != null && !string.IsNullOrEmpty(pickupSFXName))
            {
                SoundManager.Instance.PlaySFX(pickupSFXName, 0.9f);
            }

            if (pickupParticlePrefab != null)
            {
                Instantiate(pickupParticlePrefab, transform.position, Quaternion.identity);
            }

            // 4. Destroy the pickable prop cleanly from the active scene grid coordinates
            Destroy(gameObject);
        }
    }
}
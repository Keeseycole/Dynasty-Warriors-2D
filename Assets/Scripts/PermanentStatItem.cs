using UnityEngine;

public class PermanentStatItem : MonoBehaviour
{
    public enum BoostType { MaxHealth, AttackPower, DefensePower, MusouCapacity }

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
                        float maximumAllowedHealthCeiling = 150f;

                        // 🌟 VISUAL ONLY PICKUP: 
                        // If already at or above 150, log it and let the item be consumed without adding points!
                        if (activeHeroData.maxHealth >= maximumAllowedHealthCeiling)
                        {
                            Debug.Log("<color=yellow>[MAX CAPACITY PICKUP]:</color> Health is already maxed at 150. Item consumed visually only.");
                        }
                        else
                        {
                            float theoreticalNewHealth = activeHeroData.maxHealth + upgradeAmount;
                            if (theoreticalNewHealth > maximumAllowedHealthCeiling)
                            {
                                theoreticalNewHealth = maximumAllowedHealthCeiling;
                            }

                            float actualAllowedHealthUpgrade = theoreticalNewHealth - activeHeroData.maxHealth;

                            if (actualAllowedHealthUpgrade > 0)
                            {
                                activeHeroData.permanentHealthBonus += actualAllowedHealthUpgrade;
                                pHealth.PermanentHealthUpgrade(actualAllowedHealthUpgrade, maximumAllowedHealthCeiling);
                            }
                        }
                    }
                    break;

                case BoostType.AttackPower:
                    activeHeroData.permanentAttackBonus += upgradeAmount;
                    PlayerController pControl = collision.GetComponentInParent<PlayerController>();
                    if (pControl == null) pControl = collision.GetComponent<PlayerController>();
                    if (pControl != null) pControl.playerAttackDamage = activeHeroData.attackPower;
                    Debug.Log($"<color=#00FF7F>[PERMANENT UPGRADE]:</color> Attack power permanently boosted! New Total: {activeHeroData.attackPower}");
                    break;

                case BoostType.DefensePower:
                    activeHeroData.permanentDefenseBonus += upgradeAmount;
                    Debug.Log($"<color=#00FF7F>[PERMANENT UPGRADE]:</color> Defense power permanently boosted! New Total: {activeHeroData.defensePower}");
                    break;

                case BoostType.MusouCapacity:
                    PlayerCombo pCombo = collision.GetComponentInParent<PlayerCombo>();
                    if (pCombo == null) pCombo = collision.GetComponent<PlayerCombo>();

                    if (pCombo != null)
                    {
                        float maximumAllowedMusouCeiling = 200f;

                        // 🌟 VISUAL ONLY PICKUP: 
                        // If already at or above 150, log it and let the item be consumed without adding points!
                        if (activeHeroData.maxMusouCapacity >= maximumAllowedMusouCeiling)
                        {
                            Debug.Log("<color=yellow>[MAX CAPACITY PICKUP]:</color> Musou is already maxed at 150. Item consumed visually only.");
                        }
                        else
                        {
                            float theoreticalNewCapacity = activeHeroData.maxMusouCapacity + upgradeAmount;
                            if (theoreticalNewCapacity > maximumAllowedMusouCeiling)
                            {
                                theoreticalNewCapacity = maximumAllowedMusouCeiling;
                            }

                            float actualAllowedUpgradeAmount = theoreticalNewCapacity - activeHeroData.maxMusouCapacity;

                            if (actualAllowedUpgradeAmount > 0)
                            {
                                activeHeroData.permanentMusouBonus += actualAllowedUpgradeAmount;
                                pCombo.maxMusouEnergy = activeHeroData.maxMusouCapacity;

                                UnityEngine.UI.Slider mSlider = pCombo.GetComponentInChildren<UnityEngine.UI.Slider>();
                                if (mSlider != null) mSlider.maxValue = pCombo.maxMusouEnergy;

                                pCombo.currentMusouEnergy = pCombo.currentMusouEnergy;
                            }
                        }
                    }
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
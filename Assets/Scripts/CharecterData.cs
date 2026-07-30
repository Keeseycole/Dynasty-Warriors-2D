using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character/Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    [Tooltip(" name text ")]
    public string characterTitle;
    public RuntimeAnimatorController animatorController;

    [Header(" Roster Visuals")]
    public Sprite gridIcon;         // Small square face image for the selection grid
    public Sprite massivePreview;   // Huge vertical half-body cutout artwork

    // 🟢 CHANGED: These now act as your starting "Level 1" defaults!
    [Header(" Starting Level 1 Stats")]
    [Range(0, 250)][SerializeField] private float baseAttackPower = 100f;
    [Range(0, 250)][SerializeField] private float baseDefensePower = 100f;
    [Range(0, 250)][SerializeField] private float baseMaxHealth = 100f;
    [Range(0, 200)][SerializeField] private float baseMaxMusou = 100f;

    [Header("Combat Profile")]
    [Tooltip("The unique weapon reach for this character. Spear/Staff users should have higher values, while sword users have smaller values.")]
    [Range(0.5f, 4f)]
    public float uniqueAttackRadius = 1.5f;

    [Header("Movement Metrics")]
    [Tooltip("How fast this specific officer runs across the battlefield grid")]
    public float moveSpeed = 5f;

    // 🔥 THE PERSISTENT BONUS STAT BLOCKS:
    // These hidden variables accumulate all items collected mid-battle permanently!
    [Header("Saves Permanently (Do Not Edit)")]
    public float permanentHealthBonus = 0f;
    public float permanentAttackBonus = 0f;
    public float permanentDefenseBonus = 0f;
    public float permanentMusouBonus = 0f;


    // 🔥 THE COMPREHENSIVE MATHEMATICAL PROPERTY WRAPPERS:
    // These add your permanent bonuses straight to your base numbers.
    // This is what your UI and Spawner scripts read so stats match up perfectly!
    public float maxHealth => baseMaxHealth + permanentHealthBonus;
    public float attackPower => baseAttackPower + permanentAttackBonus;
    public float defensePower => baseDefensePower + permanentDefenseBonus;

    public float maxMusouCapacity => Mathf.Min(baseMaxMusou + permanentMusouBonus);

    // 🟢 DEVELOPMENT TOOL: Call this to completely wipe progress back to level 1 defaults
    public void ResetCharacterProgression()
    {
        permanentHealthBonus = 0f;
        permanentAttackBonus = 0f;
        permanentDefenseBonus = 0f;
        permanentMusouBonus = 0f;
    }
}
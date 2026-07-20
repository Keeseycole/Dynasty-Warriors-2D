using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Musou Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    [Tooltip(" name text ")]
    public string characterTitle;
    public RuntimeAnimatorController animatorController;

    [Header(" Roster Visuals")]
    public Sprite gridIcon;         // Small square face image for the selection grid
    public Sprite massivePreview;   // Huge vertical half-body cutout artwork

    [Header(" Stat Bars")]
    [Range(0, 500)] public float attackPower = 100f;
    [Range(0, 500)] public float defensePower = 100f;
    [Range(0, 500)] public float maxHealth = 100f;
}
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Stats/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public int maxHealth;
    public int attack;
    public int defense;

}

using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogLine", menuName = "Musou System/Dialog Line")]
public class DialogData : ScriptableObject
{
    public string speakerName;
    [TextArea(3, 5)] public string dialogText;
    public Sprite characterPortrait;

    public enum SpeakerSide { PlayerSide, EnemySide, Neutral }
    public SpeakerSide alignment;

    [Tooltip("Optional: Sound effect clip name to play from SoundManager when this line pops up")]
    public string voiceSFXName;
}
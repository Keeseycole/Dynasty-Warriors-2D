using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewConversation", menuName = "Musou System/Dialog Conversation")]
public class DialogConversation : ScriptableObject
{
    [System.Serializable]
    public struct DialogLine
    {
        public string speakerName;
        [TextArea(3, 5)] public string dialogText;
        public Sprite characterPortrait;

        public enum SpeakerSide { PlayerSide, EnemySide, Neutral }
        public SpeakerSide alignment;
        public string voiceSFXName;

        // 🔥 FIXED: This variable now sits perfectly inside the working struct template!
        [Tooltip("How many seconds this specific line will stay visible on screen. Leave at 0 to use global default.")]
        public float customDuration;
    }

    [Tooltip("The sequential list of dialogue lines for this conversation scene.")]
    public List<DialogLine> lines = new List<DialogLine>();
}
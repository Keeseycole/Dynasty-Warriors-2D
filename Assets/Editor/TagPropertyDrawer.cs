using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(TagPropertyAttribute))]
public class TagPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginChangeCheck();

            // Generate the official Unity project tag selector dropdown list
            string tagValue = EditorGUI.TagField(position, label, property.stringValue);

            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = tagValue;
            }
        }
        else
        {
            // Fallback warning if attached to something that isn't a text string variable
            EditorGUI.LabelField(position, label.text, "Use [TagProperty] only on strings.");
        }
    }
}
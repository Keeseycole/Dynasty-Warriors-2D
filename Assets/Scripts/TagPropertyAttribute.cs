using UnityEngine;

/// <summary>
/// A property attribute that automatically converts a standard string inspector
/// field into a clean, selectable drop-down listing Unity's active project tags.
/// </summary>
public class TagPropertyAttribute : PropertyAttribute
{
    // Keeping this class completely clean and empty is standard practice.
    // Unity's underlying PropertyDrawer engine handles the asset mapping behind the scenes!
}

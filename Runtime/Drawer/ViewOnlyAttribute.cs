#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// NOTE: not work with fields that already have a CustomPropertyDrawer.
/// CuongNT
/// </summary>
public class ViewOnlyAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ViewOnlyAttribute))]
public class ViewOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
        GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
#endif
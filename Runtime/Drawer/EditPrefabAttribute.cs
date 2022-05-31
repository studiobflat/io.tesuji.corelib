
using UnityEngine;
using System;
using System.Linq;
using Object = UnityEngine.Object;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using PropType = EditPrefabAttribute.PropType;
#endif

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class EditPrefabAttribute : PropertyAttribute
{
	public string prefabNameField;
	public EditPrefabAttribute(string prefabIdFieldName = "viewPrefabId")
	{
		this.prefabNameField = string.IsNullOrEmpty(prefabIdFieldName) ? "prefabIdFieldName" : prefabIdFieldName;
	}

	#if UNITY_EDITOR
	public enum PropType
	{
		None,
		GameObject,
		Component,
		Unknown
	}

	public PropType type;
	
	// CACHED DATA
	public string prefabName;
	public string assetPath;
	public bool isAttached;
	public GameObject prefab;
	
	string ResourcePath2AssetPath(string path)
	{
		var allPath = AssetDatabase.GetAllAssetPaths();
		foreach (var p in allPath)
		{
			if (p.Contains("/Resources/" + path + ".prefab")) return p;
		}

		return null;
	}

	public void Refresh(SerializedProperty sp)
	{
		var propType = sp.propertyType;
		if (propType == SerializedPropertyType.ObjectReference)
		{
			if (sp.type == "PPtr<$GameObject>")
			{
				type = PropType.GameObject;
			}
			else
			{
				type = PropType.Component;
			}
		}
		else
		{
			type = PropType.Unknown;
		}

		if (!string.IsNullOrEmpty(prefabNameField))
		{
			var so = sp.serializedObject;
			var p2 = sp.propertyPath.Substring(0, sp.propertyPath.LastIndexOf('/') + 1);
			var pnf = so.FindProperty(p2 + prefabNameField);
			if (pnf != null)
			{
				prefabName = pnf.stringValue;
				// Debug.Log("Found! " + prefabNameField + " --> " + prefabName);
			}
		}

		if (sp.objectReferenceValue != null)
		{
			FindAssetPath();
			prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
		}
		else
		{
			prefab = null;
			assetPath = null;
		}
	}

	public void FindAssetPath()
	{
		if (!string.IsNullOrEmpty(assetPath)) return;
		assetPath = ResourcePath2AssetPath(prefabName);

		if (string.IsNullOrEmpty(assetPath))
		{
			Debug.LogWarning("Not found: " + prefabName);
		}
	}
	#endif
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(EditPrefabAttribute))]
public class EditPrefabAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var pa = (EditPrefabAttribute)attribute;
		if (pa.type == PropType.None)
		{
			pa.Refresh(property);
		}

		EditorGUI.BeginProperty(position, label, property);
		{
			if (pa.type == PropType.Unknown)
			{
				EditorGUI.HelpBox(position, "Unsupport: " + property.type, MessageType.Warning);
			}
			else
			{
				if (pa.prefab == null)
				{

				}

				if (property.objectReferenceValue == null) // not yet loaded
				{
					if (GUI.Button(position, "Edit <" + pa.prefabName + ".prefab>"))
					{
						pa.Refresh(property);
						if (string.IsNullOrEmpty(pa.assetPath)) 
						{
							pa.FindAssetPath();
						}
						if (string.IsNullOrEmpty(pa.assetPath)) return;

						pa.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pa.assetPath);
						if (pa.prefab == null) return;

						var comp = (Component)property.serializedObject.targetObject;
						var clone = (GameObject)PrefabUtility.InstantiatePrefab(pa.prefab);
						clone.hideFlags = HideFlags.DontSave;
						clone.transform.SetParent(comp.transform, false);
						property.objectReferenceValue = clone;
						property.serializedObject.ApplyModifiedProperties();
					}
				}
				else
				{
					var rect1 = position;
					rect1.width /= 2f;

					EditorGUI.ObjectField(rect1, pa.prefab, typeof(GameObject), false);

					var rect2 = position;
					rect2.xMin += rect1.width;

					if (GUI.Button(rect2, "Apply & Unload"))
					{
						var go = (pa.type == PropType.GameObject)
							? (GameObject)property.objectReferenceValue
							: ((Component)property.objectReferenceValue).gameObject;

						go.hideFlags = HideFlags.None;
						PrefabUtility.SaveAsPrefabAsset(go, pa.assetPath);
						Object.DestroyImmediate(go);

						property.objectReferenceValue = null;
						property.serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}
		EditorGUI.EndProperty();
	}
}

#endif
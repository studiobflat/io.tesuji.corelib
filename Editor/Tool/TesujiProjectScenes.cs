using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable] public class SceneConfig
{
	public string id;
	public List<SceneAsset> listScenes = new List<SceneAsset>();
}

[CreateAssetMenu] public class TesujiProjectScenes : ScriptableObject
{
	public List<SceneAsset> common = new List<SceneAsset>();
	public SceneConfig[] configs;
	[HideInInspector] public int activeIndex;
	
	public void Apply()
	{
		var list = new List<SceneAsset>();
		list.AddRange(common);
		list.AddRange(configs[activeIndex].listScenes);
		
		EditorBuildSettings.scenes = list.Select(
			item=> new EditorBuildSettingsScene(AssetDatabase.GetAssetPath(item), true)
		).ToArray();
	}
}

[CustomEditor(typeof(TesujiProjectScenes))]
public class TesujiProjectScenesEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (target == null) return;
		var ps = (TesujiProjectScenes) target;
		if (ps == null) return;
		if (ps.configs.Length <= 1) return;

		GUILayout.BeginHorizontal();
		{
			for (var i = 0; i < ps.configs.Length; i++)
			{
				SceneConfig c = ps.configs[i];
				if (!GUILayout.Toggle(i == ps.activeIndex, c.id, EditorStyles.toolbarButton)) continue;
				ps.activeIndex = i;
				ps.Apply();
				
				EditorUtility.SetDirty(target);
			}
		}
		GUILayout.EndHorizontal();
			
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu] public class TesujiProjectDefines : ScriptableObject
{
	#if UNITY_EDITOR
	[Serializable] public class DefineInfo
	{
		public string define;
		public bool enable;
		
		[NonSerialized] public bool changed = false;
		
		public DefineInfo(string define)
		{
			enable = true;
			this.define = define;
		}
		
		public bool Draw()
		{
			Color c = GUI.backgroundColor;
			if (changed) GUI.backgroundColor = new Color(0.16f, 0.46f, 0.5f, 0.5f);
			GUILayout.BeginHorizontal();
			{
				var v = GUILayout.Toggle(enable, define);
				changed |= enable != v;
				enable = v;
			}
			GUILayout.EndHorizontal();
			GUI.backgroundColor = c;
			
			return changed;
		}
	}

	public List<DefineInfo> defines = new List<DefineInfo>();

	[ContextMenu("Apply")]
	public void Apply()
	{
		var settings =
			PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(',').ToList();

		for (var i = 0; i < defines.Count; i++)
		{
			DefineInfo key = defines[i];
			if (key.enable)
			{
				if (!settings.Contains(key.define)) settings.Add(key.define);
			}
			else
			{
				settings.Remove(key.define);
			}
		}
		
		PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, settings.ToArray());
	}

	[ContextMenu("Refresh")]
	public void Refresh()
	{
		List<string> settings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(',').ToList();
		for (var i = 0; i < defines.Count; i++)
		{
			var isEnabled = settings.Contains(defines[i].define);
			defines[i].enable = isEnabled;
			defines[i].changed = false;
		}
	}
	
	public void Draw()
	{
		var hasChanged = false;
		for (var i = 0; i < defines.Count; i++)
		{
			var v = defines[i].Draw();
			hasChanged |= v;
		}
		
		GUILayout.BeginHorizontal();
		{
			if (GUILayout.Button("Refresh")) Refresh();
			EditorGUI.BeginDisabledGroup(!hasChanged);
			{
				if (GUILayout.Button("Apply")) Apply();	
			}
			EditorGUI.EndDisabledGroup();
		}
		GUILayout.EndHorizontal();
	}
	
	#endif
}

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

	private static readonly Color GREEN = new Color(.6f, 1f, .6f, 1f);
	private static readonly Color YELLOW = new Color(1f, 1f, .6f, 1f);
	
	[Serializable] public class DefineInfo
	{
		public string id;
		public string defineKey;
		public DefineAction action;
	}
	
	public enum DefineAction { Ignore, Add, Remove }
	public List<string> defineKeys = new List<string>();
	public List<string> configIds = new List<string>();
	public List<DefineInfo> defineDB = new List<DefineInfo>();
	public int activeIndex;
	public bool drawDefault;
	public bool editable;

	[ContextMenu("Draw Default")] void ToggleDrawDefault()
	{
		drawDefault = !drawDefault;
	}
	
	[ContextMenu("Editable")] void Editable()
	{
		editable = !editable;
	}

	public static BuildTargetGroup activeBuildTargetGroup
	{
		get
		{
			var activePlatform = EditorUserBuildSettings.activeBuildTarget;
			switch (activePlatform)
			{
				case BuildTarget.Android : return BuildTargetGroup.Android;
				case BuildTarget.iOS : return BuildTargetGroup.iOS;
				default:
					Debug.LogWarning($"Unsupported platform: {activePlatform}!");
					return activeBuildTargetGroup;
			}
		}
	}
	
	HashSet<string> ReadPlatformSettings()
	{
		var settings = PlayerSettings.GetScriptingDefineSymbolsForGroup(activeBuildTargetGroup)
			.Split(';');

		var hash = new HashSet<string>();
		for (var i = 0; i < settings.Length; i++)
		{
			hash.Add(settings[i]);
		}
		// Debug.LogWarning(string.Join(",", settings));
		return hash;
	}
	Dictionary<string, DefineInfo> GetActiveSettings(string id)
	{
		var result = new Dictionary<string, DefineInfo>();
		for (var i = 0; i < defineDB.Count; i++)
		{
			DefineInfo d = defineDB[i];
			if (d.id != id) continue;
			result.Add(d.defineKey, d);
		}
		
		return result;
	}
	
	public void Apply(string id)
	{
		Dictionary<string, DefineInfo> dict = GetActiveSettings(id);
		HashSet<string> hash = ReadPlatformSettings();
		
		foreach (KeyValuePair<string, DefineInfo> kvp in dict)
		{
			switch (kvp.Value.action)
			{
				case DefineAction.Add:
					hash.Add(kvp.Key);
					continue;
				
				case DefineAction.Remove:
					hash.Remove(kvp.Key);
					continue;
			}
		}
		
		var arr =  hash.ToArray();
		Array.Sort(arr);
		
		Debug.LogWarning($"Recompile: {string.Join(" ; ", arr)} | {EditorUserBuildSettings.activeBuildTarget} | {EditorUserBuildSettings.selectedBuildTargetGroup.ToString()}");
		PlayerSettings.SetScriptingDefineSymbolsForGroup(activeBuildTargetGroup, arr);
		
	}
	
	[NonSerialized] private static bool hasRefresh;
	[NonSerialized] private static HashSet<string> platformSetting = new HashSet<string>();
	[NonSerialized] private static Dictionary<string, DefineInfo> current = new Dictionary<string, DefineInfo>();
	void Refresh()
	{
		if (hasRefresh) return;
		hasRefresh = true;
		platformSetting = ReadPlatformSettings();
		if (activeIndex < 0) activeIndex = 0;
		if (activeIndex >= configIds.Count) activeIndex = configIds.Count - 1;
		current = GetActiveSettings(configIds[activeIndex]);
	}
	
	internal bool Draw()
	{
		if (configIds.Count == 0)
		{
			EditorGUILayout.HelpBox("No config ID found!", MessageType.Info);
			return false;
		}

		if (defineKeys.Count == 0)
		{
			EditorGUILayout.HelpBox("No Define KEY found!", MessageType.Info);
			return false;
		}
		
		if (!hasRefresh) Refresh();
		
		// Draw tab
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		{
			for (var i = 0; i < configIds.Count; i++)
			{
				var t = GUILayout.Toggle(i == activeIndex, configIds[i], EditorStyles.toolbarButton);
				if (!t || (i == activeIndex)) continue;
				activeIndex = i;
				hasRefresh = false;
			}
		}
		EditorGUILayout.EndHorizontal();
		
		if (!hasRefresh) Refresh();
		
		var id = configIds[activeIndex];
		var changed = false;
		
		// EditorGUILayout.HelpBox($"{id}", MessageType.Info);
		
		for (var i = 0; i < defineKeys.Count; i++)
		{
			var key = defineKeys[i];
			GUILayout.BeginHorizontal();
			{
				var hasKey = platformSetting.Contains(key);
				DefineAction a = current.TryGetValue(key, out DefineInfo d) ? d.action : DefineAction.Ignore;
				var isApplied = (hasKey && (a == DefineAction.Add)) || (!hasKey && (a == DefineAction.Remove));
				var isNotApplied = (hasKey && (a == DefineAction.Remove)) || (!hasKey && (a == DefineAction.Add));
				var isItemEditable = editable || (id.ToUpper() == "CUSTOM");
				
				Color oColor = GUI.backgroundColor;
				Color color = isApplied ? GREEN : isNotApplied ? YELLOW : oColor;
				GUI.backgroundColor = color;
				EditorGUI.BeginDisabledGroup(!isItemEditable);
				{
					var newA = (DefineAction)EditorGUILayout.EnumPopup(key, a);
					if (newA != a)
					{
						if (d == null) // will add this key
						{
							var s = new DefineInfo()
							{
								action = newA,
								defineKey = key,
								id = id
							};
							
							defineDB.Add(s);
							current.Add(key, s);
							// Debug.Log($"ADD: {newA} : {key} : {id}");
						}
						else
						{
							// Debug.Log($"CHANGED: {newA}");
							d.action = newA;
						}
						EditorUtility.SetDirty(this);
						changed = true;
					}
				}
				GUI.backgroundColor = oColor;
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
		}

		if (GUILayout.Button("Apply"))
		{
			Apply(id);
			AssetDatabase.SaveAssets();
			return true;
		}
		
		return changed;
	}
	#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TesujiProjectDefines))]
public class TesujiProjectDefineEditor : Editor
{
	public override void OnInspectorGUI()
	{
		if (target == null) return;
		if (EditorApplication.isCompiling)
		{
			EditorGUILayout.HelpBox("Please wait for the Editor to finish compiling!", MessageType.Warning);
			return;
		}
		
		var tpd = (TesujiProjectDefines) target;
		if (tpd.drawDefault)
		{
			DrawDefaultInspector();
		}
		
		if (tpd.Draw())
		{
			Repaint();
		}
	}
}
#endif
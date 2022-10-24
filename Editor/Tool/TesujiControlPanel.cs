using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu] public class TesujiControlPanel : ScriptableObject
{
	[NonSerialized] private static TesujiControlPanel _api;
	[NonSerialized] private static bool _searched = false;

	public static TesujiControlPanel Api
	{
		get
		{
			if (_api != null) return _api;
			return _searched ? null : FindInstance();
		}
	}
	
	public int settingIndex;
	public List<ScriptableObject> settings = new List<ScriptableObject>();
	
	public static TesujiControlPanel FindInstance()
	{
		_searched = true;
		
		var guids = AssetDatabase.FindAssets("t:TesujiControlPanel");
		if (guids.Length == 0)
		{
			Debug.LogWarning("TesujiControlPanel not found!");
			return null;
		}
		
		var _path = AssetDatabase.GUIDToAssetPath(guids[0]);
		_api = AssetDatabase.LoadAssetAtPath<TesujiControlPanel>(_path);
		return _api;
	}


	private static GUIContent[] settingTitles;
	private static Editor _settingEditor;

	void RefreshSettingTitles()
	{
		var result = new List<GUIContent>();
		for (var i = 0; i < settings.Count; i++)
		{
			result.Add(new GUIContent(settings[i].name));
		}
		settingTitles = result.ToArray();
	}
	
	public void DrawSettings()
	{
		if (settings.Count == 0)
		{
			EditorGUILayout.HelpBox("No settings found!", MessageType.Warning);
			return;
		}
		
		if (settingTitles == null || settingTitles.Length != settings.Count)
		{
			RefreshSettingTitles();
		}
		
		var idx = GUILayout.Toolbar(settingIndex, settingTitles, GUILayout.Height(30f));
		ScriptableObject s = settings[idx];
		if (idx != settingIndex || _settingEditor == null)
		{
			settingIndex = idx;
			_settingEditor = Editor.CreateEditor(s);
		}

		_settingEditor.OnInspectorGUI();
	}
}
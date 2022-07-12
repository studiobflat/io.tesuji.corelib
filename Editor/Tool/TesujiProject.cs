using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;

namespace Testuji
{

    public class TesujiProject : EditorWindow
    {
        private static TesujiProject _window;
        static Texture2D iconScene;
        static Texture2D iconPlay;
        static Texture2D iconFolder;
        static GUIStyle productNameStyle;
        static GUIStyle projectStyle;
        static string projectPath;

        private static int activeSceneIndex = 0;
        

        const int MAX_SCENE = 5;
        

        [MenuItem("Tesuji/Panel/Project")]
        private static void ShowWindow()
        {
            if (_window != null) return;

            _window = CreateInstance<TesujiProject>();
            _window.titleContent = new GUIContent("Project");
            _window.Show();
        }
        
        void Init()
        {
            iconScene = AssetPreview.GetMiniTypeThumbnail(typeof(SceneAsset));
            iconPlay = EditorGUIUtility.FindTexture("PlayButton");
            iconFolder = EditorGUIUtility.FindTexture("Folder Icon");
            

			productNameStyle = new GUIStyle(EditorStyles.largeLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 32
			};

			projectStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleCenter
			};

			projectPath = Application.dataPath;
            var arr = projectPath.Split('/').ToList();
            arr.RemoveAt(arr.Count - 1);

            if (arr.Count > 3)
            {
                arr.RemoveRange(0, arr.Count - 3);
            }

            projectPath = string.Join("/", arr);
        }

        void DrawBigPlayButton()
        {
            var scenes = EditorBuildSettings.scenes;
            var scene = scenes[activeSceneIndex];
            
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.Space();
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(16);
                    GUILayout.Label(scene.path);
                    activeSceneIndex = EditorGUILayout.IntSlider(activeSceneIndex, 0, scenes.Length-1);
                }
                GUILayout.EndVertical();
                if (GUILayout.Button(iconPlay, GUILayout.Width(64f), GUILayout.Height(64f)))
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                    EditorApplication.isPlaying = true;
                }
            }
            GUILayout.EndHorizontal();
        }
        
		static void PlayScene(int n)
		{
			var scenes = EditorBuildSettings.scenes;
			var counter = 0;

			for (int i = 0;i < scenes.Length; i++)
			{
				var scene = scenes[i];
				if (!scene.enabled) continue;

				if (counter == n)
				{
					EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
					EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
					EditorApplication.isPlaying = true;
					return;
				}

				counter++;
			}
		}



        void DrawProjectInfo()
        {
            GUILayout.Label(PlayerSettings.productName, productNameStyle);

            var projectPathContent = new GUIContent(projectPath, iconFolder, "click to open folder");

            if (GUILayout.Button(projectPathContent, EditorStyles.toolbarButton))
            {
                EditorUtility.RevealInFinder(Application.dataPath);
            }
        }

        void DrawListScenes()
        {
            var listScenes = EditorBuildSettings.scenes;
            var n = Mathf.Min(listScenes.Length, listScenes.Length);

            GUILayout.BeginVertical();
            {
                for (int i = 0; i < n; i++)
                {
                    var scene = listScenes[i];
                    if (!scene.enabled) continue;

                    GUILayout.BeginHorizontal();
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);

                        if (GUILayout.Button(iconScene, EditorStyles.toolbarButton, GUILayout.Width(25f)))
                        {
                            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                        }

                        EditorGUILayout.ObjectField(asset, typeof(SceneAsset), false);
                        
                        if (GUILayout.Button(iconPlay, EditorStyles.toolbarButton, GUILayout.Width(25f)))
                        {
                            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                            EditorApplication.isPlaying = true;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }
        


        public void OnGUI()
        {
            if (productNameStyle == null) Init();

            DrawProjectInfo();
            EditorGUILayout.Space();
            DrawBigPlayButton();
            EditorGUILayout.Space();
            DrawListScenes();
        }
    }
}
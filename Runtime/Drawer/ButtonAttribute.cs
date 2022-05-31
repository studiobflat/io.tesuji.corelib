
using System;
#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#endif

public enum ButtonMode
    {
        AlwaysEnabled,
        EnabledInPlayMode,
        DisabledInPlayMode
    }

    [Flags]
    public enum ButtonSpacing
    {
        None = 0,
        Before = 1,
        After = 2
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        private string _name = null;
        private ButtonMode _mode = ButtonMode.AlwaysEnabled;
        private ButtonSpacing _spacing = ButtonSpacing.None;

        public string name
        {
            get { return _name; }
        }

        public ButtonMode mode
        {
            get { return _mode; }
        }

        public ButtonSpacing spacing
        {
            get { return _spacing; }
        }

        public ButtonAttribute()
        {
        }

        public ButtonAttribute(string name)
        {
            this._name = name;
        }

        public ButtonAttribute(ButtonMode mode)
        {
            this._mode = mode;
        }

        public ButtonAttribute(ButtonSpacing spacing)
        {
            this._spacing = spacing;
        }

        public ButtonAttribute(string name, ButtonMode mode)
        {
            this._name = name;
            this._mode = mode;
        }

        public ButtonAttribute(string name, ButtonSpacing spacing)
        {
            this._name = name;
            this._spacing = spacing;
        }

        public ButtonAttribute(string name, ButtonMode mode, ButtonSpacing spacing)
        {
            this._name = name;
            this._mode = mode;
            this._spacing = spacing;
        }
    }


#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Object), true)]
    public class ObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawEasyButtons(this);

            DrawDefaultInspector();
        }

        public static void DrawEasyButtons(Editor editor)
        {
            var methods = editor.target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetParameters().Length == 0);
            foreach (var method in methods)
            {
                var ba = (ButtonAttribute) Attribute.GetCustomAttribute(method, typeof(ButtonAttribute));

                if (ba == null) continue;

                var wasEnabled = GUI.enabled;
                GUI.enabled = ba.mode == ButtonMode.AlwaysEnabled
                              || (EditorApplication.isPlaying
                                  ? ba.mode == ButtonMode.EnabledInPlayMode
                                  : ba.mode == ButtonMode.DisabledInPlayMode);


                if (((int) ba.spacing & (int) ButtonSpacing.Before) != 0) GUILayout.Space(10);

                var buttonName = string.IsNullOrEmpty(ba.name) ? ObjectNames.NicifyVariableName(method.Name) : ba.name;
                if (GUILayout.Button(buttonName))
                {
                    foreach (var t in editor.targets)
                    {
                        method.Invoke(t, null);
                    }
                }

                if (((int) ba.spacing & (int) ButtonSpacing.After) != 0) GUILayout.Space(10);

                GUI.enabled = wasEnabled;
            }
        }
    }
#endif
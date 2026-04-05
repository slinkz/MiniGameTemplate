#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Events;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Runtime SO debugger — shows live values of all active Variables, Events, and RuntimeSets.
    /// Only works in Play mode. Auto-refreshes every frame.
    ///
    /// Access via: Tools → MiniGame Template → Debug → SO Runtime Viewer
    /// </summary>
    public class SORuntimeViewer : EditorWindow
    {
        [MenuItem("Tools/MiniGame Template/Debug/SO Runtime Viewer", false, 500)]
        public static void ShowWindow() => GetWindow<SORuntimeViewer>("SO Runtime Viewer");

        private Vector2 _scrollPos;
        private string _filter = "";
        private int _tab; // 0=Variables, 1=Events, 2=RuntimeSets
        private readonly string[] _tabNames = { "Variables", "Events", "RuntimeSets" };

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Label("ScriptableObject Runtime Viewer", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to see live SO values.", MessageType.Info);
                return;
            }

            _tab = GUILayout.Toolbar(_tab, _tabNames);
            _filter = EditorGUILayout.TextField("Filter", _filter);
            GUILayout.Space(5);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_tab)
            {
                case 0: DrawVariables(); break;
                case 1: DrawEvents(); break;
                case 2: DrawRuntimeSets(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawVariables()
        {
            DrawSOList<IntVariable>(v => $"{v.name} = {v.Value}");
            DrawSOList<FloatVariable>(v => $"{v.name} = {v.Value:F2}");
            DrawSOList<BoolVariable>(v => $"{v.name} = {v.Value}");
            DrawSOList<StringVariable>(v => $"{v.name} = \"{v.Value}\"");
        }

        private void DrawEvents()
        {
            // Find all loaded GameEvent SOs
            var events = Resources.FindObjectsOfTypeAll<GameEvent>();
            foreach (var evt in events)
            {
                if (!MatchesFilter(evt.name)) continue;

                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(evt.name, EditorStyles.boldLabel);

                // Use reflection to get listener count
                var listenersField = typeof(GameEvent).GetField("_listeners", BindingFlags.NonPublic | BindingFlags.Instance);
                if (listenersField != null)
                {
                    var listeners = listenersField.GetValue(evt) as System.Collections.IList;
                    int count = listeners?.Count ?? 0;
                    EditorGUILayout.LabelField($"Listeners: {count}", GUILayout.Width(100));
                }

                if (GUILayout.Button("Raise", GUILayout.Width(50)))
                    evt.Raise();
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                    Selection.activeObject = evt;

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRuntimeSets()
        {
            // Find all RuntimeSet<T> subclasses loaded in memory
            var runtimeSets = Resources.FindObjectsOfTypeAll<ScriptableObject>()
                .Where(so => so.GetType().BaseType != null &&
                             so.GetType().BaseType.IsGenericType &&
                             so.GetType().BaseType.GetGenericTypeDefinition().Name.StartsWith("RuntimeSet"));

            foreach (var set in runtimeSets)
            {
                if (!MatchesFilter(set.name)) continue;

                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(set.name, EditorStyles.boldLabel);

                // Get Items count via reflection
                var itemsProp = set.GetType().GetProperty("Items");
                if (itemsProp != null)
                {
                    var items = itemsProp.GetValue(set) as System.Collections.IList;
                    int count = items?.Count ?? 0;
                    EditorGUILayout.LabelField($"Count: {count}", GUILayout.Width(80));
                }

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                    Selection.activeObject = set;

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSOList<T>(Func<T, string> formatter) where T : ScriptableObject
        {
            var assets = Resources.FindObjectsOfTypeAll<T>();
            foreach (var asset in assets)
            {
                if (!MatchesFilter(asset.name)) continue;

                EditorGUILayout.BeginHorizontal("box");

                string display = formatter(asset);
                EditorGUILayout.LabelField(display);

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                    Selection.activeObject = asset;

                EditorGUILayout.EndHorizontal();
            }
        }

        private bool MatchesFilter(string name)
        {
            if (string.IsNullOrEmpty(_filter)) return true;
            return name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
#endif

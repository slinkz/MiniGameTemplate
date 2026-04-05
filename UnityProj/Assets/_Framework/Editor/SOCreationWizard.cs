#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Wizard window for quickly creating common ScriptableObject assets.
    /// Access via: Tools → MiniGame Template → Create → ...
    /// </summary>
    public class SOCreationWizard : EditorWindow
    {
        private enum SOType
        {
            IntVariable,
            FloatVariable,
            StringVariable,
            BoolVariable,
            GameEvent,
            IntGameEvent,
            FloatGameEvent,
            StringGameEvent,
            TransformRuntimeSet,
            PoolDefinition,
            State,
            SceneDefinition,
            AudioClip,
            AudioLibrary,
        }

        private SOType _selectedType = SOType.IntVariable;
        private string _assetName = "NewAsset";
        private string _savePath = "Assets/_Game/ScriptableObjects";

        [MenuItem("Tools/MiniGame Template/SO Creation Wizard", false, 150)]
        private static void ShowWindow()
        {
            var window = GetWindow<SOCreationWizard>("SO Wizard");
            window.minSize = new Vector2(350, 200);
        }

        private void OnGUI()
        {
            GUILayout.Label("ScriptableObject Creation Wizard", EditorStyles.boldLabel);
            GUILayout.Space(10);

            _selectedType = (SOType)EditorGUILayout.EnumPopup("Type", _selectedType);
            _assetName = EditorGUILayout.TextField("Asset Name", _assetName);
            _savePath = EditorGUILayout.TextField("Save Path", _savePath);

            GUILayout.Space(10);

            if (GUILayout.Button("Create", GUILayout.Height(30)))
            {
                CreateAsset();
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Tip: You can also right-click in the Project panel → Create → MiniGameTemplate → ... to create SOs.",
                MessageType.Info);
        }

        private void CreateAsset()
        {
            if (!System.IO.Directory.Exists(_savePath))
                System.IO.Directory.CreateDirectory(_savePath);

            ScriptableObject asset = _selectedType switch
            {
                SOType.IntVariable => ScriptableObject.CreateInstance<Data.IntVariable>(),
                SOType.FloatVariable => ScriptableObject.CreateInstance<Data.FloatVariable>(),
                SOType.StringVariable => ScriptableObject.CreateInstance<Data.StringVariable>(),
                SOType.BoolVariable => ScriptableObject.CreateInstance<Data.BoolVariable>(),
                SOType.GameEvent => ScriptableObject.CreateInstance<Events.GameEvent>(),
                SOType.IntGameEvent => ScriptableObject.CreateInstance<Events.IntGameEvent>(),
                SOType.FloatGameEvent => ScriptableObject.CreateInstance<Events.FloatGameEvent>(),
                SOType.StringGameEvent => ScriptableObject.CreateInstance<Events.StringGameEvent>(),
                SOType.TransformRuntimeSet => ScriptableObject.CreateInstance<Data.TransformRuntimeSet>(),
                SOType.PoolDefinition => ScriptableObject.CreateInstance<Pool.PoolDefinition>(),
                SOType.State => ScriptableObject.CreateInstance<FSM.State>(),
                SOType.SceneDefinition => ScriptableObject.CreateInstance<Core.SceneDefinition>(),
                SOType.AudioClip => ScriptableObject.CreateInstance<Audio.AudioClipSO>(),
                SOType.AudioLibrary => ScriptableObject.CreateInstance<Audio.AudioLibrary>(),
                _ => null
            };

            if (asset == null)
            {
                Debug.LogError("[SOWizard] Failed to create asset of type: " + _selectedType);
                return;
            }

            var fullPath = $"{_savePath}/{_assetName}.asset";
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[SOWizard] Created {_selectedType}: {fullPath}");
        }
    }
}
#endif

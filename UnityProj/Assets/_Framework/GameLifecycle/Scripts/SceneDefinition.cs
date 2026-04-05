using UnityEngine;

namespace MiniGameTemplate.Core
{
    /// <summary>
    /// Scene definition as a ScriptableObject.
    /// Eliminates hardcoded scene name strings throughout the project.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Core/Scene Definition", order = 0)]
    public class SceneDefinition : ScriptableObject
    {
        [Tooltip("Exact scene name as it appears in Build Settings.")]
        [SerializeField] private string _sceneName;

        [Tooltip("Full asset path for YooAsset scene loading (e.g. 'Assets/Scenes/GameScene.unity'). " +
                 "Leave empty to use SceneManager fallback with scene name only.")]
        [SerializeField] private string _scenePath;

        [Tooltip("If true, this scene is loaded additively (on top of current scene).")]
        [SerializeField] private bool _isAdditive;

#if UNITY_EDITOR
        [TextArea(1, 3)]
        [SerializeField] private string _description;
#endif

        public string SceneName => _sceneName;

        /// <summary>
        /// Full asset path for YooAsset. Empty means SceneManager fallback.
        /// </summary>
        public string ScenePath => _scenePath;

        public bool IsAdditive => _isAdditive;
    }
}

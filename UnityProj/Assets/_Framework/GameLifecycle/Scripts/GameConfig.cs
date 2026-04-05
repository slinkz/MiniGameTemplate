using UnityEngine;

namespace MiniGameTemplate.Core
{
    /// <summary>
    /// Global game configuration. One instance per project.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Core/Game Config", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Header("Game Info")]
        [SerializeField] private string _gameName = "My Mini Game";
        [SerializeField] private string _version = "0.1.0";

        [Header("Scenes")]
        [Tooltip("The first scene to load after boot initialization.")]
        [SerializeField] private SceneDefinition _initialScene;

        [Header("Gameplay Defaults")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _runInBackground = true;

        public string GameName => _gameName;
        public string Version => _version;
        public SceneDefinition InitialScene => _initialScene;
        public int TargetFrameRate => _targetFrameRate;
        public bool RunInBackground => _runInBackground;
    }
}

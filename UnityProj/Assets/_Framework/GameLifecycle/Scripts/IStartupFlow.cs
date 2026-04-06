using System.Threading.Tasks;

namespace MiniGameTemplate.Core
{
    /// <summary>
    /// Interface for game-specific startup orchestration (UI flow, privacy checks, etc.).
    /// Implement in the Game layer (_Game/) and assign on the Bootstrapper GameObject.
    ///
    /// Called by GameBootstrapper after all systems are initialized,
    /// BEFORE LoadInitialScene().
    /// </summary>
    public interface IStartupFlow
    {
        /// <summary>
        /// Run the startup flow asynchronously.
        /// Typical implementation: show loading UI → drive progress → privacy check → show main menu.
        /// </summary>
        /// <param name="gameConfig">Game configuration (name, version, initial scene, etc.)</param>
        Task RunAsync(GameConfig gameConfig);
    }
}

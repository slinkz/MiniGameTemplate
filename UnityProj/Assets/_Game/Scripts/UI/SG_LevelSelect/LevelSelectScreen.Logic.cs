using UnityEngine.SceneManagement;
using FairyGUI;
using MiniGameTemplate.UI;
using MiniGameTemplate.Utils;
using Game.ShooterGame;

namespace SG_LevelSelect
{
    /// <summary>
    /// 选关界面 — IUIPanel 实现。
    /// 由 MainMenuPanel 点击「弹幕射击」按钮后 OpenPanelAsync 打开。
    /// 显示 5 个关卡节点（三态：已通关/可用/锁定），处理关卡选择后跳转 Battle 场景。
    /// 
    /// 取代原 LevelSelectController（MonoBehaviour），改为纯 FairyGUI Panel 架构，
    /// 通过 UIManager 管理生命周期。
    /// </summary>
    public partial class LevelSelectScreen : IUIPanel
    {
        public int PanelSortOrder => UIConstants.LAYER_NORMAL;
        public bool IsFullScreen => true;
        public string PanelPackageName => "SG_LevelSelect";

        private SG_ProgressManager _progressManager;
        private readonly LevelNode[] _levelNodes = new LevelNode[5];

        private enum LevelNodeState { Cleared, Available, Locked }

        public void OnOpen(object data)
        {
            _progressManager = SG_Boot.Progress;

            // Cache level node references
            _levelNodes[0] = node_1;
            _levelNodes[1] = node_2;
            _levelNodes[2] = node_3;
            _levelNodes[3] = node_4;
            _levelNodes[4] = node_5;

            // Bind click events (only once in OnOpen)
            for (int i = 0; i < 5; i++)
            {
                int capturedIndex = i + 1; // 1-based
                _levelNodes[i].onClick.Add(() => OnLevelClicked(capturedIndex));
            }

            RefreshAllNodes();
        }

        public void OnClose()
        {
            _progressManager = null;
        }

        public void OnRefresh(object data)
        {
            // Re-read progress and refresh node states
            _progressManager = SG_Boot.Progress;
            RefreshAllNodes();
        }

        private void RefreshAllNodes()
        {
            if (_progressManager == null)
            {
                GameLog.LogWarning("[LevelSelectScreen] ProgressManager is null, showing all nodes as locked except level 1.");
                // Fallback: level 1 available, rest locked
                for (int i = 0; i < 5; i++)
                {
                    SetNodeState(_levelNodes[i], i == 0 ? LevelNodeState.Available : LevelNodeState.Locked, i + 1);
                }
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                int levelIndex = i + 1; // 1-based
                var node = _levelNodes[i];

                if (_progressManager.IsLevelCleared(levelIndex))
                    SetNodeState(node, LevelNodeState.Cleared, levelIndex);
                else if (_progressManager.IsLevelUnlocked(levelIndex))
                    SetNodeState(node, LevelNodeState.Available, levelIndex);
                else
                    SetNodeState(node, LevelNodeState.Locked, levelIndex);
            }
        }

        private void SetNodeState(LevelNode node, LevelNodeState state, int levelIndex)
        {
            // Switch FairyGUI controller for three-state display
            if (node.state != null)
            {
                switch (state)
                {
                    case LevelNodeState.Cleared:
                        node.state.selectedIndex = 0;
                        break;
                    case LevelNodeState.Available:
                        node.state.selectedIndex = 1;
                        break;
                    case LevelNodeState.Locked:
                        node.state.selectedIndex = 2;
                        break;
                }
            }
        }

        private void OnLevelClicked(int levelIndex)
        {
            // Locked level: shake feedback
            if (_progressManager != null && !_progressManager.IsLevelUnlocked(levelIndex))
            {
                ShakeLevelNode(levelIndex);
                return;
            }

            // If no progress manager, only level 1 is available
            if (_progressManager == null && levelIndex > 1)
            {
                ShakeLevelNode(levelIndex);
                return;
            }

            GameLog.Log($"[LevelSelectScreen] Level {levelIndex} selected, transitioning to Battle.");

            // Set current level index via SG_Boot or direct scene load
            // Close this panel and load Battle scene
            UIManager.Instance.ClosePanel<LevelSelectScreen>();
            SceneManager.LoadScene("Battle");
        }

        private void ShakeLevelNode(int levelIndex)
        {
            var node = _levelNodes[levelIndex - 1];
            node.TweenMoveX(node.x + 5f, 0.05f).SetRepeat(3, true);
        }
    }
}

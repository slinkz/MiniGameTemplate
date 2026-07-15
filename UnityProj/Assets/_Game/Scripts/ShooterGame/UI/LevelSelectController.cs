using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FairyGUI;
using MiniGameTemplate.Data;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 选关界面 Controller——显示 5 个关卡节点（三态）+ 处理选择。
    /// TDD_04 §3
    /// </summary>
    public class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private IntVariable _currentLevelIndex;
        [SerializeField] private string _battleSceneName = "Battle";

        private SG_ProgressManager _progressManager;
        private SG_LevelSelect.LevelSelectScreen _view;
        private readonly SG_LevelSelect.LevelNode[] _levelNodes = new SG_LevelSelect.LevelNode[5];

        private enum LevelNodeState { Cleared, Available, Locked }

        public void Init(SG_ProgressManager progressManager)
        {
            _progressManager = progressManager;
        }

        public void Show(int newlyUnlockedLevel = -1)
        {
            if (_view == null)
            {
                _view = SG_LevelSelect.LevelSelectScreen.CreateInstance();
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                SetupNodes();
            }

            _view.visible = true;
            RefreshAllNodes();

            if (newlyUnlockedLevel > 0 && newlyUnlockedLevel <= 5)
                PlayUnlockAnimation(newlyUnlockedLevel);
        }

        public void Hide()
        {
            if (_view != null)
                _view.visible = false;
        }

        private void SetupNodes()
        {
            _levelNodes[0] = _view.node_1;
            _levelNodes[1] = _view.node_2;
            _levelNodes[2] = _view.node_3;
            _levelNodes[3] = _view.node_4;
            _levelNodes[4] = _view.node_5;

            for (int i = 0; i < _levelNodes.Length; i++)
            {
                int capturedIndex = i + 1; // 1-based capture for closure
                if (_levelNodes[i] != null)
                    _levelNodes[i].onClick.Add(() => OnLevelClicked(capturedIndex));
                else
                    Debug.LogError($"[LevelSelectController] Required level node missing: node_{capturedIndex}");
            }
        }

        private void RefreshAllNodes()
        {
            for (int i = 0; i < 5; i++)
            {
                int levelIndex = i + 1; // 1-based
                var node = _levelNodes[i];

                if (_progressManager.IsLevelCleared(levelIndex))
                {
                    SetNodeState(node, LevelNodeState.Cleared);
                    int stars = _progressManager.GetLevelStars(levelIndex);
                    SetNodeStars(node, stars);
                }
                else if (_progressManager.IsLevelUnlocked(levelIndex))
                {
                    SetNodeState(node, LevelNodeState.Available);
                }
                else
                {
                    SetNodeState(node, LevelNodeState.Locked);
                }
            }
        }

        private void SetNodeState(SG_LevelSelect.LevelNode node, LevelNodeState state)
        {
            // 通过 FairyGUI Controller 切换节点三态
            var ctrl = node?.state;
            if (ctrl != null)
            {
                switch (state)
                {
                    case LevelNodeState.Cleared:
                        ctrl.selectedIndex = 0;
                        break;
                    case LevelNodeState.Available:
                        ctrl.selectedIndex = 1;
                        break;
                    case LevelNodeState.Locked:
                        ctrl.selectedIndex = 2;
                        break;
                }
            }
        }

        private void SetNodeStars(SG_LevelSelect.LevelNode node, int stars)
        {
            var starGroup = node?.star_group;
            if (starGroup == null) return;

            var ctrl = starGroup.stars;
            if (ctrl != null)
                ctrl.selectedIndex = Mathf.Clamp(stars, 0, 3);
        }

        private void OnLevelClicked(int levelIndex)
        {
            if (!_progressManager.IsLevelUnlocked(levelIndex))
            {
                // 锁定关卡：摇晃提示
                ShakeLevelNode(levelIndex);
                return;
            }

            _currentLevelIndex.SetValue(levelIndex - 1); // 1-based → 0-based
            StartCoroutine(TransitionToBattle());
        }

        private IEnumerator TransitionToBattle()
        {
            // §8.2: LevelNode 缩放 0.2s → 白闪 0.1s → LoadScene
            yield return new WaitForSeconds(0.3f);
            SceneManager.LoadScene(_battleSceneName);
        }

        private void ShakeLevelNode(int levelIndex)
        {
            var node = _levelNodes[levelIndex - 1];
            node.TweenMoveX(node.x + 5f, 0.05f).SetRepeat(3, true);
        }

        private void PlayUnlockAnimation(int levelIndex)
        {
            var node = _levelNodes[levelIndex - 1];
            node.TweenScale(new Vector2(1.2f, 1.2f), 0.2f)
                .OnComplete(() => node.TweenScale(new Vector2(1f, 1f), 0.1f));
        }
    }
}

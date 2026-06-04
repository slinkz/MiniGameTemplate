using System.Threading.Tasks;
using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Data;
using MiniGameTemplate.Entity;
using MiniGameTemplate.UI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// BattleHUD Controller V2——统一管理战斗 HUD 所有子组件。
    /// TDD_05 §S5.4
    /// 
    /// 子组件：
    /// - 血条 + 百分比文字
    /// - SkillCDPanel（6 个技能 CD 指示器）
    /// - PassiveIndicatorPanel（3 个被动栏）
    /// - WaveIndicatorAnimator（波次弹跳动效）
    /// - PickupNotificationQueue（拾取通知条）
    /// - BattleVisualFeedbackSystem（红闪/反击/道具吸入）
    /// - BattleStartSequence（战斗开始过渡动效）
    /// </summary>
    public class BattleHUDController : MonoBehaviour, IBattleHUDController
    {
        private const string FGUI_PKG = "SG_Battle";
        private const string FGUI_BATTLE_HUD = "BattleHUD";

        [Header("SO 数据源")]
        [SerializeField] private FloatVariable _baseHP;
        [SerializeField] private IntVariable _currentWaveIndex;
        [SerializeField] private IntVariable _totalWaveCount;

        [Header("血条颜色")]
        [SerializeField] private Color _hpGreen = new Color(0.3f, 0.85f, 0.3f);
        [SerializeField] private Color _hpYellow = new Color(0.9f, 0.85f, 0.2f);
        [SerializeField] private Color _hpRed = new Color(0.9f, 0.2f, 0.2f);

        // FairyGUI 引用
        private GComponent _view;
        public GComponent View => _view;
        private GProgressBar _hpBar;
        private GTextField _waveText;
        private GTextField _hpPctText;

        // 血条预损动画
        private float _displayHP = 1f;
        private float _targetHP = 1f;
        private const float PREDAMAGE_LERP_SPEED = 2f;
        private const float PREDAMAGE_DELAY = 0.3f;
        private float _predamageTimer;

        // ── V2 子组件 ──
        private SkillCDPanel _skillCDPanel;
        private PassiveIndicatorPanel _passivePanel;
        private WaveIndicatorAnimator _waveAnimator;
        private PickupNotificationQueue _notificationQueue;
        private BattleVisualFeedbackSystem _visualFeedback;
        private BattleStartSequence _startSequence;

        // 外部注入的 Entity 引用（由 BattleController 设置）
        private Entity _playerEntity;

#if UNITY_EDITOR
        [Header("Debug (Editor Only)")]
        public float Debug_DisplayHP;
        public float Debug_TargetHP;
#endif

        // ── IBattleHUDController 实现 ──

        public void Show()
        {
            // 同步版本——仅在包已预加载时使用
            _view = UIPackage.CreateObject(FGUI_PKG, FGUI_BATTLE_HUD).asCom;
            GRoot.inst.AddChild(_view);
            _view.MakeFullScreen();
            _hpBar = _view.GetChild("hp_bar").asProgress;
            _waveText = _view.GetChild("text_wave").asTextField;
            _hpPctText = _view.GetChild("text_hp_pct")?.asTextField;

            InitSubComponents();
        }

        /// <summary>
        /// 异步版本——自动加载 FairyGUI 包后再创建 HUD。
        /// Battle 场景应优先使用此方法。
        /// </summary>
        public async Task ShowAsync()
        {
            await UIPackageLoader.AddPackageAsync(FGUI_PKG, SG_Battle.SG_BattleBinder.BindAll);
            Show();
        }

        public GComponent GetView() => _view;

        public void ForceRefresh()
        {
            _displayHP = _targetHP = _baseHP.Value;
            UpdateHPBarFill(_targetHP);
            UpdateHPBarColor(_targetHP);
            UpdateHPText(_targetHP);
            UpdateWaveText();
        }

        // ── V2: 子组件管理 ──

        /// <summary>
        /// 注入玩家 Entity 引用（由 BattleController 在 InitBattle 后调用）。
        /// </summary>
        public void SetPlayerEntity(Entity player)
        {
            _playerEntity = player;
        }

        /// <summary>获取视觉反馈系统（BattleController 用于触发红闪等）</summary>
        public BattleVisualFeedbackSystem VisualFeedback => _visualFeedback;

        /// <summary>获取通知队列（PickupSystem 拾取时调用）</summary>
        public PickupNotificationQueue NotificationQueue => _notificationQueue;

        /// <summary>获取波次动效器（BattleController 波次切换时调用）</summary>
        public WaveIndicatorAnimator WaveAnimator => _waveAnimator;

        /// <summary>获取战斗开始序列</summary>
        public BattleStartSequence StartSequence => _startSequence;

        private void InitSubComponents()
        {
            if (_view == null) return;

            // 血条位于屏幕最下方——获取 Y 坐标用于定位技能面板
            float hpBarY = _view.height - 40f; // 粗略估算

            // 1. 技能 CD 面板
            _skillCDPanel = new SkillCDPanel(_view, hpBarY);

            // 2. 被动栏
            _passivePanel = new PassiveIndicatorPanel(_view);

            // 3. 波次动效
            _waveAnimator = new WaveIndicatorAnimator(_waveText);

            // 4. 拾取通知队列
            float centerX = _view.width * 0.5f;
            float notifY = hpBarY - 60f; // 基地血条上偏高
            _notificationQueue = new PickupNotificationQueue(_view, centerX, notifY);

            // 5. 视觉反馈系统
            _visualFeedback = new BattleVisualFeedbackSystem();
            _visualFeedback.Init(_view);

            // 6. 战斗开始序列（延迟到 PlayStartSequence 时初始化）
            _startSequence = new BattleStartSequence(_view);
        }

        /// <summary>
        /// 播放战斗开始过渡动效（TDD_05 S5.4 PK-R3 UID-010）。
        /// </summary>
        public void PlayStartSequence(int waveNumber, System.Action onComplete)
        {
            if (_startSequence != null)
                _startSequence.Play(waveNumber, onComplete);
            else
                onComplete?.Invoke();
        }

        // ── 生命周期 ──

        private void OnEnable()
        {
            _baseHP.OnValueChanged += OnBaseHPChanged;
            _currentWaveIndex.OnValueChanged += OnWaveChanged;
            _totalWaveCount.OnValueChanged += OnTotalWaveCountChanged;
        }

        private void OnDisable()
        {
            _baseHP.OnValueChanged -= OnBaseHPChanged;
            _currentWaveIndex.OnValueChanged -= OnWaveChanged;
            _totalWaveCount.OnValueChanged -= OnTotalWaveCountChanged;
        }

        private void OnDestroy()
        {
            _skillCDPanel?.Dispose();
            _passivePanel?.Dispose();
            _notificationQueue?.Clear();
            _visualFeedback?.Dispose();

            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            UpdatePreDamage(dt);
            TickSubComponents(dt);

#if UNITY_EDITOR
            Debug_DisplayHP = _displayHP;
            Debug_TargetHP = _targetHP;
#endif
        }

        /// <summary>
        /// 每帧驱动所有子组件。
        /// </summary>
        private void TickSubComponents(float dt)
        {
            // 通知条超时淡出
            _notificationQueue?.Tick(dt);

            // 视觉反馈淡出
            _visualFeedback?.Tick(dt);

            // 被动栏呼吸动画
            _passivePanel?.Tick(dt);

            // 技能 CD + 被动状态实时同步
            if (_playerEntity != null)
            {
                TickSkillCDPanel();
                TickPassivePanel();
            }
        }

        private void TickSkillCDPanel()
        {
            if (_skillCDPanel == null) return;

            var skillComp = _playerEntity.GetComponent(ComponentType.Skill) as SkillComponent;
            if (skillComp == null) return;

            // UI 可显示的技能数受 SkillComponent 实际数组长度限制（避免越界）
            int uiCount = Mathf.Min(SkillCDPanel.MAX_UI_SLOTS,
                                    SkillComponent.MAX_SLOTS - SkillCDPanel.SKILL_SLOT_START_INDEX);

            for (int i = 0; i < uiCount; i++)
            {
                int slotIndex = i + SkillCDPanel.SKILL_SLOT_START_INDEX;
                ref readonly var slot = ref skillComp.GetSlot(slotIndex);

                if (slot.IsEmpty)
                {
                    _skillCDPanel.UpdateSlot(i, SkillCDPanel.SkillSlotState.Empty);
                    continue;
                }

                SkillCDPanel.SkillSlotState uiState;
                float progress = 0f;

                switch (slot.State)
                {
                    case SkillState.Cooldown:
                        uiState = SkillCDPanel.SkillSlotState.Cooldown;
                        progress = 1f - slot.CooldownProgress; // 进度 0→1 代表 CD 恢复
                        break;
                    case SkillState.Casting:
                        uiState = SkillCDPanel.SkillSlotState.Casting;
                        break;
                    case SkillState.Recovery:
                        uiState = SkillCDPanel.SkillSlotState.Release;
                        break;
                    default: // Idle
                        uiState = SkillCDPanel.SkillSlotState.Ready;
                        break;
                }

                _skillCDPanel.UpdateSlot(i, uiState, progress);
            }
        }

        private void TickPassivePanel()
        {
            if (_passivePanel == null) return;

            var passiveComp = _playerEntity.GetComponent(ComponentType.Passive) as PassiveComponent;
            if (passiveComp == null || !passiveComp.IsActive)
            {
                for (int i = 0; i < PassiveComponent.MAX_PASSIVES; i++)
                    _passivePanel.UpdateSlot(i, PassiveIndicatorPanel.PassiveSlotState.Empty);
                return;
            }

            for (int i = 0; i < PassiveComponent.MAX_PASSIVES; i++)
            {
                ref readonly var slot = ref passiveComp.GetSlot(i);

                if (slot.Config == null)
                {
                    _passivePanel.UpdateSlot(i, PassiveIndicatorPanel.PassiveSlotState.Empty);
                    continue;
                }

                if (slot.IsEffectActive)
                {
                    // Active 态：环形进度消耗
                    // 持续时间来自 LinkedBuff.Duration（Buff 型被动），即时型按 ActiveTimer 原始值
                    float duration = slot.Config.LinkedBuff != null
                        ? slot.Config.LinkedBuff.Duration
                        : slot.TotalCooldown; // fallback
                    float progress = duration > 0 ? slot.ActiveTimer / duration : 0f;
                    _passivePanel.UpdateSlot(i, PassiveIndicatorPanel.PassiveSlotState.Active, progress);
                }
                else if (slot.CooldownTimer > 0f)
                {
                    // CD 中
                    float progress = slot.TotalCooldown > 0
                        ? 1f - (slot.CooldownTimer / slot.TotalCooldown)
                        : 0f;
                    _passivePanel.UpdateSlot(i, PassiveIndicatorPanel.PassiveSlotState.Cooldown, progress);
                }
                else
                {
                    // Ready
                    _passivePanel.UpdateSlot(i, PassiveIndicatorPanel.PassiveSlotState.Ready);
                }
            }
        }

        // ── 血条 ──

        private void OnBaseHPChanged(float newRatio)
        {
            float oldTarget = _targetHP;
            _targetHP = newRatio;

            if (newRatio < oldTarget)
            {
                _predamageTimer = PREDAMAGE_DELAY;
                UpdateHPBarFill(_targetHP);
            }
            else
            {
                _displayHP = newRatio;
                UpdateHPBarFill(_targetHP);
            }

            UpdateHPBarColor(_targetHP);
            UpdateHPText(_targetHP);
        }

        private void UpdatePreDamage(float dt)
        {
            if (_displayHP <= _targetHP) return;

            if (_predamageTimer > 0f)
            {
                _predamageTimer -= dt;
                return;
            }

            _displayHP = Mathf.Lerp(_displayHP, _targetHP, PREDAMAGE_LERP_SPEED * dt);
            if (Mathf.Abs(_displayHP - _targetHP) < 0.001f)
                _displayHP = _targetHP;
        }

        private void UpdateHPBarFill(float ratio)
        {
            if (_hpBar != null)
                _hpBar.value = ratio * 100;
        }

        private void UpdateHPBarColor(float ratio)
        {
            // V1 简化：通过进度条填充颜色标识
            // 实际颜色由 FairyGUI 编辑器内组件控制
        }

        private void UpdateHPText(float ratio)
        {
            if (_hpPctText != null)
                _hpPctText.text = $"{Mathf.RoundToInt(ratio * 100)}%";
        }

        // ── 波次 ──

        private void OnWaveChanged(int newWave)
        {
            UpdateWaveText();
            // V2: 触发波次切换动效
            _waveAnimator?.PlayWaveTransition(newWave, _totalWaveCount.Value);
        }

        private void OnTotalWaveCountChanged(int newTotalWaveCount)
        {
            UpdateWaveText();
        }

        private void UpdateWaveText()
        {
            if (_waveText != null)
                _waveText.text = $"Wave {_currentWaveIndex.Value}/{_totalWaveCount.Value}";
        }

    }
}

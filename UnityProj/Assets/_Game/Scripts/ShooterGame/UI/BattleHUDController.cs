using System.Threading.Tasks;
using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Data;
using MiniGameTemplate.UI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// BattleHUD Controller——血条 + 波次指示 + 暂停按钮 + 受伤红闪。
    /// TDD_04 §4
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
        }

        /// <summary>
        /// 异步版本——自动加载 FairyGUI 包后再创建 HUD。
        /// Battle 场景应优先使用此方法。
        /// 直接传入 Binder，确保包加载与扩展绑定配对，支持直接运行 Battle 场景。
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
            if (_view != null)
            {
                _view.Dispose();
                _view = null;
            }
        }

        private void Update()
        {
            UpdatePreDamage(Time.deltaTime);

#if UNITY_EDITOR
            Debug_DisplayHP = _displayHP;
            Debug_TargetHP = _targetHP;
#endif
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

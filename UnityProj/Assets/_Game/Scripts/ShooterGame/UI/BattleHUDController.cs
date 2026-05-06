using System.Threading.Tasks;
using UnityEngine;
using FairyGUI;
using MiniGameTemplate.Data;
using MiniGameTemplate.UI;

namespace Game.ShooterGame.UI
{
    /// <summary>
    /// BattleHUD Controller——血条 + 波次指示 + 暂停按钮 + 飘字 + 受伤红闪。
    /// TDD_04 §4
    /// </summary>
    public class BattleHUDController : MonoBehaviour, IBattleHUDController
    {
        private const string FGUI_PKG = "SG_Battle";
        private const string FGUI_BATTLE_HUD = "BattleHUD";
        private const string FGUI_FLOATING_TEXT = "FloatingText";

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

        // 血条预损动画
        private float _displayHP = 1f;
        private float _targetHP = 1f;
        private const float PREDAMAGE_LERP_SPEED = 2f;
        private const float PREDAMAGE_DELAY = 0.3f;
        private float _predamageTimer;

        // 飘字池
        private const int MAX_FLOATING_TEXTS = 8;
        private readonly GComponent[] _floatingTexts = new GComponent[MAX_FLOATING_TEXTS];
        private int _floatingTextHead;

#if UNITY_EDITOR
        [Header("Debug (Editor Only)")]
        public float Debug_DisplayHP;
        public float Debug_TargetHP;
        public int Debug_ActiveFloatingTextCount;
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
            RecycleAllFloatingTexts();
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
            // 场景卸载时清理 FairyGUI 对象（挂在 DontDestroyOnLoad 的 GRoot 上）
            for (int i = 0; i < MAX_FLOATING_TEXTS; i++)
            {
                if (_floatingTexts[i] != null)
                {
                    _floatingTexts[i].Dispose();
                    _floatingTexts[i] = null;
                }
            }

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
            // V1：不显示百分比文字（由 FairyGUI 包内决定是否有此元素）
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

        // ── 飘字 ──

        /// <summary>
        /// 在世界坐标位置显示飘字。
        /// 环形缓冲 FIFO——超出上限回收最旧的。TDD_04 §4.4
        /// </summary>
        public void ShowFloatingText(Vector3 worldPos, string text, Color color)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            Vector2 uiPos = GRoot.inst.GlobalToLocal(new Vector2(screenPos.x, Screen.height - screenPos.y));

            var ft = _floatingTexts[_floatingTextHead];
            if (ft == null)
            {
                ft = UIPackage.CreateObject(FGUI_PKG, FGUI_FLOATING_TEXT).asCom;
                GRoot.inst.AddChild(ft);
                _floatingTexts[_floatingTextHead] = ft;
            }
            else
            {
                GTween.Kill(ft);
            }

            ft.visible = true;
            ft.SetPosition(uiPos.x, uiPos.y, 0);
            ft.alpha = 1f;
            ft.GetChild("text").asTextField.text = text;
            ft.GetChild("text").asTextField.color = color;

            ft.TweenMoveY(uiPos.y - 60f, 0.8f).SetEase(EaseType.QuadOut);
            ft.TweenFade(0f, 0.8f).OnComplete(() => { ft.visible = false; });

            _floatingTextHead = (_floatingTextHead + 1) % MAX_FLOATING_TEXTS;

#if UNITY_EDITOR
            Debug_ActiveFloatingTextCount = CountActiveFloatingTexts();
#endif
        }

        public void RecycleAllFloatingTexts()
        {
            for (int i = 0; i < MAX_FLOATING_TEXTS; i++)
            {
                if (_floatingTexts[i] != null)
                    _floatingTexts[i].visible = false;
            }
            _floatingTextHead = 0;
        }

#if UNITY_EDITOR
        private int CountActiveFloatingTexts()
        {
            int count = 0;
            for (int i = 0; i < MAX_FLOATING_TEXTS; i++)
                if (_floatingTexts[i] != null && _floatingTexts[i].visible)
                    count++;
            return count;
        }
#endif
    }
}

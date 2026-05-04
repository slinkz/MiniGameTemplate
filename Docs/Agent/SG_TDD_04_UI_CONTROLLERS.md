---
system: shootergame
scope: ui-controllers
last_verified: 2026-05-04
depends_on: [SG_TDD_INDEX, SG_UI_DESIGN]
related_code: Assets/_Game/Scripts/ShooterGame/UI/*.cs, Assets/_Game/Scripts/ShooterGame/Core/BattleController.cs
---

# SG_TDD_04: UI Controllers

> 父文档：[SG_TDD_INDEX.md](SG_TDD_INDEX.md)

---

## 1. 界面类型与生命周期

| 界面 | C# Controller | 基类 | 场景归属 | FairyGUI 包 |
|------|--------------|------|---------|------------|
| LoadingScreen | LoadingScreenController | MonoBehaviour | Boot | Loading |
| LevelSelectScreen | LevelSelectController | MonoBehaviour | Boot | LevelSelect |
| BattleHUD | BattleHUDController | MonoBehaviour | Battle | Battle |
| PausePanel | PausePanelController | MonoBehaviour | Battle | Popup |
| VictoryPanel | VictoryPanelController | MonoBehaviour | Battle | Popup |
| DefeatPanel | DefeatPanelController | MonoBehaviour | Battle | Popup |

---

## 2. LoadingScreenController

### 2.1 职责

加载 FairyGUI 包 + 显示进度条 + 自动跳转选关界面。

### 2.2 关键接口

```csharp
namespace Game.ShooterGame.UI
{
    public class LoadingScreenController : MonoBehaviour
    {
        [SerializeField] private float _minDisplayTime = 1f;
        
        private GComponent _view;
        private GProgressBar _progressBar;
        
        public void Show()
        {
            _view = UIPackage.CreateObject("Loading", "LoadingScreen").asCom;
            GRoot.inst.AddChild(_view);
            _view.MakeFullScreen();
            _progressBar = _view.GetChild("bar").asProgress;
        }
        
        public void SetProgress(float ratio)
        {
            _progressBar.value = ratio * 100;
        }
        
        public async void Hide()
        {
            // 确保最少显示 _minDisplayTime
            _view.TweenFade(0f, 0.3f).OnComplete(() => {
                _view.Dispose();
                _view = null;
            });
        }
    }
}
```

---

## 3. LevelSelectController

### 3.1 职责

显示 5 个关卡节点（三态）+ 处理关卡选择 + 管理解锁动效。

### 3.2 关键接口

```csharp
namespace Game.ShooterGame.UI
{
    public class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private IntVariable _currentLevelIndex;
        [SerializeField] private string _battleSceneName = "Battle";
        
        private SG_ProgressManager _progressManager;
        private GComponent _view;
        private GComponent[] _levelNodes = new GComponent[5];
        
        public void Init(SG_ProgressManager progressManager)
        {
            _progressManager = progressManager;
        }
        
        public void Show(int newlyUnlockedLevel = -1)
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject("LevelSelect", "LevelSelectScreen").asCom;
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                SetupNodes();
            }
            
            _view.visible = true;
            RefreshAllNodes();
            
            // 新解锁关卡动效
            if (newlyUnlockedLevel > 0 && newlyUnlockedLevel <= 5)
                PlayUnlockAnimation(newlyUnlockedLevel);
        }
        
        private void RefreshAllNodes()
        {
            for (int i = 0; i < 5; i++)
            {
                int levelIndex = i + 1;  // 1-based
                var node = _levelNodes[i];
                
                if (_progressManager.IsLevelCleared(levelIndex))
                    SetNodeState(node, LevelNodeState.Cleared);
                else if (_progressManager.IsLevelUnlocked(levelIndex))
                    SetNodeState(node, LevelNodeState.Available);
                else
                    SetNodeState(node, LevelNodeState.Locked);
            }
        }
        
        private void OnLevelClicked(int levelIndex)
        {
            if (!_progressManager.IsLevelUnlocked(levelIndex))
            {
                // 锁定关卡：摇晃 + Toast
                ShakeLevelNode(levelIndex);
                return;
            }
            
            _currentLevelIndex.SetValue(levelIndex - 1);  // 0-based
            // ET-008: 实际通过转场 Coroutine 执行（见 §8.2 转场时序表）
            // 简化伪代码 → 实际实现：StartCoroutine(TransitionToBattle())
            StartCoroutine(TransitionToBattle());
        }
        
        private IEnumerator TransitionToBattle()
        {
            // §8.2: LevelNode 缩放 0.2s → 白闪 0.1s → LoadScene + 淡入 0.2s
            yield return PlayNodeClickAnimation(0.2f);
            yield return PlayWhiteFlash(0.1f);
            SceneManager.LoadScene(_battleSceneName);
        }
        
        private enum LevelNodeState { Cleared, Available, Locked }
    }
}
```

---

## 4. BattleHUDController

### 4.1 职责

血条 + 波次指示 + 暂停按钮 + 飘字 + 受伤红闪。这是最复杂的 UI Controller。

**暂停按钮绑定**（P3 实现）：暂停按钮 `btn_pause_bg` 的 click 事件由 `BattleController.InitBattle()` 直接从 HUD view 获取并绑定到 `OnPauseButtonClicked()`，`OnDestroy()` 中解绑。不通过 BattleHUDController 转发——减少一层间接。

### 4.2 FairyGUI 白模包对照表（P3 新增）

| 包目录 | publish name | 主组件 | 关键子元素 |
|--------|-------------|--------|-----------|
| `SG_Loading/` | Loading | LoadingScreen | bar(ProgressBar) |
| `SG_LevelSelect/` | LevelSelect | LevelSelectScreen | node_1~5(LevelNode) |
| `SG_Battle/` | Battle | BattleHUD | hp_bar, text_wave, btn_pause_bg |
| `SG_Battle/components/` | — | FloatingText | text(TextField) |
| `SG_Battle/components/` | — | Joystick | stick(GGraph) |
| `SG_Popup/` | Popup | PausePanel | btn_resume, btn_quit |
| `SG_Popup/` | Popup | VictoryPanel | btn_confirm, text_kills, text_hp |
| `SG_Popup/` | Popup | DefeatPanel | btn_retry, btn_quit, text_progress, text_encourage |

### 4.2 关键接口

```csharp
namespace Game.ShooterGame.UI
{
    public class BattleHUDController : MonoBehaviour
    {
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
        /// <summary>ET-007: 公共 getter 供 JoystickController.Init 获取 BattleHUD 容器</summary>
        public GComponent View => _view;
        private GProgressBar _hpBar;
        private GTextField _waveText;
        private GGraph _redFlashOverlay;
        
        // 血条预损动画
        private float _displayHP = 1f;   // 白色段（延迟追赶）
        private float _targetHP = 1f;    // 绿色段（立即跟踪 SO）
        private const float PREDAMAGE_LERP_SPEED = 2f;
        private const float PREDAMAGE_DELAY = 0.3f;
        private float _predamageTimer;
        
        // 飘字池
        private const int MAX_FLOATING_TEXTS = 8;
        private GComponent[] _floatingTexts = new GComponent[MAX_FLOATING_TEXTS];
        private int _floatingTextHead;  // FIFO 环形缓冲头指针
        
#if UNITY_EDITOR
        [Header("Debug (Editor Only)")]
        public float Debug_DisplayHP;
        public float Debug_TargetHP;
        public int Debug_ActiveFloatingTextCount;
#endif
        
        // ── 生命周期 ──
        
        private void OnEnable()
        {
            _baseHP.OnValueChanged += OnBaseHPChanged;
            _currentWaveIndex.OnValueChanged += OnWaveChanged;
        }
        
        private void OnDisable()
        {
            _baseHP.OnValueChanged -= OnBaseHPChanged;
            _currentWaveIndex.OnValueChanged -= OnWaveChanged;
        }
        
        private void Update()
        {
            UpdatePreDamage(Time.deltaTime);
            
#if UNITY_EDITOR
            Debug_DisplayHP = _displayHP;
            Debug_TargetHP = _targetHP;
#endif
        }
    }
}
```

### 4.3 血条预损动画实现

```csharp
private void OnBaseHPChanged(float newRatio)
{
    float oldTarget = _targetHP;
    _targetHP = newRatio;
    
    // 如果是扣血，启动预损延迟
    if (newRatio < oldTarget)
    {
        _predamageTimer = PREDAMAGE_DELAY;
        
        // 立即更新绿色段
        UpdateHPBarFill(_targetHP);
        
        // 触发红闪
        PlayRedFlash();
    }
    else
    {
        // 加血（重试时）——跳过预损直接同步
        _displayHP = newRatio;
        UpdateHPBarFill(_targetHP);
        UpdatePreDamageBar(_displayHP);
    }
    
    // 更新血条颜色
    UpdateHPBarColor(_targetHP);
    
    // 更新百分比文字
    UpdateHPText(_targetHP);
}

private void UpdatePreDamage(float dt)
{
    if (_displayHP <= _targetHP) return;  // 无需追赶
    
    if (_predamageTimer > 0f)
    {
        _predamageTimer -= dt;
        return;
    }
    
    // Lerp 追赶
    _displayHP = Mathf.Lerp(_displayHP, _targetHP, PREDAMAGE_LERP_SPEED * dt);
    if (Mathf.Abs(_displayHP - _targetHP) < 0.001f)
        _displayHP = _targetHP;
    
    UpdatePreDamageBar(_displayHP);
}

private void UpdateHPBarColor(float ratio)
{
    Color color;
    if (ratio > 0.5f)
        color = _hpGreen;
    else if (ratio > 0.3f)
        color = _hpYellow;
    else
        color = _hpRed;
    
    // FairyGUI 设置 ProgressBar 填充颜色
    // 具体 API 取决于 FairyGUI 版本
}
```

### 4.4 飘字池化实现

```csharp
/// <summary>
/// 在世界坐标位置显示飘字。
/// 环形缓冲 FIFO——超出上限回收最旧的。
/// </summary>
public void ShowFloatingText(Vector3 worldPos, string text, Color color)
{
    // 坐标转换：World → Screen → FairyGUI UI
    Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
    Vector2 uiPos = GRoot.inst.GlobalToLocal(new Vector2(screenPos.x, Screen.height - screenPos.y));
    
    // 获取池化对象
    var ft = _floatingTexts[_floatingTextHead];
    if (ft == null)
    {
        ft = UIPackage.CreateObject("Battle", "FloatingText").asCom;
        GRoot.inst.AddChild(ft);
        _floatingTexts[_floatingTextHead] = ft;
    }
    else
    {
        // ET-006: Kill 旧 Tween 防止 OnComplete 回调影响新飘字
        ft.TweenKillAll();
    }
    
    // 重置并播放
    ft.visible = true;
    ft.SetPosition(uiPos.x, uiPos.y, 0);
    ft.alpha = 1f;
    ft.GetChild("text").asTextField.text = text;
    ft.GetChild("text").asTextField.color = color;
    
    // 上浮淡出动效
    ft.TweenMoveY(uiPos.y - 60f, 0.8f).SetEase(EaseType.QuadOut);
    ft.TweenFade(0f, 0.8f).OnComplete(() => {
        ft.visible = false;
    });
    
    // 推进头指针（FIFO 环形）
    _floatingTextHead = (_floatingTextHead + 1) % MAX_FLOATING_TEXTS;
    
#if UNITY_EDITOR
    Debug_ActiveFloatingTextCount = CountActiveFloatingTexts();
#endif
}

/// <summary>重试时批量回收所有飘字</summary>
public void RecycleAllFloatingTexts()
{
    for (int i = 0; i < MAX_FLOATING_TEXTS; i++)
    {
        if (_floatingTexts[i] != null)
            _floatingTexts[i].visible = false;
    }
    _floatingTextHead = 0;
}

/// <summary>兜底强制同步（重试后调用）</summary>
public void ForceRefresh()
{
    _displayHP = _targetHP = _baseHP.Value;
    UpdateHPBarFill(_targetHP);
    UpdatePreDamageBar(_displayHP);
    UpdateHPBarColor(_targetHP);
    UpdateHPText(_targetHP);
    RecycleAllFloatingTexts();
}
```

---

## 5. PausePanelController

```csharp
namespace Game.ShooterGame.UI
{
    public class PausePanelController : MonoBehaviour
    {
        private GComponent _view;
        
        public event Action OnResume;
        public event Action OnQuit;
        
        public void Show()
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject("Popup", "PausePanel").asCom;
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                _view.GetChild("btn_resume").asButton.onClick.Add(OnResumeClicked);
                _view.GetChild("btn_quit").asButton.onClick.Add(OnQuitClicked);
            }
            
            Time.timeScale = 0f;
            _view.visible = true;
            PlayPopupAnimation();
        }
        
        public void Hide()
        {
            Time.timeScale = 1f;
            _view.visible = false;
        }
        
        private void OnResumeClicked()
        {
            Hide();
            OnResume?.Invoke();
        }
        
        private void OnQuitClicked()
        {
            Time.timeScale = 1f;
            OnQuit?.Invoke();
        }
    }
}
```

---

## 6. VictoryPanelController

```csharp
namespace Game.ShooterGame.UI
{
    public class VictoryPanelController : MonoBehaviour
    {
        [SerializeField] private IntVariable _killCount;
        [SerializeField] private FloatVariable _baseHP;
        
        private GComponent _view;
        
        public event Action OnConfirm;
        
        public void Show()
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject("Popup", "VictoryPanel").asCom;
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                _view.GetChild("btn_confirm").asButton.onClick.Add(OnConfirmClicked);
            }
            
            // 填充数据
            _view.GetChild("text_kills").asTextField.text = $"击杀数：{_killCount.Value}";
            _view.GetChild("text_hp").asTextField.text = $"剩余血量：{Mathf.RoundToInt(_baseHP.Value * 100)}%";
            
            _view.visible = true;
            PlayVictoryAnimation();
        }
        
        private void OnConfirmClicked()
        {
            _view.visible = false;
            OnConfirm?.Invoke();
        }
    }
}
```

---

## 7. DefeatPanelController

```csharp
namespace Game.ShooterGame.UI
{
    public class DefeatPanelController : MonoBehaviour
    {
        [SerializeField] private IntVariable _killCount;
        [SerializeField] private IntVariable _totalEnemyCount;
        
        private GComponent _view;
        
        // 鼓励文案池
        private static readonly string[] ENCOURAGE_TEXTS = {
            "再来一次！", "差一点！", "你能行！", "这次更近了！"
        };
        
        public event Action OnRetry;
        public event Action OnQuit;
        
        public void Show()
        {
            if (_view == null)
            {
                _view = UIPackage.CreateObject("Popup", "DefeatPanel").asCom;
                GRoot.inst.AddChild(_view);
                _view.MakeFullScreen();
                _view.GetChild("btn_retry").asButton.onClick.Add(OnRetryClicked);
                _view.GetChild("btn_quit").asButton.onClick.Add(OnQuitClicked);
            }
            
            // 填充数据
            _view.GetChild("text_progress").asTextField.text = 
                $"消灭了 {_killCount.Value}/{_totalEnemyCount.Value} 架";
            _view.GetChild("text_encourage").asTextField.text = 
                ENCOURAGE_TEXTS[UnityEngine.Random.Range(0, ENCOURAGE_TEXTS.Length)];
            
            _view.visible = true;
            PlayDefeatAnimation();
        }
        
        private void OnRetryClicked()
        {
            _view.visible = false;
            OnRetry?.Invoke();
        }
        
        private void OnQuitClicked()
        {
            _view.visible = false;
            OnQuit?.Invoke();
        }
    }
}
```

---

## 8. 转场编排

### 8.1 BattleController 中的转场协调

```csharp
// 选关→战斗（由 LevelSelectController 触发 SceneManager.LoadScene）

// 战斗→选关（胜利后）
private IEnumerator HandleVictoryConfirm()
{
    // WX-009: 检查存储返回值
    bool saved = _progressManager.MarkLevelCleared(_currentLevelIndex.Value + 1);  // 1-based
    if (!saved)
    {
        // 存储失败：Toast 提示 + 内存中进度保留（本次会话有效）
        ShowToast("进度保存失败，请检查存储空间");
    }
    yield return StartCoroutine(TransitionOut(0.4f));
    SceneManager.LoadScene("Boot");
    // Boot 场景的 LevelSelectController.Show(newlyUnlockedLevel) 处理解锁动效
}

// 战斗→选关（失败返回）
private IEnumerator HandleDefeatQuit()
{
    yield return StartCoroutine(TransitionOut(0.3f));
    SceneManager.LoadScene("Boot");
}

// 重试（不换场景）
private IEnumerator HandleRetry()
{
    yield return StartCoroutine(BlackScreenFadeIn(0.2f));
    ResetBattle();  // 重置 Entity + SO + UI
    yield return StartCoroutine(BlackScreenFadeOut(0.2f));
    EnterState(BattleState.Intro);
}
```

### 8.2 转场时序表

| 转场 | Step 1 | Step 2 | Step 3 | 总时长 |
|------|--------|--------|--------|--------|
| 加载→选关 | 淡出 LoadingScreen 0.3s | — | — | 0.3s |
| 选关→战斗 | LevelNode 缩放 0.2s | 白闪 0.1s | LoadScene + 淡入 0.2s | 0.5s |
| 胜利→选关 | VictoryPanel 淡出 0.2s | TransitionOut 0.2s | LoadScene "Boot" | 0.4s |
| 失败→选关 | DefeatPanel 滑出 0.2s | 淡入选关 0.1s | — | 0.3s |
| 重试 | 黑屏淡入 0.2s | 重置(1帧) | 黑屏淡出 0.2s | 0.4s |

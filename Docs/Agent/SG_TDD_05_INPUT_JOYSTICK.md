# SG_TDD_05: 输入与虚拟摇杆

> 父文档：[SG_TDD_INDEX.md](SG_TDD_INDEX.md)

---

## 1. Vector2Variable（框架层新增）

### 1.1 类设计

```csharp
// 路径：Assets/_Framework/DataSystem/Scripts/Variables/Vector2Variable.cs
// 命名空间：MiniGameTemplate.Data
// 与 FloatVariable / IntVariable 完全对称

namespace MiniGameTemplate.Data
{
    [CreateAssetMenu(menuName = "MiniGameTemplate/Variables/Vector2", order = 2)]
    public class Vector2Variable : ScriptableObject
    {
        [SerializeField] private Vector2 _initialValue;
        [SerializeField] private Vector2 _value;
        
        public event Action<Vector2> OnValueChanged;
        
        public Vector2 Value
        {
            get => _value;
            set
            {
                // ET-010 设计决策：使用 Unity 默认 Vector2 == 容差（~1e-5）
                // 对于摇杆场景，死区内输出恒为 Zero，死区外输出归一化方向——
                // 微小变化被吞掉不影响游戏体验（移动方向只需角度精度，不需 sub-pixel 精度）
                if (_value == value) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
        
        public void SetValue(Vector2 value) => Value = value;
        
        public void ResetToInitial() => Value = _initialValue;
        
        private void OnEnable()
        {
            _value = _initialValue;
        }
        
#if UNITY_EDITOR
        [ContextMenu("Reset to Initial Value")]
        private void EditorReset() => ResetToInitial();
#endif
    }
}
```

### 1.2 放置位置

- 文件路径：`Assets/_Framework/DataSystem/Scripts/Variables/Vector2Variable.cs`
- 属于框架层（`MiniGameTemplate.Data` 命名空间），非 Game 层
- 需在 DataSystem asmdef 中，自动包含

---

## 2. JoystickConfigSO

### 2.1 类设计

```csharp
namespace Game.ShooterGame
{
    /// <summary>
    /// 虚拟摇杆全参数 SO 化——Play Mode 中 Inspector 修改即时生效。
    /// 来源：UI §2.3 + §3.5
    /// </summary>
    [CreateAssetMenu(menuName = "ShooterGame/JoystickConfig")]
    public class JoystickConfigSO : ScriptableObject
    {
        [Header("操控参数")]
        [Tooltip("死区半径（pt）——偏移 < 此值时不响应移动")]
        public float DeadZone = 8f;
        
        [Tooltip("最大偏移半径（pt）——超过时摇杆头钳制在边缘")]
        public float MaxRadius = 60f;
        
        [Header("视觉参数")]
        [Tooltip("底座透明度")]
        [Range(0f, 1f)]
        public float Alpha_Base = 0.3f;
        
        [Tooltip("摇杆头透明度")]
        [Range(0f, 1f)]
        public float Alpha_Stick = 0.6f;
        
        [Tooltip("底座直径（pt）")]
        public float BaseDiameter = 120f;
        
        [Tooltip("摇杆头直径（pt）")]
        public float StickDiameter = 50f;
        
        [Header("动效")]
        [Tooltip("出现动效时长（秒）")]
        public float AppearDuration = 0.1f;
        
        [Tooltip("消失动效时长（秒）")]
        public float DisappearDuration = 0.15f;
    }
}
```

---

## 3. JoystickController

### 3.1 方案确认

- ✅ **FairyGUI 全屏 GGraph** 作为触摸响应区（z 低于 HUD 元素）
- ❌ ~~全屏 MonoBehaviour + Input.GetTouch()~~（微信 WebGL 不可靠）
- 暂停按钮 z 高于 GGraph → 事件分发自然解决冲突

### 3.2 类设计

```csharp
namespace Game.ShooterGame.UI
{
    /// <summary>
    /// 虚拟摇杆控制器——基于 FairyGUI 触摸事件。
    /// 每帧将方向向量写入 SG_InputDirection Vector2Variable。
    /// </summary>
    public class JoystickController : MonoBehaviour
    {
        [SerializeField] private JoystickConfigSO _config;
        [SerializeField] private Vector2Variable _inputDirection;
        
        // FairyGUI 元素
        private GGraph _touchArea;       // 全屏触摸区
        private GComponent _joystickBase; // 底座
        private GComponent _joystickStick; // 摇杆头
        
        // 状态
        private bool _isActive;
        private Vector2 _touchOrigin;    // 按下时的位置（摇杆中心）
        private bool _inputEnabled = true;
        
        public void Init(GComponent battleHUD)
        {
            // 创建全屏 GGraph 作为触摸区
            _touchArea = new GGraph();
            _touchArea.SetSize(GRoot.inst.width, GRoot.inst.height);
            _touchArea.touchable = true;
            _touchArea.sortingOrder = 50;  // 低于 HUD
            battleHUD.AddChild(_touchArea);
            
            // 创建底座和摇杆头
            _joystickBase = UIPackage.CreateObject("Battle", "Joystick").asCom;
            _joystickBase.visible = false;
            battleHUD.AddChild(_joystickBase);
            
            _joystickStick = _joystickBase.GetChild("stick").asCom;
            
            // 绑定事件
            _touchArea.onTouchBegin.Add(OnTouchBegin);
            _touchArea.onTouchMove.Add(OnTouchMove);
            _touchArea.onTouchEnd.Add(OnTouchEnd);
        }
        
        public void SetEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled && _isActive)
            {
                Deactivate();
            }
        }
        
        // ── 触摸处理 ──
        
        private void OnTouchBegin(EventContext context)
        {
            if (!_inputEnabled) return;
            
            var touch = context.inputEvent;
            _touchOrigin = new Vector2(touch.x, touch.y);
            _isActive = true;
            
            // 显示摇杆（在按下位置）
            ShowJoystick(_touchOrigin);
        }
        
        private void OnTouchMove(EventContext context)
        {
            if (!_isActive) return;
            
            var touch = context.inputEvent;
            Vector2 currentPos = new Vector2(touch.x, touch.y);
            Vector2 delta = currentPos - _touchOrigin;
            float distance = delta.magnitude;
            
            // 死区检测
            if (distance < _config.DeadZone)
            {
                _inputDirection.SetValue(Vector2.zero);
                UpdateStickVisual(Vector2.zero);
                return;
            }
            
            // 方向归一化
            Vector2 direction = delta.normalized;
            
            // 钳制摇杆头在最大半径内
            float clampedDistance = Mathf.Min(distance, _config.MaxRadius);
            Vector2 stickOffset = direction * clampedDistance;
            
            // 写入 SO
            _inputDirection.SetValue(direction);
            
            // 更新视觉
            UpdateStickVisual(stickOffset);
        }
        
        private void OnTouchEnd(EventContext context)
        {
            if (!_isActive) return;
            Deactivate();
        }
        
        private void Deactivate()
        {
            _isActive = false;
            _inputDirection.SetValue(Vector2.zero);
            HideJoystick();
        }
        
        // ── 视觉更新 ──
        
        private void ShowJoystick(Vector2 center)
        {
            _joystickBase.SetPosition(
                center.x - _config.BaseDiameter * 0.5f,
                center.y - _config.BaseDiameter * 0.5f, 0);
            _joystickBase.alpha = _config.Alpha_Base;
            _joystickBase.visible = true;
            _joystickBase.TweenScale(new Vector2(1, 1), _config.AppearDuration)
                         .SetValue(new Vector2(0, 0));
        }
        
        private void HideJoystick()
        {
            _joystickBase.TweenFade(0f, _config.DisappearDuration)
                         .OnComplete(() => { _joystickBase.visible = false; });
        }
        
        private void UpdateStickVisual(Vector2 offset)
        {
            // 摇杆头相对底座中心的偏移
            float centerX = _config.BaseDiameter * 0.5f;
            float centerY = _config.BaseDiameter * 0.5f;
            _joystickStick.SetPosition(
                centerX + offset.x - _config.StickDiameter * 0.5f,
                centerY + offset.y - _config.StickDiameter * 0.5f, 0);
        }
    }
}
```

---

## 4. SG_PlayerInputBridge

### 4.1 职责

读取 `SG_InputDirection` SO → 写入玩家 Entity 的 `MovementComponent`。
桥接 UI 层（摇杆）和 Entity 层（移动），保持双方解耦。

### 4.2 类设计

```csharp
namespace Game.ShooterGame
{
    /// <summary>
    /// 摇杆→Entity 移动桥接。
    /// 每帧读取 SG_InputDirection，写入玩家 Entity 的 MovementComponent。
    /// 挂载在 Battle 场景中。
    /// </summary>
    public class SG_PlayerInputBridge : MonoBehaviour
    {
        [SerializeField] private Vector2Variable _inputDirection;
        
        private Entity _playerEntity;
        private MovementComponent _movement;
        
        /// <summary>由 BattleController 调用，传入玩家 Entity</summary>
        public void Init(Entity playerEntity)
        {
            _playerEntity = playerEntity;
            _movement = playerEntity.GetComponent(ComponentType.Movement) as MovementComponent;
            
            if (_movement == null)
                Debug.LogError("[SG_PlayerInputBridge] Player Entity 缺少 MovementComponent!");
        }
        
        private void Update()
        {
            if (_movement == null) return;
            
            // 读取 SO 值，写入 MovementComponent
            // 注意：摇杆输出的是 UI 坐标方向（x 右正，y 下正）
            // Entity 世界坐标 y 轴向上，需要翻转 y
            Vector2 input = _inputDirection.Value;
            
            // FairyGUI 触摸坐标 y 轴向下，需翻转为世界坐标
            Vector2 worldDir = new Vector2(input.x, -input.y);
            
            _movement.SetMoveDirection(worldDir);
        }
        
        /// <summary>禁用输入（Intro/结算状态调用）</summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
                _movement?.SetMoveDirection(Vector2.zero);
        }
    }
}
```

### 4.3 Y 轴翻转结论

| 坐标系 | X 正方向 | Y 正方向 |
|--------|---------|---------|
| FairyGUI 触摸 (`inputEvent.x/y`) | 右 | **下**（左上角为原点） |
| Unity 世界坐标 | 右 | **上** |

**确定结论**：FairyGUI 的 `inputEvent.x/y` 使用屏幕坐标系（左上角原点，Y 向下），
与 Unity 世界坐标 Y 轴方向相反。因此**必须翻转 Y 轴**：

```csharp
// 摇杆向上拖 → delta.y < 0（FairyGUI Y 减小 = 屏幕向上）
// 翻转后 worldDir.y > 0 → 飞机向上移动 ✅
Vector2 worldDir = new Vector2(input.x, -input.y);
```

> **依据**：FairyGUI 官方文档明确 `Stage.touchPosition` 和 `InputEvent.x/y` 使用
> 左上角原点坐标系（与 Unity ScreenToWorldPoint 的左下角原点不同）。

---

## 5. 输入管线时序

```
每帧时序：

1. FairyGUI Input → JoystickController.OnTouchMove
   → SG_InputDirection.SetValue(direction)

2. SG_PlayerInputBridge.Update()
   → 读取 SG_InputDirection.Value
   → MovementComponent.SetMoveDirection(worldDir)

3. EntitySystemBootstrap.Update()
   → EntityManager.Tick(dt)
     → MovementComponent.Tick(dt)
       → Entity.Position += direction * speed * dt

4. EntitySystemBootstrap.Update() (续)
   → ClampPlayerPositions()
     → 确保飞机在可视区域内
```

---

## 6. 暂停按钮冲突解决

| 场景 | 行为 |
|------|------|
| 手指在暂停按钮区域按下 | 暂停按钮响应（z 更高，优先拦截） |
| 手指在其他区域按下 | 摇杆激活 |
| 摇杆激活后手指拖入暂停按钮区域 | 摇杆继续响应（首帧判定规则） |
| 暂停状态 | JoystickController.SetEnabled(false) |

---

## 7. 行为契约

| ID | 契约 | 验证方式 |
|----|------|---------|
| SG-BC-10 | 摇杆输出方向始终归一化或零向量 | JoystickController 内 normalize |
| SG-BC-11 | 松手后 SG_InputDirection 立即归零 | OnTouchEnd → SetValue(Zero) |
| SG-BC-12 | BattleState != Playing 时输入被禁用 | BattleController 状态转换时调用 SetEnabled |
| SG-BC-13 | Vector2Variable 值变化才触发事件 | == 比较避免重复通知 |
| SG-BC-14 | 摇杆视觉不遮挡 HUD 信息 | z=50 < HUD 元素 z |

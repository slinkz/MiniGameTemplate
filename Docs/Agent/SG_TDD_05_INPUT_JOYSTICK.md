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

### 3.2 1:1 跟手模式（v2 迭代）

**设计决策**：绕过速度系统，手指移多少飞机就移多少。

- `OnTouchMove` 输出**原始像素帧间 delta**（`currentPos - lastTouchPos`），不做归一化
- 抖动过滤：`frameDelta.sqrMagnitude < 1f` 时忽略（死区 1px²）
- 摇杆视觉保持独立逻辑（显示偏移量 clamp 到 MaxRadius）
- 松手 / 不动 → delta 恒为 `Vector2.zero`

```csharp
// 核心逻辑（简化）
private void OnTouchMove(EventContext context)
{
    Vector2 currentPos = new Vector2(touch.x, touch.y);
    Vector2 frameDelta = currentPos - _lastTouchPos;
    _lastTouchPos = currentPos;

    // 抖动过滤
    if (frameDelta.sqrMagnitude < 1f)
    {
        _inputDirection.SetValue(Vector2.zero);
        return;
    }

    // 输出原始像素 delta（Bridge 消费后清零）
    _inputDirection.SetValue(frameDelta);
}
```

---

## 4. SG_PlayerInputBridge

### 4.1 职责

读取 `SG_InputDirection`（像素 delta）→ 换算为世界坐标偏移 → 直接设置玩家 Entity 位置。
同时常驻 `ControlComponent.SetAttackInput(true)` + `SetAimInput(Vector2.up)`（全自动射击）。
桥接 UI 层（摇杆）和 Entity 层（位置），保持双方解耦。

### 4.2 1:1 跟手模式

**设计决策**：绕过 `MovementComponent` 的速度系统，直接做位置偏移。

```csharp
namespace Game.ShooterGame
{
    public class SG_PlayerInputBridge : MonoBehaviour
    {
        [SerializeField] private Vector2Variable _inputDirection;
        [SerializeField] private DanmakuWorldConfig _worldConfig;

        private Entity _playerEntity;
        private MovementComponent _movement;
        private ControlComponent _control;

        // 屏幕像素→世界坐标换算系数（运行时计算一次）
        private float _pixelToWorldX;
        private float _pixelToWorldY;

        public void Init(Entity playerEntity)
        {
            _playerEntity = playerEntity;
            _movement = playerEntity.GetComponent(ComponentType.Movement) as MovementComponent;
            _control = playerEntity.GetComponent(ComponentType.Control) as ControlComponent;

            // 全自动射击 + 禁止速度系统移动转发
            if (_control != null)
            {
                _control.SetAttackInput(true);
                _control.SetAimInput(Vector2.up);
                _control.SuppressMovement = true; // 位置由 Bridge 直接设置
            }

            // 像素→世界换算系数
            float screenW = GRoot.inst.width;
            float screenH = GRoot.inst.height;
            float worldW = _worldConfig != null ? _worldConfig.WorldBounds.width : 12f;
            float worldH = _worldConfig != null ? _worldConfig.WorldBounds.height : 20f;
            _pixelToWorldX = worldW / screenW;
            _pixelToWorldY = worldH / screenH;
        }

        private void Update()
        {
            Vector2 input = _inputDirection.Value;
            if (input.sqrMagnitude < 0.01f) return;

            _inputDirection.SetValue(Vector2.zero); // 消费后立即清零

            if (_movement == null) return;

            // 像素 delta → 世界坐标偏移（FairyGUI y↓ → 世界 y↑，翻转 y）
            float worldDx = input.x * _pixelToWorldX;
            float worldDy = -input.y * _pixelToWorldY;

            Vector2 newPos = _playerEntity.Position + new Vector2(worldDx, worldDy);

            // 边界钳制
            if (_worldConfig != null)
            {
                Rect bounds = _worldConfig.WorldBounds;
                newPos.x = Mathf.Clamp(newPos.x, bounds.xMin, bounds.xMax);
                newPos.y = Mathf.Clamp(newPos.y, bounds.yMin, bounds.yMax);
            }

            _movement.SetPosition(newPos); // 直接设位置（触发 OnPositionChanged）
        }
    }
}
```

### 4.3 关键设计要点

| 要点 | 说明 |
|------|------|
| 消费后清零 | `_inputDirection.SetValue(Vector2.zero)` 防止残留 delta 下帧重复生效 |
| SuppressMovement | 告诉 ControlComponent 不要每帧写 SetMoveDirection(0,0)，避免架构冲突 |
| SetPosition 发事件 | v2 修复后，`SetPosition()` 会发布 `OnPositionChanged`，与 Tick 路径一致 |
| 边界钳制 | 用 `DanmakuWorldConfig.WorldBounds`，不需要额外 Clamp 系统 |
| null fallback | `_worldConfig == null` 时用硬编码 12×20，不崩溃（仅 Editor 漏配时） |

---

## 5. 输入管线时序（1:1 跟手模式）

```
每帧时序：

1. FairyGUI Input → JoystickController.OnTouchMove
   → 计算帧间像素 delta（currentPos - lastTouchPos）
   → SG_InputDirection.SetValue(pixelDelta)

2. SG_PlayerInputBridge.Update()
   → 读取 SG_InputDirection.Value（像素 delta）
   → 消费后清零 SetValue(Vector2.zero)
   → 像素 delta × (worldSize / screenSize) → 世界坐标偏移
   → newPos = Entity.Position + worldOffset
   → 边界钳制（WorldBounds）
   → MovementComponent.SetPosition(newPos) → 触发 OnPositionChanged

3. ControlComponent.Tick()（TickOrder=100）
   → SuppressMovement=true → 跳过 SetMoveDirection（不干扰直接位置模式）
   → 仍然传递 WantsAttack=true + AimDirection=up

4. EntityViewBridge.SyncAll()
   → 从 Entity.Position 读取新位置 → 同步到 Transform
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
| SG-BC-10 | 摇杆输出原始像素 delta（不归一化），松手后恒为零 | JoystickController.OnTouchEnd → SetValue(Zero) |
| SG-BC-11 | Bridge 消费后立即清零 SO，避免残留 | Update 开头 SetValue(Vector2.zero) |
| SG-BC-12 | BattleState != Playing 时输入被禁用 | BattleController 状态转换时调用 SetEnabled |
| SG-BC-13 | Vector2Variable 值变化才触发事件 | == 比较避免重复通知 |
| SG-BC-14 | 摇杆视觉不遮挡 HUD 信息 | z=50 < HUD 元素 z |
| SG-BC-15 | SetPosition 触发 OnPositionChanged 事件 | MovementComponent.SetPosition 内部发布 |
| SG-BC-16 | SuppressMovement=true 时速度系统不干扰位置 | ControlComponent.Tick 跳过 SetMoveDirection |

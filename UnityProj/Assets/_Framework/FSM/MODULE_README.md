# FSM 模块

## 用途
基于 ScriptableObject 的简易有限状态机。用于管理游戏全局状态（Menu → Playing → Paused → GameOver）。

## 核心类
| 类 | 用途 |
|---|------|
| `State` | [CreateAssetMenu] 状态SO，定义一个状态 |
| `StateTransition` | [CreateAssetMenu] 转换SO，定义从A到B的转换条件 |
| `StateMachine` | 状态机组件，持有当前状态并处理转换 |

## 预置状态
- `State_Menu` — 主菜单
- `State_Playing` — 游戏进行中
- `State_Paused` — 游戏暂停
- `State_GameOver` — 游戏结束

## 使用方式
```csharp
[SerializeField] private StateMachine _gameFSM;
[SerializeField] private State _playingState;

void StartGame() {
    _gameFSM.TransitionTo(_playingState);
}
```

## 状态变更事件
StateMachine 在状态切换时会 Raise 对应的 GameEvent（如果配置了）。
可在 Inspector 中为每个 State 指定 OnEnter/OnExit 事件。

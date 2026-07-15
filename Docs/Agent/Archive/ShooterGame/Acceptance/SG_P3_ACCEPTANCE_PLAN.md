---
system: shootergame
scope: p3-fairygui-acceptance
last_verified: 2026-05-04
depends_on: [SG_TDD_04, SG_UI_DESIGN]
related_code: UIProject/assets/SG_*/**/*, Assets/_Game/Scripts/ShooterGame/UI/*.cs
---

# SG-P3 验收计划：FairyGUI 白模包

> **版本**：v1.0 | **日期**：2026-05-04

---

## 产出清单

### FairyGUI 白模 XML（16 个文件，4 个包）

| 包目录 | publish name | 文件数 | 主组件 |
|--------|-------------|--------|--------|
| `UIProject/assets/SG_Loading/` | Loading | 2 | LoadingScreen |
| `UIProject/assets/SG_LevelSelect/` | LevelSelect | 3 | LevelSelectScreen + LevelNode |
| `UIProject/assets/SG_Battle/` | Battle | 5 | BattleHUD + FloatingText + Joystick + SG_PrimaryButton |
| `UIProject/assets/SG_Popup/` | Popup | 6 | PausePanel + VictoryPanel + DefeatPanel + SG_PopupButton + SG_SecondaryButton |

### C# 代码修改（2 个文件）

| 文件 | 改动 |
|------|------|
| `BattleController.cs` | 暂停按钮绑定 + OnDestroy 清理 + hudView 局部变量复用 |
| `JoystickController.cs` | `_joystickStick` 类型 GComponent → GObject（匹配 XML graph 元素） |

### 文档更新（2 个文件）

| 文件 | 改动 |
|------|------|
| `SHOOTER_GAME/TDD/SG_TDD_04_UI_CONTROLLERS.md` | 新增 Frontmatter + §4.1 暂停按钮绑定说明 + §4.2 白模包对照表 |
| `INDEX.md` | 路由表 B 新增 FairyGUI 白模路径 |

---

## 验收步骤

### A1：FairyGUI 编辑器导入验证

**前置条件**：安装 FairyGUI 编辑器（v2024.x）

1. 打开 FairyGUI 编辑器
2. 打开项目：`MiniGameTemplate/UIProject/` 下的 `.fairy` 工程文件
3. 确认左侧资源树出现 4 个新包：**SG_Loading**、**SG_LevelSelect**、**SG_Battle**、**SG_Popup**
4. **逐包检查**：
   - 双击每个包根组件（LoadingScreen / LevelSelectScreen / BattleHUD / PausePanel / VictoryPanel / DefeatPanel）
   - 确认子元素名称与下表一致：

| 组件 | 必须存在的子元素名 |
|------|-------------------|
| LoadingScreen | `bar` |
| LevelSelectScreen | `node_1`, `node_2`, `node_3`, `node_4`, `node_5` |
| BattleHUD | `hp_bar`, `text_wave`, `btn_pause_bg`, `btn_pause_text` |
| FloatingText | `text` |
| Joystick | `base`, `stick` |
| PausePanel | `btn_resume`, `btn_quit` |
| VictoryPanel | `btn_confirm`, `text_kills`, `text_hp` |
| DefeatPanel | `btn_retry`, `btn_quit`, `text_progress`, `text_encourage` |

**通过标准**：✅ 所有包正常加载，无 XML 解析错误，子元素名全部匹配

### A2：FairyGUI 发布

1. 在 FairyGUI 编辑器中，依次选中 4 个包
2. 点击"发布"（Publish），目标路径：`MiniGameTemplate/UnityProj/Assets/Resources/FairyGUI/`
3. 确认输出 4 组 `_fui.bytes` 文件：
   - `Loading_fui.bytes`
   - `LevelSelect_fui.bytes`
   - `Battle_fui.bytes`
   - `Popup_fui.bytes`

**通过标准**：✅ 发布成功，无警告，bytes 文件大小 > 0

### A3：Unity PlayMode 冒烟测试

1. 回到 Unity Editor
2. 确认 `Resources/FairyGUI/` 下有新发布的 bytes 文件
3. 打开 Boot 场景，进入 Play Mode
4. **观察 Loading 界面**：
   - 应出现全屏 Loading 界面（深色背景 + 进度条 + 加载文字）
   - 进度条应有动画效果
5. **自动跳转选关界面**：
   - 加载完成后应看到 5 个关卡节点（纵向排列）
   - 关卡 1 可点击，其余锁定
6. **点击关卡 1 进入战斗**：
   - 应看到 BattleHUD（底部血条 + 顶部波次 + 右上暂停按钮）
   - 摇杆触摸区域应覆盖屏幕下半部分
7. **点击暂停按钮**：
   - 应弹出 PausePanel（半透明遮罩 + "继续" 和 "返回" 按钮）
   - 游戏应暂停（Time.timeScale = 0）
8. **战斗结束**：
   - 胜利：应出现 VictoryPanel（击杀数 + 剩余血量 + 确定按钮）
   - 失败：应出现 DefeatPanel（进度 + 鼓励文字 + 重试大按钮 + 返回按钮）

**通过标准**：✅ UI 正常显示，子元素名绑定无报错（Console 无 NullReferenceException）

### A4：暂停按钮事件验证

1. 在战斗中点击右上角暂停按钮（蓝色圆形区域）
2. 确认 PausePanel 弹出
3. 点击"继续"按钮，确认游戏恢复
4. 再次暂停，点击"返回"按钮，确认跳回选关界面

**通过标准**：✅ 暂停/恢复/返回全链路正常

### A5：摇杆操控验证

1. 在战斗中，用鼠标在屏幕下半部分拖拽
2. 确认摇杆底座+摇杆头正确显示在按下位置
3. 拖拽方向应与飞机移动方向一致
4. 松手后摇杆消失，飞机停止移动

**通过标准**：✅ 摇杆交互正常，无 NullRef 报错

---

## 已知限制（P3 范围外）

- **白模视觉**：所有界面使用纯色矩形/圆形占位，无精美美术资源（P4 或后续迭代美化）
- **关卡节点动效**：LevelNode 三态切换动效为 FairyGUI Controller 内置，编辑器中预览可能不同于运行时
- **转场动画**：白闪/淡入淡出效果在代码中实现，FairyGUI 层面仅提供容器

---

## 快速排障

| 症状 | 可能原因 | 处理 |
|------|---------|------|
| FairyGUI 编辑器打不开包 | XML 格式异常 | 检查 `package.xml` 的 `<resources>` 和 `<publish>` 节点 |
| Unity 中 UI 不显示 | bytes 未发布到正确路径 | 检查 `Resources/FairyGUI/` 目录 |
| NullReferenceException | 子元素名不匹配 | 对照上方 A1 的名称表 |
| 暂停按钮无反应 | `btn_pause_bg` 名字不对或 touchable=false | 检查 BattleHUD.xml 中 graph 的 touchable 属性 |
| 摇杆 stick 报错 | `asCom` 类型不匹配 | 确认已用最新代码（GObject 而非 GComponent） |

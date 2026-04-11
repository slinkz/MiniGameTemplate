# VFXDemo

阶段 2 的最小 Sprite Sheet VFX 验证示例。

## 目标
- 验证共享图集 + VFXTypeSO + SpriteSheetVFXSystem 的闭环
- 不追求美术质量，先验证运行链路

## 使用步骤
1. 在 Unity 菜单执行：`Tools/MiniGame Template/VFX/Create Stage2 Demo Assets`
2. 打开 `Scenes/VFXDemo.unity`
3. 场景中应只有以下根对象：
   - `Main Camera`
   - `Directional Light`
   - `SpriteSheetVFXSystemRoot`
   - `VFXDemoSpawnerRoot`
   - `VFXDemoUIRoot`
4. 选中 `SpriteSheetVFXSystemRoot`，确认 `SpriteSheetVFXSystem` 已绑定：
   - `Config/VFXRenderConfig_Demo.asset`
   - `Registry/VFXTypeRegistry_Demo.asset`
5. 选中 `VFXDemoSpawnerRoot`，确认 `VFXDemoSpawner` 已绑定：
  - `Type/VFXType_Explosion_Test.asset`
  - `Type/VFXType_Explosion_Blue.asset`
  - `Type/VFXType_HealFlash_Test.asset`
  - `Type/VFXType_SmokePuff_Test.asset`
  - `Type/VFXType_DissolveAfterimage_Test.asset`
  - `SpriteSheetVFXSystemRoot` 上的 `SpriteSheetVFXSystem` 组件引用

6. 选中 `VFXDemoUIRoot`，确认 `VFXDemoInputHint` 已绑定：
   - `VFXDemoSpawnerRoot` 上的 `VFXDemoSpawner` 组件引用
7. 确认 `Main Camera` 位置为 `(0, 0, -10)`
8. 点击 Play，观察一排特效轮播，并验证：
   - `R`：从第一列重新开始播放
   - `Space`：立即补一发
   - `1`：切到顺序轮播类型
   - `2`：切到随机播放类型
   - `3`：固定播放第 1 个类型（默认爆炸）
   - `4`：固定播放第 2 个类型（蓝色爆炸）
  - `5`：固定播放第 3 个类型（治疗闪光验证版）
  - `6`：固定播放第 4 个类型（烟雾 puff 验证版）
  - `7`：固定播放第 5 个类型（消散残影验证版）
  - `Esc`：返回主菜单



## 模板接入 SOP
### 新增一个 VFX 示例时，最小闭环必须包含
1. 一个独立场景（不要把测试对象直接堆进 `Boot`）
2. 一个 `SpriteSheetVFXSystemRoot`
   - 挂 `SpriteSheetVFXSystem`
   - 绑定一份 `VFXRenderConfig` 资产
   - 绑定一份 `VFXTypeRegistry` 资产
3. 一个播放入口对象（例如 `VFXDemoSpawnerRoot`）
   - 挂单一职责的播放控制脚本
   - 通过 Inspector 引用 `SpriteSheetVFXSystem`
   - 通过 Inspector 引用目标 `VFXTypeSO`
4. 一个场景内交互对象（例如 `VFXDemoUIRoot`）
   - 挂 `ExampleSceneHotkeys` 负责返回主菜单与左上角说明文字
   - 再挂 Demo 专用输入脚本（例如 `VFXDemoInputHint`）负责额外快捷键
   - 不要把播放逻辑继续塞回输入脚本
5. Build Settings 中注册该场景
6. 主菜单增加入口，并同步 FairyGUI 源文件与导出代码

### 复用边界
- `VFXRenderConfig_Demo.asset`：偏 Demo 级渲染配置，可复制后按项目改材质/贴图
- `VFXTypeRegistry_Demo.asset`：偏 Demo 级类型注册表，建议每个示例/玩法各自维护，避免把无关类型堆成大杂烩
- `VFXType_Explosion_Test.asset` / `VFXType_Explosion_Blue.asset`：示例类型资产，分别用于验证默认爆炸与变体爆炸；其中蓝色变体使用更深的蓝色 Tint + 更大尺寸，并切到 Normal 层，便于肉眼明确区分多类型切换效果；后续新特效类型可直接复制其一作为起点模板
- `VFXDemoSpawner`：当前是 Demo 专用播放控制，不应直接演化成通用 VFX 管理器
- `VFXDemoInputHint`：当前是示例场景交互壳，后续若多个示例都要返回/提示逻辑，再抽公共组件

## 说明
- **不要在 `Boot` 场景里做 VFX 阶段2验证**。`Boot` 是启动场景，容易混入启动流程并污染场景状态
- 正式验证场景为：`Assets/_Example/VFXDemo/Scenes/VFXDemo.unity`
- 当前图集为程序生成的占位资源，目的是验证系统闭环
- 后续可直接替换为 AI 生成的真实 Sprite Sheet
- 主菜单已新增 `特效Demo` 按钮，可直接进入此场景


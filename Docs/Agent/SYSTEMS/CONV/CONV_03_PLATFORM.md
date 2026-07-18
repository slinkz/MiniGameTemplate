---
system: conventions
scope: platform-rendering
last_verified: 2026-05-02
depends_on: [CONV_02_CODING]
related_code: Assets/_Framework/RuntimeAtlas/*.cs, Assets/_Framework/Danmaku/*.cs
---


### 内存
- **总堆上限约 256MB**（WeChat 实际更低），加载大纹理前检查内存余量
- 场景切换后调用 `AssetService.Instance.UnloadUnusedAssetsAsync()`
- 纹理最大 1024px（AssetImportEnforcer 自动限制）

### 渲染
- 使用 Built-in Render Pipeline（非 URP/HDRP）
- 小心 `OnGUI` 在 release 构建中的开销（仅 Debug 工具使用，用 `#if` 守卫）
- Draw Call 预算：尽量 < 50 DC（移动 WebGL）

### 音频
- 短音效强制 Mono（AudioImportEnforcer 自动处理）
- 压缩格式: Vorbis，质量 50%
- 加载方式: CompressedInMemory

### 文件 I/O
- WebGL 无文件系统，**禁止 `System.IO`**
- 持久化只用 `PlayerPrefs`（通过 `ISaveSystem` 接口）
- 配置数据通过 `ConfigManager` 加载二进制 `.bytes`（仅 YooAsset，无 Resources fallback）

---

## [AGENT] 安全编码规范

### 输入验证
```csharp
// ✅ 文件名/路径验证——防止路径穿越
if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\") || fileName.Contains("\0"))
{
    Debug.LogError($"[System] SEC: Invalid file name rejected: '{fileName}'");
    return null;
}

// ✅ 数值范围钳制
health = Mathf.Clamp(health, 0, MAX_HEALTH);
```

### 网络安全
```csharp
// ✅ CDN / API URL 必须 HTTPS（已有 ValidateUrlSecurity 辅助方法）
// ❌ 禁止 HTTP（MITM 攻击风险）

// ✅ UnityWebRequest 加超时
var request = UnityWebRequest.Get(url);
request.timeout = 10; // 秒
```

### 数据完整性
- 所有 PlayerPrefs 存档 **必须** 通过 `ISaveSystem`（自带 HMAC 签名）
- 竞技类数据必须服务端校验，客户端 HMAC 仅防休闲篡改
- `DeleteAll()` 需要二次确认 UI，防误操作

### PII（个人隐私信息）保护
- **绝不** 日志以下内容: OpenId、手机号、身份证号、auth code、token、密码、剪贴板内容
- 微信用户昵称/头像仅在 UI 显示，不写入本地日志
- Debug 构建中如需查看敏感数据，使用 `GameLog`（release 自动剥离）

### 条件编译守卫
```csharp
// 仅在 Editor / Development 构建中存在的代码
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 调试面板、作弊码、测试入口
#endif

// 编辑器专用（如 AssetDatabase 调用）
#if UNITY_EDITOR
    AssetDatabase.Refresh();
#endif
```

---

## [AGENT] 模块依赖规范

### 层级依赖图

```
L0: Utils (GameLog, Singleton, MathUtils, CoroutineRunner)
L1: EventSystem, DataSystem, Timer, AssetSystem
L1-R: Rendering (RBM / RuntimeAtlasSystem / RenderVertex) — 零业务依赖
L2: UISystem, AudioSystem, ObjectPool
L3: FSM, WeChatBridge
L4: GameLifecycle (GameBootstrapper, SceneLoader)
L5: DebugTools
L-VFX: VFXSystem (SpriteSheetVFXSystem / VFXBatchRenderer) — 依赖 L1-R
L-Danmaku: DanmakuSystem — 依赖 L0 + L1 + L1-R + L-VFX (via IDanmakuVFXRuntime)
──────────────────────────────
L6: Game（_Game/ 目录，可引用以上所有层）
```

### 规则
- **只能向下依赖**：L2 可引用 L1 和 L0，不可引用 L3+
- **禁止循环依赖**：如果 A 引用 B，B 不可引用 A
- **跨模块通信**：必须通过 SO 事件/变量，不可直接引用对方类
- **Game 层** 可引用框架任意层，但框架层不可引用 Game 层
- 运行 `Tools → MiniGame Template → Validate Architecture` 检查违规

---

## [AGENT] SO 设计模式速查

| 需求 | 使用 | 创建菜单 |
|------|------|---------|
| 跨组件通信（无参） | `GameEvent` | MiniGameTemplate/Events/Game Event |
| 跨组件通信（带参） | `IntGameEvent` / `FloatGameEvent` / `StringGameEvent` | MiniGameTemplate/Events/... |
| 共享数值状态 | `IntVariable` / `FloatVariable` | MiniGameTemplate/Variables/... |
| 共享开关状态 | `BoolVariable` | MiniGameTemplate/Variables/Bool |
| 共享文本状态 | `StringVariable` | MiniGameTemplate/Variables/String |
| 追踪运行时对象集合 | `RuntimeSet<T>` + `RuntimeSetRegistrar` | 自定义 |
| 音频配置 | `AudioClipSO` / `AudioLibrary` | MiniGameTemplate/Audio/... |
| 对象池配置 | `PoolDefinition` | MiniGameTemplate/Pool/Pool Definition |
| FSM 状态/转换 | `State` / `StateTransition` | MiniGameTemplate/FSM/... |
| 场景引用 | `SceneDefinition` | MiniGameTemplate/Scene Definition |
| 全局配置 | `GameConfig` / `AssetConfig` | MiniGameTemplate/... |

### [AGENT] 创建新 SO 类型的检查清单
1. `[CreateAssetMenu]` 指定 menuName（前缀 `MiniGameTemplate/`）和 order
2. 如果有运行时可变状态，在 `OnEnable` 中重置（防止跨 Play Mode 残留）
3. Editor 专用功能用 `#if UNITY_EDITOR` 守卫
4. 更新 `Docs/SO_CATALOG.md` 中的类型目录

---

## [AGENT] 集合迭代安全规范

### 禁止迭代中修改集合
```csharp
// ❌ 错误：foreach 中修改字典
foreach (var panel in _activePanels.Values)
    panel.Close(); // 如果 Close() 内部修改了 _activePanels → InvalidOperationException

// ✅ 正确：先快照再迭代
var snapshot = new List<GComponent>(_activePanels.Values);
_activePanels.Clear();
foreach (var panel in snapshot)
    CleanupPanel(panel);
```

### List 遍历中删除元素
```csharp
// ✅ 正确：倒序遍历
for (int i = list.Count - 1; i >= 0; i--)
{
    if (ShouldRemove(list[i]))
        list.RemoveAt(i); // O(n) per removal — 少量元素可接受
}

// ✅ 正确（大量删除）：批量标记后 RemoveAll
```

---

## [AGENT] DanmakuSystem 编码规范

### 激光/喷雾 API 调用
```csharp
// ✅ 正确：FireLaser 必须提供 length 参数
DanmakuSystem.Instance.FireLaser(typeIndex, origin, angle, length: 10f);

// ✅ 正确：FireSpray 必须提供 lifetime 参数
DanmakuSystem.Instance.FireSpray(typeIndex, origin, dir, cone, range, lifetime: 3f);

// ✅ Attached 模式——激光/喷雾跟随 Transform
DanmakuSystem.Instance.FireLaser(typeIndex, source, length: 10f, lifetime: 5f);

// ❌ 错误：LaserTypeSO 上没有 Length/Duration 字段，不能从 SO 读取
// laser.Length = type.Length; // CS1061
```

### 引用计数
- `AttachSourceRegistry.Register()` 初始引用计数为 1（注册即持有），**不需要额外 AddRef**
- `FreeLaser()` / `FreeSpray()` 内部会自动 `Release(attachId)`，**不需要手动释放**
- `ClearAll()` 调用 `_attachRegistry.FreeAll()` 全部重置

### 碰撞系统
- 碰撞系统为 **7 阶段**（不是 5 阶段），包含激光vs障碍物折射和喷雾vs屏幕边缘回收
- `LaserSegmentSolver` 内置 `MAX_ITERATIONS = 32` 防止密集穿透障碍物导致无限循环
- 激光生命周期判断统一使用 `laser.Lifetime`（不是 `type.TotalDuration`），支持自定义 lifetime

### 零 GC 要求
- DanmakuSystem 内所有热路径（Update 循环）禁止 new/LINQ/Lambda
- `LaserPool.Free()` 保留 `Segments[]` 数组引用，不 new 新数组
- `AttachSourceRegistry` 使用固定数组 + 空闲栈，零 GC

---

## [AGENT] Mesh 顶点布局规范（强制）

### 血泪教训
此规范源于 2026-04-19 两次修复才修好的渲染 Bug：CPU 结构体字段顺序与 GPU 顶点声明顺序不一致，导致子弹完全不可见。

### Unity 标准顶点属性排序（强制遵循）
Unity 在 `Mesh.SetVertexBufferParams()` 时会**静默重排**不符合标准顺序的顶点属性。标准顺序为：

```
Position → Normal → Tangent → Color → TexCoord0 → TexCoord1 → ... → TexCoord7 → BlendWeight → BlendIndices
```

跳过未使用的属性，但**已使用属性之间的相对顺序不可改变**。

### 三条铁律

1. **`VertexAttributeDescriptor[]` 数组必须按标准顺序声明**
   - ✅ `Position, Color, TexCoord0`
   - ❌ `Position, TexCoord0, Color`（Unity 会静默重排为 Position, Color, TexCoord0）

2. **`[StructLayout(LayoutKind.Sequential)]` 结构体字段必须与 `VertexAttributeDescriptor[]` 声明顺序完全一致**
   - 不是"想当然的语义顺序"，是"Unity 标准属性排序"
   - CPU 结构体的 `Marshal.OffsetOf` 必须与 GPU 侧实际偏移一一对应

3. **如果控制台出现 "Mesh vertex buffer attributes were supplied in non-standard order" 警告，说明顶点布局有对齐风险**
   - 此警告意味着 Unity 已经强制重排了 GPU 侧布局
   - 必须立即检查 CPU 结构体是否与重排后的顺序一致
   - **不可忽略此警告**

### 本项目的标准顶点格式

```csharp
// RenderVertex.cs — CPU 侧
[StructLayout(LayoutKind.Sequential)]
public struct RenderVertex
{
    public Vector3 Position;   // 12B, offset=0
    public Color32 Color;      // 4B,  offset=12
    public Vector2 UV;         // 8B,  offset=16
}   // sizeof=24

// RenderBatchManager.cs — GPU 侧
private static readonly VertexAttributeDescriptor[] VertexLayout =
{
    new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
    new(VertexAttribute.Color,    VertexAttributeFormat.UNorm8,  4),
    new(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32, 2),
};
```

### [AGENT] 修改顶点布局时的检查清单
- [ ] `VertexAttributeDescriptor[]` 顺序符合 Unity 标准排序
- [ ] `struct RenderVertex` 字段顺序与 `VertexAttributeDescriptor[]` 一致
- [ ] 进入 Play 模式后控制台**无** "non-standard order" 警告
- [ ] 通过 `Marshal.OffsetOf` 反射验证各字段偏移符合预期

---

## [AGENT] 渲染/视觉问题排查顺序

适用于多类型 VFX、换色、皮肤切换、材质切换、Blend Mode 差异等“日志说切对了，但肉眼看不出来”的问题。

### 强制顺序
1. **先检查可视化验证样本是否足够可区分**
   - 至少满足以下一项强差异：颜色、尺寸、轮廓、混合层、贴图内容
   - 如果当前差异只依赖轻微 Tint，且底图高亮/偏白/偏黄，优先怀疑样本设计失败，不要直接判定逻辑切换失败
2. **再检查选择链路**
   - 输入是否触发
   - 模式是否切换成功
   - 最终选中的类型名是否正确
3. **再检查运行时映射**
   - registry / runtime index / 入池数据是否正确
   - 禁止把上下文相关运行时值（如 `RuntimeIndex`）持久化到 SO 资产
4. **最后才检查渲染表现**
   - 材质、Blend、Layer、贴图底色、Shader 对 Tint 的影响

### 调试日志规则
- 默认只打“用户主动操作”对应的低噪音日志
- 不要一上来就开每帧日志
- 自动轮播与手动触发分开日志入口
- 输入层 → 选择层 → 系统层 → 渲染层，逐层打开，不要一次全开

## [AGENT] 代码提交检查清单

Agent 在完成代码编写后，提交前必须自检以下项目：

- [ ] **日志**: 全部使用 `GameLog`，无裸 `Debug.Log`（致命错误除外）
- [ ] **null 检查**: 所有 `[SerializeField]` 引用在使用前检查
- [ ] **事件配对**: `OnEnable` 注册 → `OnDisable` 注销，1:1 对应
- [ ] **定时器清理**: 持有 `TimerHandle` 的组件在禁用/销毁时 Cancel
- [ ] **async 安全**: 无 `.Result` / `.Wait()`，`async void` 有 try-catch
- [ ] **GC 安全**: Update/OnGUI 中无字符串拼接、new 集合、LINQ、闭包
- [ ] **WebGL 安全**: 无 `System.IO`、无 `Thread`、无同步等待
- [ ] **安全**: 无 PII 日志、文件名已验证、URL 使用 HTTPS
- [ ] **命名**: 遵循命名规范表
- [ ] **行数**: MonoBehaviour ≤ 150 行
- [ ] **FairyGUI 分层**: FairyGUI 导出的 `*.cs` 不手动修改；业务逻辑在 `*.Logic.cs` 中实现 `IUIPanel`；`OnRefresh` 调 `ApplyData` 不调 `OnOpen`

- [ ] **依赖方向**: 不违反层级依赖图
- [ ] **知识入口**: 新核心模块需要补充或更新对应 `MODULE_CARDS/`、`CONTEXT_PACKS/` 或 `INDEX.md`
- [ ] **Review Skill**: 任何代码改动后必须执行 `code-review-checklist` Skill，修完 bug 后再复查一次
- [ ] **Unity CLI 编译验证**: 代码评审与 bug 修复完成后，必须验证编译通过。**优先使用 MCP 工具** `unity_get_compilation_errors`（见 ARCHITECTURE.md 的 MCP 集成章节），MCP 不可用时回退到 Unity 编辑器命令行 batchmode 编译检查
- [ ] **可视化验证样本检查**: 涉及多类型渲染/换色/皮肤切换时，先确认验证样本在肉眼上可明显区分（颜色、尺寸、轮廓、混合层至少一项强差异）；如果日志已证明类型/状态切换正确，应优先检查素材与混合表现，而不是继续在输入链路和状态机上兜圈子

### [AGENT] 强制编译验证流程
1. 完成代码编写
2. 加载并执行 `code-review-checklist` Skill
3. 修复审查发现的问题
4. **编译验证（按优先级选择）**：
   - **首选：MCP 工具**（Unity Editor 打开时）
     ```
     unity_list_instances          { "refresh": true }
     unity_select_instance        { "port": <扫描到的真实端口> }
     unity_get_compilation_errors { "severity": "all", "port": <当前实例端口> }
     ```
     返回 `count: 0` 即通过。若有错误，直接根据文件/行号修复。
   - **备选：HTTP 直连**（MCP Server 不可用时）
     ```powershell
     curl.exe -s http://127.0.0.1:7891/api/compilation/errors
     ```
   - **兜底：Unity CLI batchmode**（Unity Editor 未打开时）
     ```powershell
     & "C:\UnityWin2021\Unity.exe" -batchmode -quit -projectPath "..." -logFile "..."
     ```
5. 如果存在编译错误，继续修复并重复步骤 2-4，直到编译通过
6. 只有在 review + 编译都通过后，才通知用户进编辑器做运行验证

### [AGENT] Unity CLI 编译命令（Windows）
```powershell
& "C:\UnityWin2021\Unity.exe" -batchmode -quit -projectPath "g:\Workspace\MiniGameTemplate\MiniGameTemplate\UnityProj" -logFile "g:\Workspace\MiniGameTemplate\MiniGameTemplate\UnityProj\Library\unity-batch-compile.log"
```

### [AGENT] Unity CLI 编译日志判定
- 通过标志：`Tundra build success`，且无 `error CSxxxx` / `Compilation failed`
- 常见误区：
  - `.tasks/` 作为 `-logFile` 目录会被 Unity 视为非法目录名（Windows 下）
  - 若有其他 Unity 编辑器实例打开，会报 `Multiple Unity instances cannot open the same project`

---

## [AGENT] 技术文档管理规范（SDD 借鉴）

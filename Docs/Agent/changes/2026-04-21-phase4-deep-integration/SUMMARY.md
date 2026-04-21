# RuntimeAtlas Phase 4 深化集成（ADR-031）

## 动机

R0~R5 + R4.1/R4.3/R4.4 完成后，Editor Play Mode 验收通过 8/12 AC。三项深化任务旨在补全 RuntimeAtlas 的完整度、优化内存占用、统一剩余渲染路径：

1. **R4.4A 懒建页**：微信小游戏环境下 48 MB RT 常驻不可接受
2. **Laser 接入 Atlas**：Laser 独立贴图是 DC 合并的遗漏路径
3. **Trail 纹理化**：TrailPool 仅支持纯色拖尾，缺乏视觉表现力

## 变更范围

### 1. R4.4A — Atlas 懒建页（Lazy Page Creation）
- `RuntimeAtlasManager.InitChannel()`：移除无条件 `CreatePage()` 调用
- `RuntimeAtlasManager.TryAllocateInternal()`：首次分配时触发 Page 0 创建
- `RuntimeAtlasManager.HandleRTLost()`：跳过 Pages=0 且 SourceTextures=0 的空 Channel（CR-01）
- 节省最多 32 MB RT 内存（4 个未使用 Channel）

### 2. Laser 接入 RuntimeAtlas（方案 C）
- `LaserTypeSO`：新增 `UseRuntimeAtlas` 字段
- `RuntimeAtlasBindingResolver`：新增 `ResolveLaser()` 方法
- `LaserRenderer` / `LaserWarningRenderer`：Atlas 材质克隆 + UV 语义分离
- `DanmakuLaser.shader`：`_ATLASMODE_ON` keyword 分支（`multi_compile_local`）
- UV.y 归一化到 [0,1] 整条映射，Atlas RT wrapMode=Clamp

### 3. Trail 纹理化
- `BulletTypeSO`：新增 `TrailTexture` 字段
- `RuntimeAtlasBindingResolver`：新增 `ResolveTrail()` 方法
- `TrailPool.Render()`：RT Lost 恢复路径（检测 IsCreated() → 回退 whiteTexture → 下帧恢复）
- 所有 Trail（含 whiteTexture fallback）统一走 Atlas Channel.Trail

### 4. 共享 RuntimeAtlasManager 注入
- `DanmakuSystem` 持有唯一 `RuntimeAtlasManager` 实例
- 通过 Initialize 参数注入各 Renderer，避免多实例创建 18 个冗余 Channel

## 关键决策

| 编号 | 决策 | 理由 |
|------|------|------|
| PI-001 | 单实例共享注入 vs 各自创建 | Channel 隔离已保证互不干扰，无需多实例 |
| PI-002 | Laser Atlas UV.y 归一化 [0,1] | Atlas RT wrapMode=Clamp 无法 wrap |
| PI-003 | GetStats Pages.Count 直接使用 | 语义精确，totalPixels=0 时分母保护 |
| PI-004 | Trail RT Lost 回退 whiteTexture | 保证渲染不中断 |
| PI-005 | ResolveLaser() 去掉冗余参数 | 直接从 LaserTypeSO 读取 |

## 行为变化

- **AC-13（新增）**：未使用 Channel 不创建 RT → PageCount=0, FillRate=0
- **AC-07（修改）**：Laser 从"不入 Atlas"改为"可选入 Atlas"（`UseRuntimeAtlas` 控制）
- **AC-14（新增）**：Laser Atlas 模式 DC 不增加
- **AC-15（新增）**：Trail 支持自定义纹理，统一走 Atlas

## 已知遗留

- Editor Play Mode 15 项 AC 验收尚未完成（P0）
- 真机验证（55fps / DC≤50 / Batch≤24 / overflow=0）尚未执行（P0）
- LaserRenderer / LaserWarningRenderer 重复代码待提取 `LaserRenderHelper`（P1）
- Atlas UV 除零 Assert 待补（P1）

## PK 评审

1 轮 5 个问题（PI-001~005），全部收敛。详见 `UnityProj/docs/Agent/PHASED_IMPL_PK_Question.md`。

## 代码评审

TDD 100% 符合，零偏离。6 项 CR 发现，修复 3 项（CR-01/03/06），接受 3 项（CR-02/04/05）。

## 关联

- ADR: ADR-031（深化集成）
- 前置: ADR-028（RuntimeAtlas 核心）、ADR-029（Additive 移除）、ADR-030（TypeRegistry 内化）
- Commits: 0e1b4ec, dc5241d

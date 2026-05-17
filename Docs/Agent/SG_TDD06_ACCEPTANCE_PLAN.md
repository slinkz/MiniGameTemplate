---
system: wechat
scope: cloud-save-acceptance
last_verified: 2026-05-07
depends_on: [SG_TDD_06_CLOUD_SAVE]
related_code: Assets/_Framework/WeChatBridge/Scripts/WxAuth*.cs, Assets/_Framework/WeChatBridge/Scripts/CloudSync*.cs, Assets/_Framework/DataSystem/Scripts/Persistence/Cloud*.cs
---

# SG_TDD_06 云存储验收计划

> **版本**：v1.0 | **日期**：2026-05-07  
> **前置**：编译零错误 ✅ | 代码评审通过 ✅

---

## 1. 验收前准备

### 1.1 微信云开发环境配置

1. 登录 [微信公众平台](https://mp.weixin.qq.com/) → 开发管理 → 云开发
2. 创建云开发环境（如已有则跳过）
3. 初始化数据库集合：
   - 创建集合 `progress`（无需预设字段，首次写入自动创建 schema）
4. 部署 3 个云函数：
   - 源码在 `MiniGameTemplate/CloudFunctions/` 目录
   - 使用微信开发者工具右键 → 创建并部署：云端安装依赖
   - 云函数列表：`login` / `getProgress` / `saveProgress`

### 1.2 game.js 初始化（关键！）

在微信小游戏的 `game.js` 入口文件（Unity 导出的 minigame/ 目录中），确保在最早位置初始化云开发：

```javascript
// game.js 最顶部
if (typeof wx !== 'undefined' && wx.cloud) {
  wx.cloud.init({ env: 'your-env-id' }); // 替换为实际环境 ID
}
```

### 1.3 域名白名单（如使用自建后端）

本方案使用微信云开发，**不需要**配置域名白名单。

---

## 2. Editor 验收（V1 行为不变）

| # | 验收项 | 操作步骤 | 期望结果 |
|---|--------|---------|---------|
| 1 | Editor 启动正常 | 打开 Boot 场景 → Play | Console 看到 `[Bootstrapper] PlayerPrefsSaveSystem (V1) initialized.` |
| 2 | Stub 日志输出 | 通关任意关卡 | Console **不应**出现 `[WxAuth]` 或 `[CloudSync]` 的成功日志（Stub 模式） |
| 3 | 进度存档正常 | 通关第 1 关 → 退出 Play → 重新 Play → 进选关界面 | 第 2 关已解锁 |
| 4 | Reload 无异常 | SG_Boot.InitProgress() 重复调用 | 无报错，Progress 不重复创建 |

**行动指导**：如果看到 `CloudSaveSystem (V2) initialized.` 在 Editor 中出现，说明条件编译有误，检查 `UNITY_WEBGL && !UNITY_EDITOR` 宏。

---

## 3. 真机验收（微信开发者工具 + 真机预览）

### 3.1 开发者工具模拟

| # | 验收项 | 操作步骤 | 期望结果 |
|---|--------|---------|---------|
| 5 | 静默登录 | WebGL 导出 → 微信开发者工具打开 → 查看 Console | `[WxAuth] Login success, openid=xxxxxx...` |
| 6 | 首次云写入 | 通关第 1 关 | 云开发控制台 → progress 集合 → 新增一条记录，`clearedLevels: [1]` |
| 7 | 断网降级 | 开发者工具 → 断网 → 通关第 2 关 | 本地照常保存，不报错；Console 看到 `[CloudSync] Upload failed, retry` |
| 8 | 联网后同步 | 恢复网络 → 等待 2~4s | Console 看到上传成功；云端记录变为 `clearedLevels: [1, 2]` |

### 3.2 真机预览

| # | 验收项 | 操作步骤 | 期望结果 |
|---|--------|---------|---------|
| 9 | 清除数据恢复 | 手机设置 → 清除小游戏缓存 → 重新打开 | 进度从云端恢复（之前通关的关卡仍解锁） |
| 10 | 双设备同步 | 设备 A 通关第 3 关 → 设备 B 重启小游戏 | 设备 B 看到第 3 关已解锁 |
| 11 | 超时保护 | 模拟慢网（开发者工具 → 弱网模式） | 5s 超时后 callback 正常触发，游戏不卡死 |
| 12 | 性能无劣化 | 对比 V1 启动时间 | 启动到可操作 <2s（与 V1 持平） |

---

## 4. 异常场景

| # | 验收项 | 模拟方法 | 期望结果 |
|---|--------|---------|---------|
| 13 | 云函数不存在 | 删除 `getProgress` 云函数 → 重启 | 游戏正常运行（local-only 模式），Console 看到 `[CloudSave] Login failed — running in local-only mode.` |
| 14 | 登录失败 3 次 | 修改 `login` 云函数返回错误 → 连续触发 3 次 | 第 3 次后停止重试，Console 看到 `[WxAuth] Max login failures reached` |
| 15 | 并发写入 | 快速连续通关 2 关 | 只上传最新快照（not queued），云端数据 = `[1,2,3]`（union merge） |

---

## 5. 回滚方案

如果真机验证出现严重问题：

1. `GameBootstrapper.CreateSaveSystem()` 中注释掉 `if (_weChatBridge.IsWeChatPlatform)` 分支
2. 或在 `game.js` 中不初始化 `wx.cloud`（此时 `!wx.cloud` → jslib 返回 `no wx.cloud` 错误 → Stub 降级）

---

## 6. 验收签字

| 环节 | 状态 | 日期 | 备注 |
|------|------|------|------|
| Editor 验收 | ✅ | 2026-05-17 | 通过 |
| 开发者工具验收 | ✅ | 2026-05-17 | 通过 |
| 真机验收 | ✅ | 2026-05-17 | 通过 |
| 异常场景验收 | ✅ | 2026-05-17 | 通过 |

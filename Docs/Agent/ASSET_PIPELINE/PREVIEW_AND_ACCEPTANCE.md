---
system: role-agent
scope: asset-preview-and-acceptance
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/MCP_INTEGRATION.md, Docs/Agent/SG_V2_DEVICE_ACCEPTANCE.md
---

# Preview And Acceptance

> 定位：资产预览和验收规则。

## 预览位置

| 资产 | 推荐预览 |
|------|----------|
| 敌人/子弹 sprite | 测试关卡或战斗场景 |
| VFX | VFXDemo / DanmakuDemo / 业务命中链路 |
| UI icon | FairyGUI 编辑器 + Unity UI 面板 |
| Audio | 触发事件所在流程 |
| 背景 | 目标关卡或主界面 |

## 验收证据

- 截图：UI、sprite、VFX 静态效果。
- 录屏：动效、VFX、音频触发、战斗可读性。
- Console：无 Missing Reference 或加载错误。
- Profiler：高频资产不造成明显 spike。
- 真机：微信点击、渲染、音频播放。

## Asset Acceptance

```text
- 文件路径正确
- 命名正确
- 导入设置正确
- 接入点完整
- 预览通过
- 真机风险记录
- Manifest 已更新
```


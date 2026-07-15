---
system: role-agent
scope: economy-and-progression
status: proposed
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SG_GAME_DESIGN.md, Docs/Agent/WECHAT_INTEGRATION.md, Docs/Agent/SG_TDD_06_CLOUD_SAVE.md
---

# Economy And Progression

> 定位：成长、奖励、广告和长期留存设计入口。当前多为后续版本方向，未实现内容必须标记为 proposed。

## 当前已实现/基线

| 系统 | 状态 | 说明 |
|------|------|------|
| 线性关卡解锁 | active | 通关第 N 关解锁 N+1 |
| 关卡重玩 | active | V1 无额外奖励 |
| 进度存储 | active | `sg_progress` / 云存储升级见 TDD |
| 星级评价 | reserved | UI 可显示，规则可后续扩展 |

## 后续方向

| 方向 | 体验价值 | 依赖 |
|------|----------|------|
| 复活广告 | 降低失败流失 | 微信广告、失败界面、战斗重置 |
| 翻倍奖励广告 | 提升 IAA | 结算奖励、广告回调 |
| 技能解锁/升级 | 长期成长 | 配置表、出战准备、保存 |
| 皮肤/外观 | 收藏和变现 | 资产管线、商城 UI |
| 每日挑战 | 留存 | 云时间/本地时间、关卡生成 |

## 设计前置问题

1. 奖励是什么，是否已经有消费口？
2. 广告失败或用户取消时流程如何回退？
3. 是否影响云存储结构和版本迁移？
4. 是否需要 UI 新状态或资产？
5. 是否符合微信平台能力和隐私约束？


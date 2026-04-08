# 弹幕游戏技术瓶颈与性能分析（微信小游戏平台）

## 概述

本文档分析在微信小游戏平台上开发弹幕（Danmaku / Bullet Hell）游戏所面临的技术瓶颈与性能问题，
作为弹幕系统架构设计的决策依据。

---

## 一、渲染层——最核心的瓶颈

| 问题 | 原因 | 量级参考 |
|------|------|---------|
| **Draw Call 爆炸** | 弹幕数量大（几百~几千），每个弹幕若为独立 Sprite/Mesh 会产生大量 Draw Call | 屏幕 500 颗弹幕 → 未优化时可能 500+ DC |
| **Overdraw 严重** | 弹幕密集覆盖、半透明叠加、发光特效，同一像素被反复着色 | 弹幕密集区 overdraw 可达 8x-12x |
| **Fill Rate 不足** | 微信小游戏运行在手机 WebGL 上，GPU Fill Rate 远低于原生，半透明混合尤其昂贵 | 中低端机 720p 下半透明 fill 已是瓶颈 |
| **WebGL 1.0 限制** | 部分老设备仅支持 WebGL 1.0，无 instancing、无 MRT、无 compute shader | GPU instancing 回退为逐对象提交 |

**关键数字**：微信小游戏 WebGL 环境下，**Draw Call 建议控制在 50-80 以内**，弹幕系统本身应控制在 1-3 个 Draw Call。

---

## 二、CPU 层——逻辑更新的压力

| 问题 | 原因 |
|------|------|
| **每帧遍历所有弹幕** | N 颗弹幕 × M 个碰撞体 = O(N×M) 复杂度，WebGL(IL2CPP/Wasm) 比原生慢 2-5x |
| **Physics2D 极慢** | Unity Physics 在 WebGL 上是单线程、无 Job System 加速，大量 Collider2D 会直接卡死 |
| **GC 压力** | 弹幕频繁创建/销毁的托管堆分配，WebGL 无增量 GC，暂停更明显 |
| **无多线程** | WebGL 不支持 C# Thread / Job System（SharedArrayBuffer 默认关闭），所有逻辑跑单线程 |
| **MonoBehaviour 桥接开销** | 每颗弹幕若挂 MonoBehaviour 跑 Update()，几百个 C++ → C# 桥接调用开销可观 |

---

## 三、内存层

| 问题 | 原因 |
|------|------|
| **微信小游戏内存上限** | iOS 约 1GB，Android 约 1.2GB，实际可用远低于此 |
| **Texture 内存** | 弹幕种类多时每种弹幕的贴图、动画帧占 VRAM；WebGL 纹理无法按需卸载 |
| **对象池不当泄漏** | 池子预分配过大 → 内存浪费；不回收 → 泄漏 |
| **文件系统缓存限额** | 微信小游戏文件系统限额 200MB |

---

## 四、微信小游戏平台特有限制

| 限制 | 影响 |
|------|------|
| **首包大小 ≤ 20MB**（推荐 ≤ 12MB） | 特效、音效资源必须走分包/CDN 下载 |
| **无 SharedArrayBuffer** | 无法使用 Unity Job System / Burst |
| **帧率默认 30fps** | 可申请 60fps，但弹幕游戏对流畅度要求高 |
| **音频并发限制** | `InnerAudioContext` 同时约 10 个，射击音效需池化复用 |
| **触摸事件延迟** | 微信 WebView → Unity 输入有 1-2 帧延迟，闪避手感偏黏 |
| **Shader 编译卡顿** | 首次加载 Shader 触发编译卡顿，WebGL Shader Warm-up 效果有限 |

---

## 五、风险矩阵

| 等级 | 问题 | 不处理的后果 |
|------|------|-------------|
| 🔴 P0 | Draw Call / Fill Rate | 中低端机 < 15fps，不可玩 |
| 🔴 P0 | 每弹幕 MonoBehaviour | 500 颗弹幕时 Update 桥接 > 8ms |
| 🔴 P0 | Physics2D 碰撞 | WebGL 单线程物理直接卡死 |
| 🟡 P1 | GC 暂停 | 每次 GC 暂停 50-200ms，弹幕游戏体感致命 |
| 🟡 P1 | 首包过大 | 审核不过 / 用户流失 |
| 🟡 P1 | 触摸延迟 | 闪避手感偏黏，硬核玩家抱怨 |
| 🟢 P2 | Shader 编译卡顿 | 首次进入战斗掉帧 |
| 🟢 P2 | 音效并发 | 密集射击时音效丢失 |

---

## 六、核心结论

微信小游戏上做弹幕，最大的敌人是 **WebGL 单线程 + 低 Fill Rate + 无 Job/Burst**。

解法：**抛弃 GameObject 体系，用纯数据数组 + 单 Mesh 合批 + 自写碰撞**，把弹幕系统做成一个自包含的高性能模块。

详细架构设计见 → [DANMAKU_ARCHITECTURE.md](./DANMAKU_ARCHITECTURE.md)

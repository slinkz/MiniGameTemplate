# 框架模块使用手册 — Part 2：基础设施模块

> AudioSystem · AssetSystem · Timer · ObjectPool

---

## 5. AudioSystem — 音频管理

**用途**：播放 BGM 和 SFX，音量通过 SO 变量驱动。

**位置**：`Assets/_Framework/AudioSystem/`

### 配置音效

1. 右键 → Create → MiniGameTemplate → Audio → Audio Clip，创建 `AudioClipSO`
2. 在 Inspector 中拖入音频文件，调整音量和音调

或者创建一个 `AudioLibrary`（按 key 索引的音效集合），把多个 `AudioClipSO` 集中管理。

### 播放音效

```csharp
// 方式一：直接引用 AudioClipSO
[SerializeField] private AudioClipSO _clickSound;

void OnClick()
{
    AudioManager.Instance.PlaySFX(_clickSound);
}

// 方式二：通过 AudioLibrary 按 key 播放
AudioManager.Instance.PlaySFX("click");
```

### BGM 控制

```csharp
[SerializeField] private AudioClipSO _bgm;

void Start()
{
    AudioManager.Instance.PlayBGM(_bgm);
}

void StopMusic()
{
    AudioManager.Instance.StopBGM();
}
```

### 音量控制

音量使用 `FloatVariable` SO 驱动：
- `MasterVolume` — 主音量
- `BGMVolume` — 背景音乐音量
- `SFXVolume` — 音效音量

只需要在 UI 滑块上绑定对应的 FloatVariable 即可实现音量调节。

### SFX 通道池

SFX 使用 AudioSource 池化方案（默认 4 个通道，round-robin 分配），支持多个音效同时播放。在 AudioManager Inspector 中可以调整 `SFX Pool Size`。

---

## 6. AssetSystem — 资源管理 (YooAsset)

**用途**：封装 YooAsset，提供统一的资源加载 API。

**位置**：`Assets/_Framework/AssetSystem/`

### 运行模式

在 `AssetConfig` SO 的 Inspector 中选择：

| 模式 | 用途 | 何时使用 |
|------|------|----------|
| **EditorSimulate** | 直接从 AssetDatabase 加载 | 编辑器中开发时 |
| **Offline** | 从 StreamingAssets 中的 Bundle 加载 | 离线发布/首包 |
| **Host** | 从远程 CDN 加载 + 本地缓存 | 线上热更新 |
| **WebGL** | 微信小游戏专用文件系统 | 微信小游戏发布 |

### 加载资源

```csharp
// 加载预制件
var handle = AssetService.Instance.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemy.prefab");
await handle.Task;
var prefab = handle.AssetObject as GameObject;
var instance = Object.Instantiate(prefab);

// ⚠️ 用完后必须释放 handle，否则资源无法卸载
handle.Release();
```

### 加载场景

```csharp
var sceneHandle = AssetService.Instance.LoadSceneAsync("Assets/Scenes/GameScene.unity");
await sceneHandle.Task;
```

### 热更新（Host 模式）

```csharp
// 1. 请求最新版本号
var version = await AssetService.Instance.RequestPackageVersionAsync();

// 2. 更新清单
await AssetService.Instance.UpdatePackageManifestAsync(version);

// 3. 下载需要更新的资源
var downloader = AssetService.Instance.CreateResourceDownloader();
if (downloader != null)
{
    downloader.BeginDownload();
    await downloader.Task;
}
```

### 内存清理

```csharp
// 场景切换后调用，卸载未使用的资源
AssetService.Instance.UnloadUnusedAssets();

// 完全重置（慎用）
AssetService.Instance.ForceUnloadAllAssets();
```

---

## 7. Timer — 计时器

**用途**：不依赖 MonoBehaviour 的计时器服务。

**位置**：`Assets/_Framework/Timer/`

### 延迟调用（一次性）

```csharp
// 3 秒后执行
var handle = TimerService.Instance.Delay(3f, () => {
    Debug.Log("3 seconds passed!");
});
```

### 重复调用

```csharp
// 每 0.5 秒执行一次
var handle = TimerService.Instance.Repeat(0.5f, () => {
    Debug.Log("Tick!");
});
```

### 控制计时器

```csharp
// 取消
TimerService.Instance.Cancel(handle);

// 暂停/恢复
TimerService.Instance.Pause(handle);
TimerService.Instance.Resume(handle);

// 检查是否还在运行
bool active = TimerService.Instance.IsActive(handle);

// 获取剩余时间
float remaining = TimerService.Instance.GetRemaining(handle);
```

### 不受 TimeScale 影响

```csharp
// realTime: true → 不受 Time.timeScale 影响（暂停菜单中也会计时）
var handle = TimerService.Instance.Delay(3f, callback, realTime: true);
```

> ⚠️ 持有 `TimerHandle` 的组件必须在 `OnDisable` 中 Cancel 计时器，防止组件销毁后回调触发空引用。

---

## 8. ObjectPool — 对象池

**用途**：避免频繁创建/销毁 GameObject 导致的 GC 开销。

**位置**：`Assets/_Framework/ObjectPool/`

### 创建池定义

右键 → Create → MiniGameTemplate → Pool → Pool Definition

在 Inspector 中配置：
- **Prefab**：要池化的预制件
- **Initial Size**：预热数量（提前创建好放在池中）
- **Max Size**：最大数量（0 = 无限制）

### 使用对象池

```csharp
[SerializeField] private PoolDefinition _bulletPoolDef;

void Shoot()
{
    // 从池中获取
    var bullet = PoolManager.Instance.Get(_bulletPoolDef);
    bullet.transform.position = spawnPoint.position;
}

void OnBulletHit(GameObject bullet)
{
    // 归还到池中（不是 Destroy！）
    PoolManager.Instance.Return(_bulletPoolDef, bullet);
}
```

### 自动延时回收

给池化对象挂上 `PooledObject` 组件，可以配置自动延时回收（比如粒子特效播放完毕后自动归还）。

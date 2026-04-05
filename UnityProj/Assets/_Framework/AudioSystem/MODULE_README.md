# AudioSystem 模块

## 用途
SO驱动的音频播放系统。BGM和SFX通过ScriptableObject配置，支持音量控制和音效库管理。

## 核心类
| 类 | 用途 |
|---|------|
| `AudioManager` | 音频播放控制（Singleton，框架内部） |
| `AudioClipSO` | 单个音效配置SO（clip + volume + pitch） |
| `AudioLibrary` | 音效库SO（集中管理多个AudioClipSO引用） |

## 使用方式
```csharp
// Inspector中配置AudioClipSO引用
[SerializeField] private AudioClipSO _clickSound;

void OnClick() {
    AudioManager.Instance.PlaySFX(_clickSound);
}

// 或通过AudioLibrary按名称播放
AudioManager.Instance.PlaySFX("click");
```

## 音量控制
音量使用FloatVariable SO，可直接绑定到UI滑块：
- `MasterVolume` (FloatVariable)
- `BGMVolume` (FloatVariable)
- `SFXVolume` (FloatVariable)

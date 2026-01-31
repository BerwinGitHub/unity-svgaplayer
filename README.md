# Unity SVGAPlayer
> 痛点：在 Unity 中想使用 SVGA 比较不方便。目前官方只支持纯纹理格式SVGA（SVGAPlayer-Unity），常见的矢量帧SVGA支持不够友好.（官方项目：https://github.com/svga/SVGAPlayer-Unity）  

`SVGAComponent` 是一个用于在 Unity 中播放 SVGA 动画的高性能组件。它支持 SVGA 格式的矢量动画播放，包括音频支持、帧回调、播放控制等功能。

### 主要特性
- **播放控制**：支持播放、暂停、停止、循环播放。
- **帧控制**：支持跳转到指定帧 (`Seek`)，获取当前帧索引。
- **速率控制**：支持动态调整播放速率 (`PlaybackRate`)。
- **音频支持**：自动处理和播放 SVGA 文件中嵌入的音频。
- **高性能**：基于 Unity `RectTransform` 和原生 UI 系统渲染，利用 `LibTessDotNet` 处理矢量图形。
- **内存管理**：高效管理内存，避免内存泄漏。
- **占位替换**：支持在运行时替换 SVGA 文件中的占位符，实现动态内容展示。

### 事件回调
- **OnCompleted**：动画播放完成时触发。
- **AddClickCallback**：添加 Bone 点击回调，点击时触发。

### 示例
#### 1.纯图 SVGA
![纯图 SVGA](./Docus/uTools_GIF_1769841097245.gif)
#### 2.矢量帧 SVGA
![矢量帧 SVGA](./Docus/uTools_GIF_1769842341569.gif)
#### 3.图 + 矢量帧 SVGA
![图 + 矢量帧 SVGA](./Docus/uTools_GIF_1769842146799.gif)



## 快速上手 (Quick Start)

### 1. 引入组件
在你的场景中创建一个 UI 对象（如 Image 或空 GameObject），并挂载 `SVGAPlayer` 组件。

### 2. 实现 Provider
在使用 `SVGAPlayer` 之前，你需要实现 `ISVGAPlayerProvider` 接口，用于提供日志输出和文件写入路径（用于解压音频等）。

```csharp
using Bo.SVGA;
using UnityEngine;

public class MySVGAProvider : ISVGAPlayerProvider
{

    private MonoBehaviour _monoBehaviour;

    public MySVGAProvider(MonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public string GetWritablePath()
    {
        return Application.persistentDataPath;
    }

    public MonoBehaviour GetMonoBehaviour()
    {
        return _monoBehaviour;
    }

    public void Log(string message) => Debug.Log(message);
    public void LogError(string message) => Debug.LogError(message);
    public void LogWarning(string message) => Debug.LogWarning(message);
}
```

### 3. 加载并播放
加载 SVGA 数据（二进制流）并初始化播放器。

```csharp
using Bo.SVGA;
using UnityEngine;
using System.IO;

public class SVGADemo : MonoBehaviour
{
    public SVGAPlayer svgaPlayer;

    void Start()
    {
        // 假设你已经获取了 svga 文件的字节数组
        byte[] svgaBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "demo.svga"));
        
        // 初始化 Provider
        var provider = new MySVGAProvider();
        
        // 加载数据
        svgaPlayer.LoadFromBytes(svgaBytes, provider);
        
        // 设置属性 (可选)
        svgaPlayer.Loop = true;
        
        // 开始播放
        svgaPlayer.Play();
        
        // 监听完成事件
        svgaPlayer.OnCompleted += (player, loopCount) => {
            Debug.Log($"Animation completed {loopCount} times");
        };
    }
}
```

## API 说明

### SVGAPlayer

| 方法/属性 | 说明 |
| :--- | :--- |
| `LoadFromBytes(byte[], ISVGAPlayerProvider)` | 从字节数组加载 SVGA 数据 |
| `LoadFromStream(Stream, ISVGAPlayerProvider)` | 从流加载 SVGA 数据 |
| `Play()` | 开始播放 |
| `Pause()` | 暂停播放 |
| `Stop()` | 停止播放并重置到第一帧 |
| `Seek(int frame)` | 跳转到指定帧 |
| `IsPlaying` | 是否正在播放中 |
| `Loop` | 是否循环播放 |
| `SetPlaybackRate(float)` | 设置播放倍速 (默认 1.0) |
| `GetFps()` | 获取当前帧率 |
| `OnCompleted` | 播放完成回调事件 |

## 版本记录 (Version History)

### [1.0.0] - 2025-12-16
- 初始版本发布
- 支持基础 SVGA 播放
- 支持音频同步播放
- 支持基本的播放控制（Play/Pause/Stop/Seek）

---
更多详情请参考 `CHANGELOG.md` 文件。
参考项目：SVGAPlayer-Unity

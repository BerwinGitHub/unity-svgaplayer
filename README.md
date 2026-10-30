<div align="center">

# Unity SVGAPlayer

**在 Unity 中流畅播放 SVGA 动画的高性能组件**

[English](./README.en.md) · 简体中文

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

</div>

---

> **痛点**：在 Unity 中使用 SVGA 并不方便。官方 [SVGAPlayer-Unity](https://github.com/svga/SVGAPlayer-Unity) 仅支持纯纹理格式的 SVGA，对常见的矢量帧 SVGA 支持不够友好。

`SVGAPlayer` 专为解决这一问题而生：它支持 **矢量帧 SVGA** 的高性能播放，并内置音频支持、帧回调、播放控制、占位符替换等能力，基于 Unity 原生 UI 系统渲染，开箱即用。

## 目录

- [主要特性](#主要特性)
- [效果演示](#效果演示)
- [快速上手](#快速上手)
- [事件回调](#事件回调)
- [API 说明](#api-说明)
- [版本记录](#版本记录)
- [许可证](#许可证)
- [参考项目](#参考项目)

## 主要特性

| 特性 | 说明 |
| :--- | :--- |
| ▶️ 播放控制 | 播放、暂停、停止、循环播放 |
| 🎞 帧控制 | 跳转到指定帧（`Seek`），获取当前帧索引 |
| ⚡ 速率控制 | 动态调整播放倍速（`SetPlaybackRate`） |
| 🔊 音频支持 | 自动解压并播放 SVGA 文件中嵌入的音频 |
| 🚀 高性能 | 基于 `RectTransform` 与原生 UI 系统渲染，使用 `LibTessDotNet` 处理矢量图形 |
| 🧠 内存管理 | 高效管理内存，避免内存泄漏 |
| 🎁 占位替换 | 运行时替换 SVGA 文件中的占位符，实现动态内容展示 |

## 效果演示

<table>
  <tr>
    <td align="center" width="33%">
      <b>纯图 SVGA</b><br/>
      <img src="./Docus/uTools_GIF_1769841097245.gif" alt="纯图 SVGA" />
    </td>
    <td align="center" width="33%">
      <b>矢量帧 SVGA</b><br/>
      <img src="./Docus/uTools_GIF_1769842341569.gif" alt="矢量帧 SVGA" />
    </td>
    <td align="center" width="33%">
      <b>图 + 矢量帧 SVGA</b><br/>
      <img src="./Docus/uTools_GIF_1769842146799.gif" alt="图 + 矢量帧 SVGA" />
    </td>
  </tr>
</table>

## 快速上手

### 1. 引入组件

在场景中创建一个 UI 对象（如 `Image` 或空 GameObject），并挂载 `SVGAPlayer` 组件。

### 2. 实现 Provider

使用 `SVGAPlayer` 前，需要实现 `ISVGAPlayerProvider` 接口，用于提供日志输出和可写文件路径（用于解压音频等）。

```csharp
using Bo.SVGA;
using UnityEngine;

public class MySVGAProvider : ISVGAPlayerProvider
{
    private readonly MonoBehaviour _monoBehaviour;

    public MySVGAProvider(MonoBehaviour monoBehaviour)
    {
        _monoBehaviour = monoBehaviour;
    }

    public string GetWritablePath() => Application.persistentDataPath;

    public MonoBehaviour GetMonoBehaviour() => _monoBehaviour;

    public void Log(string message) => Debug.Log(message);
    public void LogError(string message) => Debug.LogError(message);
    public void LogWarning(string message) => Debug.LogWarning(message);
}
```

### 3. 加载并播放

加载 SVGA 数据（二进制流）并初始化播放器：

```csharp
using Bo.SVGA;
using UnityEngine;
using System.IO;

public class SVGADemo : MonoBehaviour
{
    public SVGAPlayer svgaPlayer;

    void Start()
    {
        // 假设你已经获取了 .svga 文件的字节数组
        byte[] svgaBytes = File.ReadAllBytes(
            Path.Combine(Application.streamingAssetsPath, "demo.svga"));

        // 初始化 Provider（传入当前 MonoBehaviour）
        var provider = new MySVGAProvider(this);

        // 加载数据
        svgaPlayer.LoadFromBytes(svgaBytes, provider);

        // 设置属性（可选）
        svgaPlayer.Loop = true;

        // 开始播放
        svgaPlayer.Play();

        // 监听完成事件
        svgaPlayer.OnCompleted += (player, loopCount) =>
        {
            Debug.Log($"Animation completed {loopCount} times");
        };
    }
}
```

## 事件回调

| 事件 | 说明 |
| :--- | :--- |
| `OnCompleted` | 动画播放完成时触发 |
| `AddClickCallback` | 添加 Bone 点击回调，点击时触发 |

## API 说明

### `SVGAPlayer`

**加载**

| 方法 | 说明 |
| :--- | :--- |
| `LoadFromBytes(byte[], ISVGAPlayerProvider)` | 从字节数组加载 SVGA 数据 |
| `LoadFromStream(Stream, ISVGAPlayerProvider)` | 从流加载 SVGA 数据 |

**播放控制**

| 方法 / 属性 | 说明 |
| :--- | :--- |
| `Play()` | 开始播放 |
| `Pause()` | 暂停播放 |
| `Stop()` | 停止播放并重置到第一帧 |
| `Seek(int frame)` | 跳转到指定帧 |
| `IsPlaying` | 是否正在播放中 |
| `Loop` | 是否循环播放 |
| `SetPlaybackRate(float)` | 设置播放倍速（默认 `1.0`） |
| `GetFps()` | 获取当前帧率 |

**事件**

| 事件 | 说明 |
| :--- | :--- |
| `OnCompleted` | 播放完成回调事件 |

## 版本记录

### [1.0.0] - 2025-12-16

- 初始版本发布
- 支持基础 SVGA 播放
- 支持音频同步播放
- 支持基本的播放控制（Play / Pause / Stop / Seek）

更多详情请参考 [`CHANGELOG.md`](./CHANGELOG.md)。

## 许可证

本项目基于 [MIT License](./LICENSE) 开源。

## 参考项目

- [SVGAPlayer-Unity](https://github.com/svga/SVGAPlayer-Unity) — SVGA 官方 Unity 播放器

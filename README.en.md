<div align="center">

# Unity SVGAPlayer

**A high-performance component for playing SVGA animations in Unity**

English · [简体中文](./README.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

</div>

---

> **The problem**: Playing SVGA in Unity is not convenient. The official [SVGAPlayer-Unity](https://github.com/svga/SVGAPlayer-Unity) only supports pure-texture SVGA files, and offers poor support for the common vector-frame SVGA format.

`SVGAPlayer` solves this: it renders **vector-frame SVGA** at high performance, with built-in audio support, frame callbacks, playback control, and runtime dynamic image replacement — all on top of Unity's native UI system, ready to use out of the box.

## Table of Contents

- [Key Features](#key-features)
- [Demos](#demos)
- [Quick Start](#quick-start)
- [Event Callbacks](#event-callbacks)
- [API Reference](#api-reference)
- [Version History](#version-history)
- [License](#license)
- [Credits](#credits)

## Key Features

| Feature | Description |
| :--- | :--- |
| ▶️ Playback Control | Play, pause, stop, and loop |
| 🎞 Frame Control | Seek to a specific frame (`Seek`), get the current frame index |
| ⚡ Speed Control | Dynamically adjust playback rate (`SetPlaybackRate`) |
| 🔊 Audio Support | Automatically extracts and plays audio embedded in SVGA files |
| 🚀 High Performance | Renders via `RectTransform` and the native UI system; vector graphics processed by `LibTessDotNet` |
| 🧠 Memory Management | Efficient memory handling with no leaks |
| 🎁 Dynamic Replacement | Replace placeholders inside an SVGA file at runtime for dynamic content |

## Demos

<table>
  <tr>
    <td align="center" width="33%">
      <b>Texture-only SVGA</b><br/>
      <img src="./Docus/uTools_GIF_1769841097245.gif" alt="Texture-only SVGA" />
    </td>
    <td align="center" width="33%">
      <b>Vector-frame SVGA</b><br/>
      <img src="./Docus/uTools_GIF_1769842341569.gif" alt="Vector-frame SVGA" />
    </td>
    <td align="center" width="33%">
      <b>Texture + Vector-frame SVGA</b><br/>
      <img src="./Docus/uTools_GIF_1769842146799.gif" alt="Texture + Vector-frame SVGA" />
    </td>
  </tr>
</table>

## Quick Start

### 1. Add the Component

Create a UI object in your scene (e.g. an `Image` or an empty GameObject) and attach the `SVGAPlayer` component.

### 2. Implement the Provider

Before using `SVGAPlayer`, implement the `ISVGAPlayerProvider` interface, which provides log output and a writable file path (used for extracting audio, etc.).

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

### 3. Load and Play

Load the SVGA data (a binary blob) and start playback:

```csharp
using Bo.SVGA;
using UnityEngine;
using System.IO;

public class SVGADemo : MonoBehaviour
{
    public SVGAPlayer svgaPlayer;

    void Start()
    {
        // Assume you already have the byte array of the .svga file
        byte[] svgaBytes = File.ReadAllBytes(
            Path.Combine(Application.streamingAssetsPath, "demo.svga"));

        // Initialize the provider (pass in the current MonoBehaviour)
        var provider = new MySVGAProvider(this);

        // Load the data
        svgaPlayer.LoadFromBytes(svgaBytes, provider);

        // Configure properties (optional)
        svgaPlayer.Loop = true;

        // Start playback
        svgaPlayer.Play();

        // Listen for the completion event
        svgaPlayer.OnCompleted += (player, loopCount) =>
        {
            Debug.Log($"Animation completed {loopCount} times");
        };
    }
}
```

## Event Callbacks

| Event | Description |
| :--- | :--- |
| `OnCompleted` | Triggered when the animation finishes playing |
| `AddClickCallback` | Adds a Bone click callback, triggered on click |

## API Reference

### `SVGAPlayer`

**Loading**

| Method | Description |
| :--- | :--- |
| `LoadFromBytes(byte[], ISVGAPlayerProvider)` | Load SVGA data from a byte array |
| `LoadFromStream(Stream, ISVGAPlayerProvider)` | Load SVGA data from a stream |

**Playback Control**

| Method / Property | Description |
| :--- | :--- |
| `Play()` | Start playback |
| `Pause()` | Pause playback |
| `Stop()` | Stop playback and reset to the first frame |
| `Seek(int frame)` | Jump to a specific frame |
| `IsPlaying` | Whether the animation is currently playing |
| `Loop` | Whether the animation loops |
| `SetPlaybackRate(float)` | Set the playback rate (default `1.0`) |
| `GetFps()` | Get the current frame rate |

**Events**

| Event | Description |
| :--- | :--- |
| `OnCompleted` | Callback event fired when playback completes |

## Version History

### [1.0.0] - 2025-12-16

- Initial release
- Basic SVGA playback support
- Synchronized audio playback
- Basic playback controls (Play / Pause / Stop / Seek)

See [`CHANGELOG.md`](./CHANGELOG.md) for more details.

## License

This project is released under the [MIT License](./LICENSE).

## Credits

- [SVGAPlayer-Unity](https://github.com/svga/SVGAPlayer-Unity) — The official SVGA player for Unity

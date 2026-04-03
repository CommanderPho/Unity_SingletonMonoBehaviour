# Unity_SingletonMonoBehaviour

Generic SingletonMonoBehaviour class, plus a **Windows 10 desktop overlay** that renders a transparent, always-on-top, click-through window with monitor-edge glow effects when the cursor moves between screens.

---

## SingletonMonoBehaviour

### Importing

You can use Package Manager or import it directly.

```
https://github.com/XJINE/Unity_SingletonMonoBehaviour.git?path=Assets/Packages/SingletonMonoBehaviour
```

### How to Use

Make inheritance & access simply.

```csharp
public class Sample1 : SingletonMonoBehaviour<Sample1>
{
    public int value = 0;
}

Sample1.Instance.value += 1;
```

---

## Desktop Overlay (`Assets/Packages/DesktopOverlay`)

A Unity-based Windows 10 overlay that:

- Runs as a **borderless, transparent, always-on-top** window spanning the entire virtual desktop.
- Is fully **click-through** (`WS_EX_LAYERED | WS_EX_TRANSPARENT`) — no mouse/keyboard events are intercepted.
- **Tracks the cursor** across multiple monitors and **triggers edge glow animations** when the cursor crosses between screens.

### Package contents

| File | Purpose |
|------|---------|
| `MonitorLayout.cs` | Static helper: enumerates monitors via Win32 `EnumDisplayMonitors`, caches `VirtualDesktopBounds`, provides `PixelToWorld()` |
| `TransparentOverlayWindow.cs` | Singleton: Win32 window setup (layered, transparent, topmost) + orthographic camera configuration |
| `CursorMonitorTracker.cs` | Singleton: polls `GetCursorPos` each frame, fires `OnMonitorChanged(oldIdx, newIdx, dir)` |
| `EdgeGlowController.cs` | Singleton: spawns edge-strip quads per monitor, reacts to `OnMonitorChanged` |
| `MonitorEdgeGlow.cs` | Component on each edge quad: `TriggerGlow()` coroutine fade-out |
| `Shaders/GlowEdge.shader` | Unlit, alpha-blended shader with smooth strip falloff |

### Scene setup

1. Create a new scene with one **Orthographic Camera** tagged `MainCamera`.  
   Set its background colour to `(0, 0, 0, 0)` (fully transparent).
2. Create a persistent `GameObject` (e.g. `OverlayManager`) and attach:
   - `TransparentOverlayWindow`
   - `CursorMonitorTracker`
   - `EdgeGlowController`
3. In **Player Settings**:
   - *Resolution*: windowed, fullscreen mode = `Windowed`.
   - *Other*: enable *Run In Background*.
4. Build for **Windows x86_64** (Mono or IL2CPP).

### How it works

#### Window transparency (Win32)

`TransparentOverlayWindow.Awake()` calls:

```csharp
// Strip window decorations → borderless popup
SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);

// Click-through layered window, hidden from Alt+Tab
SetWindowLong(hWnd, GWL_EXSTYLE,
    WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);

// Per-pixel DWM alpha (margins = -1 = entire client area)
DwmExtendFrameIntoClientArea(hWnd, ref margins);

// Always-on-top without stealing focus
SetWindowPos(hWnd, HWND_TOPMOST, vd.x, vd.y, vd.width, vd.height,
    SWP_NOACTIVATE | SWP_SHOWWINDOW);
```

The orthographic camera is configured with `orthographicSize = virtualDesktopHeight / 2`
so that **1 world unit = 1 pixel**, making it straightforward to position quads from
`MonitorLayout.PixelToWorld()`.

#### Edge glow

`EdgeGlowController` creates four thin quad GameObjects per monitor (left, right, top,
bottom edges).  Each quad has a `MonitorEdgeGlow` component and its own instance of the
`DesktopOverlay/GlowEdge` material.

When `CursorMonitorTracker` fires `OnMonitorChanged`, the controller determines which
edges were crossed from the direction vector and calls `TriggerGlow()` on the relevant
quads.  The coroutine animates the material's `_Alpha` property with a quadratic ease-out
over `glowDuration` seconds (default 0.5 s).

#### Coordinate system

```
Windows virtual desktop  →  Unity world space
  pixel (px, py)            worldX =  px - vd.centre.x
                            worldY = -(py - vd.centre.y)   // Y flipped
```

`MonitorLayout.PixelToWorld(px, py)` handles this conversion.

### Customising the glow

All glow parameters are `[SerializeField]` on `EdgeGlowController` and editable in
the Inspector:

| Property | Default | Description |
|----------|---------|-------------|
| `glowThickness` | `20` | Width of each edge strip in pixels |
| `glowColor` | `#41A6FF` | Peak colour of the glow |
| `glowIntensity` | `2` | Colour multiplier (use > 1 for HDR-style punch) |
| `glowDuration` | `0.5` | Fade duration in seconds |

### Limitations

- Windows 10 / Windows 11 only (uses Win32 + DWM).  All native calls are guarded by
  `#if UNITY_STANDALONE_WIN` / `#if UNITY_EDITOR_WIN` so the project compiles on other
  platforms.
- Monitor layout is cached at startup.  Call `MonitorLayout.Refresh()` and
  `CursorMonitorTracker.Instance.RefreshMonitorList()` if the display configuration
  changes at runtime.
- Uses Unity's **built-in render pipeline** shader.  For URP, replace
  `GlowEdge.shader` with an equivalent URP Unlit shader or Shader Graph.

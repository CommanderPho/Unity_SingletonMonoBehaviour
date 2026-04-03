using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Static helper that enumerates physical monitors via Win32 and caches their
/// virtual-desktop rectangles.  Call <see cref="Refresh"/> to re-query after a
/// display configuration change.
/// </summary>
public static class MonitorLayout
{
    #region Win32 types

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int  cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private delegate bool MonitorEnumProc(
        IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    #endregion

    // -------------------------------------------------------------------------

    /// <summary>Describes one physical monitor.</summary>
    public struct MonitorInfo
    {
        /// <summary>Zero-based enumeration index.</summary>
        public int Index;
        /// <summary>Bounding rectangle in virtual-desktop (pixel) coordinates.</summary>
        public RectInt Bounds;
        /// <summary>True for the Windows primary monitor.</summary>
        public bool IsPrimary;
    }

    // -------------------------------------------------------------------------

    private static List<MonitorInfo> _monitors;
    private static RectInt           _virtualDesktop;
    private static bool              _initialized;

    /// <summary>All monitors detected on the last <see cref="Refresh"/> call.</summary>
    public static IReadOnlyList<MonitorInfo> Monitors
    {
        get { if (!_initialized) Refresh(); return _monitors; }
    }

    /// <summary>
    /// Smallest rectangle (virtual-desktop pixel coords) that encloses all
    /// monitor bounds.
    /// </summary>
    public static RectInt VirtualDesktopBounds
    {
        get { if (!_initialized) Refresh(); return _virtualDesktop; }
    }

    /// <summary>
    /// Re-enumerate physical monitors.  Call this once at startup or whenever
    /// the display configuration may have changed.
    /// </summary>
    public static void Refresh()
    {
        _monitors = new List<MonitorInfo>();
        int index = 0;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Keep the delegate alive for the duration of the enumeration.
        var proc = new MonitorEnumProc((hMon, hdcMon, ref lprcMon, data) =>
        {
            var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (GetMonitorInfo(hMon, ref info))
            {
                var r = info.rcMonitor;
                _monitors.Add(new MonitorInfo
                {
                    Index     = index++,
                    Bounds    = new RectInt(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top),
                    IsPrimary = (info.dwFlags & 0x1u) != 0
                });
            }
            return true;
        });
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
#endif

        if (_monitors.Count == 0)
        {
            // Fallback: single screen using Unity's current resolution.
            _monitors.Add(new MonitorInfo
            {
                Index     = 0,
                Bounds    = new RectInt(0, 0,
                                Screen.currentResolution.width,
                                Screen.currentResolution.height),
                IsPrimary = true
            });
        }

        // Compute the bounding virtual desktop rectangle.
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var m in _monitors)
        {
            if (m.Bounds.xMin < minX) minX = m.Bounds.xMin;
            if (m.Bounds.yMin < minY) minY = m.Bounds.yMin;
            if (m.Bounds.xMax > maxX) maxX = m.Bounds.xMax;
            if (m.Bounds.yMax > maxY) maxY = m.Bounds.yMax;
        }
        _virtualDesktop = new RectInt(minX, minY, maxX - minX, maxY - minY);
        _initialized    = true;
    }

    /// <summary>
    /// Convert a virtual-desktop pixel position to Unity world-space.
    /// The virtual desktop centre maps to world (0, 0).  One pixel = one world
    /// unit.  The Y axis is flipped (Windows Y increases downward; Unity Y
    /// increases upward).
    /// </summary>
    public static Vector3 PixelToWorld(float pixelX, float pixelY, float z = 0f)
    {
        if (!_initialized) Refresh();
        float cx = _virtualDesktop.x + _virtualDesktop.width  * 0.5f;
        float cy = _virtualDesktop.y + _virtualDesktop.height * 0.5f;
        return new Vector3(pixelX - cx, -(pixelY - cy), z);
    }
}

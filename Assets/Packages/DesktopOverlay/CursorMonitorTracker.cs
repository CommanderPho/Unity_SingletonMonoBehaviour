using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Singleton that polls the Win32 cursor position every frame and fires
/// <see cref="OnMonitorChanged"/> whenever the cursor moves to a different
/// physical monitor.
///
/// Because the overlay window is click-through (<c>WS_EX_TRANSPARENT</c>),
/// it never captures mouse focus; polling <c>GetCursorPos</c> works regardless
/// of which window is active.
/// </summary>
public class CursorMonitorTracker : SingletonMonoBehaviour<CursorMonitorTracker>
{
    #region Win32 interop

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }
#endif

    #endregion

    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired when the cursor crosses from one monitor to another.
    /// Parameters: <c>oldMonitorIndex</c>, <c>newMonitorIndex</c>,
    /// <c>direction</c> (sign vector: x = ±1 for left/right, y = ±1 for
    /// up/down in Windows screen space where Y increases downward).
    /// </summary>
    public event Action<int, int, Vector2Int> OnMonitorChanged;

    private MonitorLayout.MonitorInfo[] _monitors;
    private int _currentMonitorIndex = -1;

    // -------------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        RefreshMonitorList();
    }

    /// <summary>Re-read the monitor list, e.g. after a display config change.</summary>
    public void RefreshMonitorList()
    {
        MonitorLayout.Refresh();
        var list = MonitorLayout.Monitors;
        _monitors = new MonitorLayout.MonitorInfo[list.Count];
        for (int i = 0; i < list.Count; i++)
            _monitors[i] = list[i];
    }

    private void Update()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!GetCursorPos(out var pt)) return;

        int newIndex = -1;
        for (int i = 0; i < _monitors.Length; i++)
        {
            var b = _monitors[i].Bounds;
            if (pt.X >= b.xMin && pt.X < b.xMax &&
                pt.Y >= b.yMin && pt.Y < b.yMax)
            {
                newIndex = i;
                break;
            }
        }

        if (newIndex == -1) return; // cursor outside all known monitors

        if (newIndex != _currentMonitorIndex)
        {
            if (_currentMonitorIndex != -1)
            {
                // Determine direction by comparing monitor centres (Windows
                // coordinates: Y increases downward).
                var oldCentre = _monitors[_currentMonitorIndex].Bounds.center;
                var newCentre = _monitors[newIndex].Bounds.center;
                var dir = new Vector2Int(
                    Math.Sign(newCentre.x - oldCentre.x),
                    Math.Sign(newCentre.y - oldCentre.y));

                OnMonitorChanged?.Invoke(_currentMonitorIndex, newIndex, dir);
            }

            _currentMonitorIndex = newIndex;
        }
#endif
    }
}

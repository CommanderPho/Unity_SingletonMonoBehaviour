using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class CursorMonitorTracker : MonoBehaviour
{
    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X;
        public int Y;
    }

    public event Action<int, int, Vector2Int> OnMonitorChanged;

    MonitorLayout.Monitor[] monitors;
    int currentIndex = -1;
    Vector2Int lastCursorPosition;

    void Start()
    {
        monitors = MonitorLayout.GetMonitors().ToArray();
        if (monitors.Length > 0)
        {
            POINT p;
            if (GetCursorPos(out p))
            {
                currentIndex = GetMonitorIndexAt(p.X, p.Y);
                lastCursorPosition = new Vector2Int(p.X, p.Y);
            }
        }
    }

    void Update()
    {
        POINT p;
        if (!GetCursorPos(out p)) return;

        int newIndex = GetMonitorIndexAt(p.X, p.Y);

        if (newIndex != currentIndex && newIndex != -1 && currentIndex != -1)
        {
            var old = monitors[currentIndex].Bounds.center;
            var cur = monitors[newIndex].Bounds.center;
            var dir = new Vector2Int(
                Math.Sign(cur.x - old.x),
                Math.Sign(cur.y - old.y));

            OnMonitorChanged?.Invoke(currentIndex, newIndex, dir);
        }

        currentIndex = newIndex;
        lastCursorPosition = new Vector2Int(p.X, p.Y);
    }

    int GetMonitorIndexAt(int x, int y)
    {
        for (int i = 0; i < monitors.Length; i++)
        {
            var b = monitors[i].Bounds;
            if (x >= b.xMin && x < b.xMax && y >= b.yMin && y < b.yMax)
            {
                return i;
            }
        }
        return -1;
    }

    public MonitorLayout.Monitor[] GetMonitors() => monitors;
}

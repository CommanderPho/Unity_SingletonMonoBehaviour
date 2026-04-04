using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;

public static class MonitorLayout
{
    public struct Monitor
    {
        public int Index;
        public RectInt Bounds;
    }

    public static List<Monitor> GetMonitors()
    {
        var list = new List<Monitor>();
        Screen[] screens = Screen.AllScreens;

        for (int i = 0; i < screens.Length; i++)
        {
            System.Drawing.Screen s = screens[i];
            list.Add(new Monitor
            {
                Index = i,
                Bounds = new RectInt(
                    s.Bounds.X,
                    s.Bounds.Y,
                    s.Bounds.Width,
                    s.Bounds.Height)
            });
        }

        return list;
    }

    public static RectInt GetVirtualDesktopBounds()
    {
        List<Monitor> monitors = GetMonitors();
        if (monitors.Count == 0)
            return new RectInt(0, 0, Screen.primaryScreen.Bounds.Width, Screen.primaryScreen.Bounds.Height);

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var m in monitors)
        {
            minX = Mathf.Min(minX, m.Bounds.xMin);
            minY = Mathf.Min(minY, m.Bounds.yMin);
            maxX = Mathf.Max(maxX, m.Bounds.xMax);
            maxY = Mathf.Max(maxY, m.Bounds.yMax);
        }

        return new RectInt(minX, minY, maxX - minX, maxY - minY);
    }
}

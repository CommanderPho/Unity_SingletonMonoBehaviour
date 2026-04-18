using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Singleton that configures the Unity window as a transparent, borderless,
/// always-on-top, click-through overlay covering the entire virtual desktop.
///
/// Attach this component to a persistent GameObject in your first scene.
/// It is safe to use in the Editor; all Win32 calls are guarded by
/// <c>#if UNITY_STANDALONE_WIN</c> so the Editor camera configuration still
/// runs on non-Windows hosts.
/// </summary>
public class TransparentOverlayWindow : SingletonMonoBehaviour<TransparentOverlayWindow>
{
    #region Win32 interop

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr FindWindow(string cls, string title);

    [DllImport("user32.dll")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
    }

    // Window style indices
    private const int GWL_STYLE   = -16;
    private const int GWL_EXSTYLE = -20;

    // Window styles
    private const uint WS_POPUP    = 0x80000000u;
    private const uint WS_VISIBLE  = 0x10000000u;
    // Strip: caption, sysmenu, thickframe, minimise/maximise boxes
    private const uint WS_DECORATIONS_MASK = 0x00CF0000u;

    // Extended window styles
    private const uint WS_EX_LAYERED     = 0x00080000u;
    private const uint WS_EX_TRANSPARENT = 0x00000020u;
    private const uint WS_EX_TOOLWINDOW  = 0x00000080u; // hide from Alt+Tab

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    // SetWindowPos flags
    private const uint SWP_NOACTIVATE  = 0x0010u;
    private const uint SWP_SHOWWINDOW  = 0x0040u;
#endif

    #endregion

    // -------------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();

        // Keep the overlay running even when it does not have focus.
        Application.runInBackground = true;
        Application.targetFrameRate = 60;

        // Enumerate monitors before anything else so VirtualDesktopBounds is ready.
        MonitorLayout.Refresh();

#if UNITY_STANDALONE_WIN
        SetupNativeWindow();
#endif
        SetupCamera();
    }

#if UNITY_STANDALONE_WIN
    private void SetupNativeWindow()
    {
        // Try GetActiveWindow first (reliable when called early in Start/Awake),
        // fall back to GetForegroundWindow, and finally search by product name.
        IntPtr hWnd = GetActiveWindow();
        if (hWnd == IntPtr.Zero)
            hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            hWnd = FindWindow(null, Application.productName);

        if (hWnd == IntPtr.Zero)
        {
            Debug.LogError("[TransparentOverlayWindow] Could not obtain window handle.");
            return;
        }

        // --- Window style: borderless popup ----------------------------------
        uint style = GetWindowLong(hWnd, GWL_STYLE);
        style &= ~WS_DECORATIONS_MASK;
        style |= WS_POPUP | WS_VISIBLE;
        SetWindowLong(hWnd, GWL_STYLE, style);

        // --- Extended style: layered + transparent (click-through) -----------
        uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
        SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);

        // --- DWM per-pixel alpha compositing ---------------------------------
        // Margins of -1 tell DWM to treat the entire client area as the glass
        // sheet, enabling per-pixel alpha so transparent pixels are truly
        // see-through when rendered with a (0,0,0,0) camera clear colour.
        var margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        // --- Position / size: cover entire virtual desktop -------------------
        var vd = MonitorLayout.VirtualDesktopBounds;
        SetWindowPos(hWnd, HWND_TOPMOST,
            vd.x, vd.y, vd.width, vd.height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);

        // Tell Unity about the new resolution so its internal viewport matches.
        Screen.SetResolution(vd.width, vd.height, false);
    }
#endif

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[TransparentOverlayWindow] No main camera found. " +
                             "Attach a Camera tagged 'MainCamera' to the scene.");
            return;
        }

        var vd = MonitorLayout.VirtualDesktopBounds;

        cam.orthographic     = true;
        cam.orthographicSize = vd.height * 0.5f; // 1 world unit = 1 pixel
        cam.aspect           = (float)vd.width / vd.height;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0f, 0f, 0f, 0f); // fully transparent
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }
}

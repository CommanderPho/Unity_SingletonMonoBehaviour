using System.Collections.Generic;
using UnityEngine;

/// <summary>Which border of a monitor an edge quad represents.</summary>
public enum MonitorEdge { Left, Right, Top, Bottom }

/// <summary>
/// Singleton that, at startup, spawns thin quad GameObjects along every
/// monitor edge and wires them to <see cref="CursorMonitorTracker"/> so the
/// correct edges glow whenever the cursor crosses between screens.
///
/// Coordinate mapping:
///   - Virtual-desktop pixel space  → Unity world space via
///     <see cref="MonitorLayout.PixelToWorld"/>.
///   - 1 pixel = 1 world unit.
///   - Windows Y increases downward; Unity Y increases upward (Y is flipped).
///   - Windows yMin  = top of monitor,  yMax = bottom of monitor.
/// </summary>
public class EdgeGlowController : SingletonMonoBehaviour<EdgeGlowController>
{
    [Tooltip("Width of each edge strip in pixels / world units.")]
    [SerializeField] private float glowThickness = 20f;

    [Tooltip("Peak colour of the glow.")]
    [SerializeField] private Color glowColor = new Color(0.25f, 0.65f, 1f, 1f);

    [Tooltip("Colour intensity multiplier fed to the shader's _Intensity property.")]
    [SerializeField] private float glowIntensity = 2f;

    [Tooltip("Time in seconds for the glow to fade from full to zero.")]
    [SerializeField] private float glowDuration = 0.5f;

    // -------------------------------------------------------------------------

    private readonly Dictionary<(int monitorIndex, MonitorEdge edge), MonitorEdgeGlow>
        _edgeGlows = new Dictionary<(int, MonitorEdge), MonitorEdgeGlow>();

    private Shader _glowShader;

    // -------------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        _glowShader = Shader.Find("DesktopOverlay/GlowEdge");
        if (_glowShader == null)
            Debug.LogError("[EdgeGlowController] Shader 'DesktopOverlay/GlowEdge' not found. " +
                           "Make sure GlowEdge.shader is in the project.");
    }

    private void Start()
    {
        CreateEdgeQuads();

        if (CursorMonitorTracker.Instance != null)
            CursorMonitorTracker.Instance.OnMonitorChanged += HandleMonitorChanged;
    }

    private void OnDestroy()
    {
        if (CursorMonitorTracker.Instance != null)
            CursorMonitorTracker.Instance.OnMonitorChanged -= HandleMonitorChanged;
    }

    // -------------------------------------------------------------------------

    private void CreateEdgeQuads()
    {
        if (_glowShader == null) return;

        var monitors = MonitorLayout.Monitors;
        var vd       = MonitorLayout.VirtualDesktopBounds;

        float halfVdW = vd.width  * 0.5f;
        float halfVdH = vd.height * 0.5f;

        // Virtual-desktop centre in pixel space (used for world-space transform).
        float vdCx = vd.x + halfVdW;
        float vdCy = vd.y + halfVdH;

        foreach (var monitor in monitors)
        {
            var b = monitor.Bounds;

            // Monitor centre in world space.
            float worldCx = b.x + b.width  * 0.5f - vdCx;
            float worldCy = -(b.y + b.height * 0.5f - vdCy); // flip Y

            // Left edge  – xMin of monitor (rightmost pixel is world x = b.xMin - vdCx)
            CreateEdge(monitor.Index, MonitorEdge.Left,
                position: new Vector3(
                    (b.xMin - vdCx) + glowThickness * 0.5f,
                    worldCy, 0f),
                scale: new Vector3(glowThickness, b.height, 1f));

            // Right edge – xMax of monitor
            CreateEdge(monitor.Index, MonitorEdge.Right,
                position: new Vector3(
                    (b.xMax - vdCx) - glowThickness * 0.5f,
                    worldCy, 0f),
                scale: new Vector3(glowThickness, b.height, 1f));

            // Top edge – yMin in Windows (smallest pixel Y = topmost row).
            // Flipped in world: worldY = -(b.yMin - vdCy) = vdCy - b.yMin (positive = up).
            CreateEdge(monitor.Index, MonitorEdge.Top,
                position: new Vector3(
                    worldCx,
                    (vdCy - b.yMin) - glowThickness * 0.5f, 0f),
                scale: new Vector3(b.width, glowThickness, 1f));

            // Bottom edge – yMax in Windows (largest pixel Y = bottommost row).
            CreateEdge(monitor.Index, MonitorEdge.Bottom,
                position: new Vector3(
                    worldCx,
                    (vdCy - b.yMax) + glowThickness * 0.5f, 0f),
                scale: new Vector3(b.width, glowThickness, 1f));
        }
    }

    private void CreateEdge(int monitorIndex, MonitorEdge edge,
                             Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name             = $"Monitor{monitorIndex}_{edge}Edge";
        go.transform.parent = transform;
        go.transform.localPosition = position;
        go.transform.localScale    = scale;

        // Remove the auto-added collider — the overlay must not intercept input.
        Destroy(go.GetComponent<MeshCollider>());

        // Create a per-instance material so each quad has its own alpha.
        var mat = new Material(_glowShader)
        {
            name = $"GlowEdge_M{monitorIndex}_{edge}"
        };
        mat.SetColor("_Color",     glowColor);
        mat.SetFloat("_Intensity", glowIntensity);
        mat.SetFloat("_Alpha",     0f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var glow = go.AddComponent<MonitorEdgeGlow>();
        glow.GlowDuration = glowDuration;

        _edgeGlows[(monitorIndex, edge)] = glow;
    }

    // -------------------------------------------------------------------------

    private void HandleMonitorChanged(int oldIndex, int newIndex, Vector2Int dir)
    {
        // dir.x: positive = cursor moved right, negative = left.
        // dir.y: positive = cursor moved down (larger Windows Y), negative = up.

        if (dir.x > 0)
        {
            TriggerEdge(oldIndex, MonitorEdge.Right);
            TriggerEdge(newIndex, MonitorEdge.Left);
        }
        else if (dir.x < 0)
        {
            TriggerEdge(oldIndex, MonitorEdge.Left);
            TriggerEdge(newIndex, MonitorEdge.Right);
        }

        if (dir.y > 0) // cursor moved downward
        {
            TriggerEdge(oldIndex, MonitorEdge.Bottom);
            TriggerEdge(newIndex, MonitorEdge.Top);
        }
        else if (dir.y < 0) // cursor moved upward
        {
            TriggerEdge(oldIndex, MonitorEdge.Top);
            TriggerEdge(newIndex, MonitorEdge.Bottom);
        }
    }

    private void TriggerEdge(int monitorIndex, MonitorEdge edge)
    {
        if (_edgeGlows.TryGetValue((monitorIndex, edge), out var glow))
            glow.TriggerGlow();
    }
}

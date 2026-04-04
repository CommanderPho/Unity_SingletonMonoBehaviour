using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EdgeGlowController : MonoBehaviour
{
    [SerializeField] private Material glowMaterial;
    [SerializeField] private float edgeThickness = 10f;
    [SerializeField] private Vector2Int virtualDesktopOffset;

    private List<EdgeQuad> leftEdges = new List<EdgeQuad>();
    private List<EdgeQuad> rightEdges = new List<EdgeQuad>();
    private List<EdgeQuad> topEdges = new List<EdgeQuad>();
    private List<EdgeQuad> bottomEdges = new List<EdgeQuad>();

    private CursorMonitorTracker tracker;

    void Start()
    {
        tracker = GetComponent<CursorMonitorTracker>();
        if (tracker != null)
        {
            tracker.OnMonitorChanged += HandleMonitorChanged;
        }

        BuildEdgeQuads();
    }

    void OnDestroy()
    {
        if (tracker != null)
        {
            tracker.OnMonitorChanged -= HandleMonitorChanged;
        }
    }

    void HandleMonitorChanged(int oldIndex, int newIndex, Vector2Int direction)
    {
        if (direction.x > 0)
        {
            TriggerEdge(leftEdges, newIndex);
            TriggerEdge(rightEdges, oldIndex);
        }
        else if (direction.x < 0)
        {
            TriggerEdge(rightEdges, newIndex);
            TriggerEdge(leftEdges, oldIndex);
        }

        if (direction.y > 0)
        {
            TriggerEdge(topEdges, newIndex);
            TriggerEdge(bottomEdges, oldIndex);
        }
        else if (direction.y < 0)
        {
            TriggerEdge(bottomEdges, newIndex);
            TriggerEdge(topEdges, oldIndex);
        }
    }

    void TriggerEdge(List<EdgeQuad> edges, int monitorIndex)
    {
        if (monitorIndex >= 0 && monitorIndex < edges.Count)
        {
            edges[monitorIndex].TriggerGlow();
        }
    }

    void BuildEdgeQuads()
    {
        MonitorLayout.Monitor[] monitors = tracker?.GetMonitors() ?? MonitorLayout.GetMonitors().ToArray();

        foreach (var monitor in monitors)
        {
            CreateEdgeQuad(EdgeDirection.Left, monitor.Bounds.xMin, monitor.Bounds.yMin, monitor.Bounds.height, leftEdges);
            CreateEdgeQuad(EdgeDirection.Right, monitor.Bounds.xMax - edgeThickness, monitor.Bounds.yMin, monitor.Bounds.height, rightEdges);
            CreateEdgeQuad(EdgeDirection.Top, monitor.Bounds.xMin, monitor.Bounds.yMax - edgeThickness, monitor.Bounds.width, topEdges);
            CreateEdgeQuad(EdgeDirection.Bottom, monitor.Bounds.xMin, monitor.Bounds.yMin, monitor.Bounds.width, bottomEdges);
        }
    }

    void CreateEdgeQuad(EdgeDirection direction, int x, int y, float size, List<EdgeQuad> list)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = $"Edge_{direction}_{list.Count}";

        MeshRenderer mr = quad.GetComponent<MeshRenderer>();
        mr.material = new Material(glowMaterial);
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        EdgeQuad edgeQuad = quad.AddComponent<EdgeQuad>();
        edgeQuad.Initialize(mr.material);
        edgeQuad.direction = direction;

        float worldX = x + (direction == EdgeDirection.Right ? edgeThickness * 0.5f : 0f);
        float worldY = y + (direction == EdgeDirection.Top ? edgeThickness * 0.5f : 0f);
        float worldW = direction == EdgeDirection.Left || direction == EdgeDirection.Right ? edgeThickness : size;
        float worldH = direction == EdgeDirection.Top || direction == EdgeDirection.Bottom ? edgeThickness : size;

        quad.transform.position = new Vector3(worldX + worldW * 0.5f, worldY + worldH * 0.5f, 0);
        quad.transform.localScale = new Vector3(worldW, worldH, 1);

        list.Add(edgeQuad);
    }
}

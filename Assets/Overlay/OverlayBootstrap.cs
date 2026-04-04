using UnityEngine;
using UnityEngine.Rendering;

public class OverlayBootstrap : MonoBehaviour
{
    [SerializeField] private Material glowMaterial;
    [SerializeField] private float edgeThickness = 10f;

    private CursorMonitorTracker tracker;
    private EdgeGlowController glowController;

    void Start()
    {
        SetupCamera();
        SetupOverlayManager();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("OverlayCamera");
            cam = camObj.AddComponent<Camera>();
        }

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.cullingMask = ~0;
        cam.depth = -1;
        cam.allowMSAA = false;
        cam.allowHDR = false;
        cam.useOcclusionCulling = false;
    }

    void SetupOverlayManager()
    {
        GameObject managerObj = new GameObject("OverlayManager");
        managerObj.transform.SetParent(transform);

        tracker = managerObj.AddComponent<CursorMonitorTracker>();
        glowController = managerObj.AddComponent<EdgeGlowController>();

        if (glowMaterial != null)
        {
            glowController.glowMaterial = glowMaterial;
        }
        glowController.edgeThickness = edgeThickness;
    }
}

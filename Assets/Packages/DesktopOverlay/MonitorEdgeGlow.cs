using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to each edge-glow quad by <see cref="EdgeGlowController"/>.
/// Call <see cref="TriggerGlow"/> to start a flash-then-fade animation.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class MonitorEdgeGlow : MonoBehaviour
{
    /// <summary>Total duration of the glow fade-out in seconds.</summary>
    public float GlowDuration = 0.5f;

    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

    private Material   _material;
    private Coroutine  _activeCoroutine;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Obtain the instance material so each quad has its own alpha value.
        _material = GetComponent<MeshRenderer>().material;
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Immediately sets the glow to full intensity and begins fading it out
    /// over <see cref="GlowDuration"/> seconds.  Safe to call while a fade is
    /// already in progress — the existing coroutine is cancelled and restarted.
    /// </summary>
    public void TriggerGlow()
    {
        if (_material == null) return;

        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(GlowRoutine());
    }

    private IEnumerator GlowRoutine()
    {
        float elapsed = 0f;

        while (elapsed < GlowDuration)
        {
            // Quadratic ease-out: bright at start, fades quickly then lingers.
            float t     = elapsed / GlowDuration;
            float alpha = 1f - t * t;
            _material.SetFloat(AlphaId, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _material.SetFloat(AlphaId, 0f);
        _activeCoroutine = null;
    }
}

using UnityEngine;

public class EdgeQuad : MonoBehaviour
{
    [SerializeField] private EdgeDirection direction;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private float glowDuration = 0.4f;
    [SerializeField] private AnimationCurve glowCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private float glowTimer = -1f;
    private float currentIntensity = 0f;

    public EdgeDirection Direction => direction;

    public void TriggerGlow()
    {
        glowTimer = glowDuration;
    }

    void Start()
    {
        if (glowMaterial != null)
        {
            glowMaterial.SetInt("_EdgeDirection", (int)direction);
            glowMaterial.SetFloat("_Intensity", 0f);
        }
    }

    void Update()
    {
        if (glowTimer >= 0f)
        {
            glowTimer -= Time.deltaTime;
            float t = 1f - (glowTimer / glowDuration);
            currentIntensity = glowCurve.Evaluate(t);

            if (glowTimer <= 0f)
            {
                glowTimer = -1f;
                currentIntensity = 0f;
            }

            if (glowMaterial != null)
            {
                glowMaterial.SetFloat("_Intensity", currentIntensity);
            }
        }
    }

    public void Initialize(Material material)
    {
        glowMaterial = material;
        if (glowMaterial != null)
        {
            glowMaterial.SetInt("_EdgeDirection", (int)direction);
            glowMaterial.SetFloat("_Intensity", 0f);
        }
    }
}

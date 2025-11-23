using UnityEngine;

public class HockeyPuckHighlight : MonoBehaviour
{
    public Material highlightMaterial;     // Assign a bright/glowing material in Inspector
    public Color trailColor = Color.red;
    public float trailWidth = 0.1f;
    public float trailTime = 1.0f;

    private Material originalMaterial;
    private Renderer puckRenderer;
    private TrailRenderer trailRenderer;

    void Start()
    {
        // Get the puck's renderer and store the original material
        puckRenderer = GetComponent<Renderer>();
        if (puckRenderer != null)
        {
            originalMaterial = puckRenderer.material;
        }

        // Add and configure the TrailRenderer
        trailRenderer = gameObject.AddComponent<TrailRenderer>();
        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = trailColor;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = 0f;
        trailRenderer.time = trailTime;
        trailRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));


        // Set highlight material
        if (highlightMaterial != null && puckRenderer != null)
        {
            puckRenderer.material = highlightMaterial;
        }
    }

    // Optional: Remove highlight and trail
    public void RemoveHighlight()
    {
        if (puckRenderer != null && originalMaterial != null)
        {
            puckRenderer.material = originalMaterial;
        }
        if (trailRenderer != null)
        {
            Destroy(trailRenderer);
        }
    }
}

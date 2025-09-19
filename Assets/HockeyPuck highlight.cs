using UnityEngine;

public class HockeyPuckhighlight : MonoBehaviour
{
    public GameObject trailVFXPrefab;      // Assign a Trails VFX URP prefab in Inspector
    public Material highlightMaterial;     // Assign a bright/glowing material in Inspector

    private Material originalMaterial;
    private Renderer puckRenderer;
    private GameObject trailInstance;

    void Start()
    {
        // Get the puck's renderer and store the original material
        puckRenderer = GetComponent<Renderer>();
        if (puckRenderer != null)
        {
            originalMaterial = puckRenderer.material;
        }

        // Instantiate and attach the trail VFX prefab
        if (trailVFXPrefab != null)
        {
            trailInstance = Instantiate(trailVFXPrefab, transform);
            trailInstance.transform.localPosition = Vector3.zero;
        }

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
        if (trailInstance != null)
        {
            Destroy(trailInstance);
        }
    }
}

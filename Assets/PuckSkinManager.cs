using UnityEngine;

public class PuckSkinManager : MonoBehaviour
{
    [Header("Skin-Original")]
    public GameObject originalPuck;
    public Material originalMaterial;

    [Header("Skin-Duck")]
    public GameObject duckPuck;
    public Material duckMaterial;

    private const string DuckPuckEnabledKey = "DuckPuckEnabled";

    // Cached renderer references so we don't accidentally disable the GameObject that hosts this script
    private Renderer rootRenderer;
    private Renderer originalRenderer;
    private Renderer duckRenderer;

    void Start()
    {
        CacheRenderers();
        ApplySkin();
    }

    void OnEnable()
    {
        // Ensure skin is applied if this gameobject is enabled again at runtime
        CacheRenderers();
        ApplySkin();
    }

    private void CacheRenderers()
    {
        // Root renderer if this script is on a root object with a mesh
        rootRenderer = GetComponent<Renderer>();

        // Avoid searching on nulls; use GetComponentInChildren so we find child geometry too
        if (originalPuck != null)
            originalRenderer = originalPuck.GetComponentInChildren<Renderer>();
        else
            originalRenderer = GetComponentInChildren<Renderer>();

        if (duckPuck != null)
            duckRenderer = duckPuck.GetComponentInChildren<Renderer>();
        else
            duckRenderer = null;
    }

    void OnValidate()
    {
        // Warn the developer if the script is set to toggle the GameObject that it is attached to
        if (originalPuck == gameObject)
        {
            Debug.LogWarning("PuckSkinManager: originalPuck points to the same GameObject that this script is attached to. Toggle will use renderer.enabled to avoid disabling the script.");
        }
        if (duckPuck == gameObject)
        {
            Debug.LogWarning("PuckSkinManager: duckPuck points to the same GameObject that this script is attached to. Toggle will use renderer.enabled to avoid disabling the script.");
        }
    }

    // Make the method public so other scripts (e.g., SettingsMenu) can call it
    public void ApplySkin()
    {
        bool duckPuckEnabled = PlayerPrefs.GetInt(DuckPuckEnabledKey, 0) == 1;

        // Debug: log to verify that the method runs and what it's toggling.
        Debug.Log($"ApplySkin called on '{gameObject.name}'. DuckEnabled={duckPuckEnabled}, originalPuck set={(originalPuck != null)}, duckPuck set={(duckPuck != null)}");

        if (duckPuckEnabled)
        {
            // If the originalPuck is the host object for this script, don't disable the entire object (that would stop this script)
            if (originalPuck != null && originalPuck != gameObject)
                originalPuck.SetActive(false);
            else if (originalRenderer != null)
                originalRenderer.enabled = false;

            if (duckPuck != null)
                duckPuck.SetActive(true);
            else if (duckRenderer != null)
                duckRenderer.enabled = true;

            if (duckRenderer != null)
                duckRenderer.material = duckMaterial;
            else if (rootRenderer != null)
                rootRenderer.material = duckMaterial;
            else
                Debug.LogWarning($"PuckSkinManager: Duck material requested but no renderer available on '{gameObject.name}'.");
        }
        else
        {
            if (duckPuck != null && duckPuck != gameObject)
                duckPuck.SetActive(false);
            else if (duckRenderer != null)
                duckRenderer.enabled = false;

            if (originalPuck != null)
            {
                // Avoid re-enabling a root object that may be controlled elsewhere
                if (originalPuck != gameObject)
                    originalPuck.SetActive(true);
                else if (originalRenderer != null)
                    originalRenderer.enabled = true;
            }

            if (originalRenderer != null)
                originalRenderer.material = originalMaterial;
            else if (rootRenderer != null)
                rootRenderer.material = originalMaterial;
            else
                Debug.LogWarning($"PuckSkinManager: Original material requested but no renderer available on '{gameObject.name}'.");
        }
    }

    // Utility: refresh the skin on all puck instances currently active in the scene
    public static void RefreshAllPucks()
    {
#if UNITY_2023_1_OR_NEWER
        var all = UnityEngine.Object.FindObjectsByType<PuckSkinManager>(UnityEngine.FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<PuckSkinManager>();
#endif
        Debug.Log($"PuckSkinManager.RefreshAllPucks: Found {all.Length} instances.");
        foreach (var m in all)
        {
            if (m != null) m.ApplySkin();
        }
    }
}

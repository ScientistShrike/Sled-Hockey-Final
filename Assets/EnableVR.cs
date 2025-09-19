
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
#endif

public class EnableVR : MonoBehaviour
{
    void Awake()
    {
#if UNITY_EDITOR
        var settings = XRGeneralSettings.Instance;
        if (settings == null)
        {
            Debug.LogError("XRGeneralSettings is null.");
            return;
        }
        settings.InitManagerOnStart = true;
#endif
    }
}

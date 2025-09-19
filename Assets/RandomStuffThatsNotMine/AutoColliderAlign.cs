using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(BoxCollider))]
public class AutoColliderAlign : MonoBehaviour
{

    void Start()
    {
        var rect = GetComponent<RectTransform>();
        var collider = GetComponent<BoxCollider>();

        collider.size = new Vector3(rect.rect.width, rect.rect.height, 1f);
        collider.center = new Vector3(rect.rect.width / 2, -rect.rect.height / 2, 0f);
    }


    void Update()
    {

    }
}

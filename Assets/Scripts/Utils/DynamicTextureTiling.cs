using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DynamicTextureTiling : MonoBehaviour
{
    [SerializeField] private float scaleX;
    [SerializeField] private float scaleZ;
    private Renderer rend;
    private Vector3 initialScale;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        Vector3 currentScale = transform.localScale;

        // Calculate tiling based on scale ratio (relative to initial)
        float tileX = currentScale.x * scaleX;
        float tileZ = currentScale.z * scaleZ;

        rend.material.mainTextureScale = new Vector2(tileX, tileZ);
    }
}
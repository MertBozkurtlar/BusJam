using UnityEngine;

/// <summary>
/// Simple colour-setter for the bus prefab.
/// </summary>
public class Bus : MonoBehaviour
{
    public void SetColour(ColorId id)
    {
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = id.ToUnityColor();
    }
}
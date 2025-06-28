using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class ColorMetaAttribute : PropertyAttribute
{
    public readonly float r, g, b;
    public ColorMetaAttribute(float r, float g, float b) { this.r=r; this.g=g; this.b=b; }
    public Color Color => new Color(r, g, b);
}

public enum ColorId
{
    [ColorMeta(.9f, .2f, .2f)]  Red,
    [ColorMeta(.2f, .1f, .9f)]  Blue,
    [ColorMeta(.1f, .9f, .1f)] Green,
    [ColorMeta(1f, 1f, .2f)]  Yellow
}

public enum CellKind
{
    Empty,
    PassengerRed = 1 + (int)ColorId.Red,
    PassengerBlue = 1 + (int)ColorId.Blue,
    PassengerGreen = 1 + (int)ColorId.Green,
    PassengerYellow = 1 + (int)ColorId.Yellow,
    Void,
}

[CreateAssetMenu(menuName = "BusJam/LevelData")]
public class LevelData : ScriptableObject
{
    public int timeLimit = 10;
    public int rows = 6, cols = 6;
    public CellKind[] cells = new CellKind[36];
    
    public ColorId[] buses = { ColorId.Red, ColorId.Blue };

    private void OnValidate()
    {
        int needed = Mathf.Max(1, rows) * Mathf.Max(1, cols);
        if (cells == null || cells.Length != needed) cells = new CellKind[needed];
    }
}
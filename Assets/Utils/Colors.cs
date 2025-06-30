using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attribute for defining the display colour of a <see cref="ColorId"/> entry.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ColorMetaAttribute : PropertyAttribute
{
    public readonly float r, g, b;
    public ColorMetaAttribute(float r, float g, float b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }
    public Color Color => new Color(r, g, b);
}

public enum ColorId
{
    [ColorMeta(.9f, .2f, .2f)] Red,
    [ColorMeta(.2f, .1f, .9f)] Blue,
    [ColorMeta(.1f, .9f, .1f)] Green,
    [ColorMeta( 1f,  1f, .2f)] Yellow
}

/// <summary>
/// Converts a <see cref="ColorId"/> to a <see cref="UnityEngine.Color"/>.
/// Adding a new colour only requires a new enum entry with the <see cref="ColorMetaAttribute"/>.
/// </summary>
public static class ColorUtil
{
    private static readonly Dictionary<ColorId, Color> lookup;

    static ColorUtil()
    {
        lookup = new Dictionary<ColorId, Color>();
        foreach (ColorId id in Enum.GetValues(typeof(ColorId)))
        {
            var field = typeof(ColorId).GetField(id.ToString());
            var meta  = Attribute.GetCustomAttribute(field, typeof(ColorMetaAttribute)) as ColorMetaAttribute;
            lookup[id] = meta == null ? Color.magenta : meta.Color;
        }
    }

    public static Color ToUnityColor(this ColorId id) => lookup[id];
}
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class LevelDesignerWindow : EditorWindow
{
    [MenuItem("Bus Jam/Level Designer")]
    private static void Open() => GetWindow<LevelDesignerWindow>("Level Designer");

    private static readonly Dictionary<ColorId, Color> colorOf =
        Enum.GetValues(typeof(ColorId))
             .Cast<ColorId>()
             .ToDictionary(
                 id => id,
                 id =>
                 {
                     var a = (ColorMetaAttribute)typeof(ColorId)
                             .GetField(id.ToString())
                             .GetCustomAttributes(typeof(ColorMetaAttribute), false)
                             .First();
                     return a.Color;
                 });

    private static readonly Dictionary<CellKind, Color> passengerColors =
        Enum.GetValues(typeof(ColorId))
             .Cast<ColorId>()
             .ToDictionary(
                 id => (CellKind)(1 + (int)id),
                 id => colorOf[id]);

    private LevelData level;
    private SerializedObject so;
    private SerializedProperty rowsProp, colsProp, cellsProp, busesProp, timeLimitProp;
    private ReorderableList busList;
    private CellKind brush = CellKind.Empty;

    private void OnGUI()
    {
        DrawHeader();
        if (level == null) return;

        GUILayout.Space(3);
        DrawGrid();
        GUILayout.Space(6);
        DrawPalette();
        GUILayout.Space(10);
        DrawBusQueue();
    }

    private void DrawHeader()
    {
        EditorGUI.BeginChangeCheck();
        level = (LevelData)EditorGUILayout.ObjectField("Level Asset", level, typeof(LevelData), false);
        if (EditorGUI.EndChangeCheck()) CacheProps();

        if (level != null && (so == null || so.targetObject != level)) CacheProps();
        if (level == null) return;

        so.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(timeLimitProp);
        EditorGUILayout.PropertyField(rowsProp);
        EditorGUILayout.PropertyField(colsProp);
        if (EditorGUI.EndChangeCheck())
        {
            int need = Math.Max(1, rowsProp.intValue) * Math.Max(1, colsProp.intValue);
            if (cellsProp.arraySize != need) cellsProp.arraySize = need;
            so.ApplyModifiedProperties();
            return;
        }

        so.ApplyModifiedProperties();
    }

    private void CacheProps()
    {
        if (level == null) { so = null; return; }

        so       = new SerializedObject(level);
        rowsProp = so.FindProperty(nameof(LevelData.rows));
        colsProp = so.FindProperty(nameof(LevelData.cols));
        cellsProp= so.FindProperty(nameof(LevelData.cells));
        busesProp= so.FindProperty(nameof(LevelData.buses));
        timeLimitProp = so.FindProperty(nameof(LevelData.timeLimit));

        busList = new ReorderableList(so, busesProp, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Bus Queue (front first)"),
            drawElementCallback = (rect, idx, _, __) =>
            {
                var elem = busesProp.GetArrayElementAtIndex(idx);
                elem.enumValueIndex = (int)(ColorId)EditorGUI.EnumPopup(rect, (ColorId)elem.enumValueIndex);
            }
        };
    }

    private void DrawGrid()
    {
        so.Update();

        int rows = Math.Max(1, rowsProp.intValue);
        int cols = Math.Max(1, colsProp.intValue);
        const float btn = 22f;

        for (int r = 0; r < rows; r++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                if (idx >= cellsProp.arraySize) continue;

                var cell = cellsProp.GetArrayElementAtIndex(idx);
                var kind = (CellKind)cell.enumValueIndex;

                GUI.backgroundColor = ColorFor(kind);
                if (GUILayout.Button(GUIContent.none, GUILayout.Width(btn), GUILayout.Height(btn)))
                {
                    Undo.RecordObject(level, "Paint Cell");
                    cell.enumValueIndex = Event.current.button == 1 ? (int)CellKind.Void : (int)brush;
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
        so.ApplyModifiedProperties();
    }

    private void DrawPalette()
    {
        GUILayout.Label("Brush");
        EditorGUILayout.BeginHorizontal();
        DrawBrush(CellKind.Empty, "Empty");
        DrawBrush(CellKind.Void, "Void");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        foreach (var kv in passengerColors)
            DrawBrush(kv.Key, kv.Key.ToString().Replace("Passenger", ""));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBrush(CellKind kind, string label)
    {
        GUI.backgroundColor = ColorFor(kind);
        bool pick = GUILayout.Toggle(brush == kind, label, "Button", GUILayout.Height(24));
        if (pick) brush = kind;
        GUI.backgroundColor = Color.white;
    }

    private void DrawBusQueue()
    {
        so.Update();
        busList?.DoLayoutList();
        so.ApplyModifiedProperties();
    }

    private static Color ColorFor(CellKind k)
    {
        if (k == CellKind.Empty) return Color.white;
        if (k == CellKind.Void)  return new Color(.25f, .25f, .25f);
        return passengerColors.TryGetValue(k, out var c) ? c : Color.magenta;
    }
}
#endif

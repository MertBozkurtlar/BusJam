#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using BusJam;

public class LevelDesignerWindow : EditorWindow
{
    [MenuItem("Bus Jam/Level Designer")]
    private static void Open() => GetWindow<LevelDesignerWindow>("Level Designer");

    /* static colour tables */
    private static readonly Dictionary<ColorId, Color> colorOf =
        Enum.GetValues(typeof(ColorId))
            .Cast<ColorId>()
            .ToDictionary(
                id => id,
                id => ((ColorMetaAttribute)typeof(ColorId)
                       .GetField(id.ToString())
                       .GetCustomAttributes(typeof(ColorMetaAttribute), false)
                       .First()).Color);

    private static readonly Color VoidGrey = new(.25f, .25f, .25f);

    /* instance state  */
    private LevelData        level;
    private SerializedObject so;
    private SerializedProperty rowsProp, colsProp, cellsProp, busesProp, timeLimitProp, waitingAreaSizeProp;
    private ReorderableList  busList;

    // Current paint brush
    private Cell brush = new() { type = CellType.Empty, colour = 0 };
    
    private Vector2 scrollPosition;

    /* GUI */
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        if (level == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        GUILayout.Space(3);
        DrawGrid();
        GUILayout.Space(6);
        DrawPalette();
        GUILayout.Space(10);
        DrawBusQueue();

        EditorGUILayout.EndScrollView();
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
        EditorGUILayout.PropertyField(waitingAreaSizeProp);
        EditorGUILayout.PropertyField(rowsProp);
        EditorGUILayout.PropertyField(colsProp);

        if (EditorGUI.EndChangeCheck())
        {
            int need = Mathf.Max(1, rowsProp.intValue) * Mathf.Max(1, colsProp.intValue);
            if (cellsProp.arraySize != need) cellsProp.arraySize = need;
            so.ApplyModifiedProperties();
            return;                                   // skip grid this frame
        }

        so.ApplyModifiedProperties();
    }

    private void CacheProps()
    {
        if (level == null) { so = null; return; }

        so = new SerializedObject(level);
        
        rowsProp            = so.FindProperty(nameof(LevelData.rows));
        colsProp            = so.FindProperty(nameof(LevelData.cols));
        cellsProp           = so.FindProperty(nameof(LevelData.cells));
        busesProp           = so.FindProperty(nameof(LevelData.buses));
        timeLimitProp       = so.FindProperty(nameof(LevelData.timeLimit));
        waitingAreaSizeProp = so.FindProperty(nameof(LevelData.waitingAreaSize));

        busList = new ReorderableList(so, busesProp, true, true, true, true)
        {
            drawHeaderCallback = r => EditorGUI.LabelField(r, "Bus Queue (front first)"),
            drawElementCallback = (r, i, _, __) =>
            {
                var elem = busesProp.GetArrayElementAtIndex(i);
                elem.enumValueIndex = (int)(ColorId)EditorGUI.EnumPopup(r, (ColorId)elem.enumValueIndex);
            }
        };
    }


    private void DrawGrid()
    {
        so.Update();

        int rows = Mathf.Max(1, rowsProp.intValue);
        int cols = Mathf.Max(1, colsProp.intValue);
        const float btn = 22f;

        for (int r = 0; r < rows; r++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                if (idx >= cellsProp.arraySize) continue;

                var cellProp = cellsProp.GetArrayElementAtIndex(idx);
                var typeProp   = cellProp.FindPropertyRelative(nameof(Cell.type));
                var colourProp = cellProp.FindPropertyRelative(nameof(Cell.colour));

                var type   = (CellType)typeProp.enumValueIndex;
                var colour = (ColorId)colourProp.enumValueIndex;

                GUI.backgroundColor = ColorFor(type, colour);

                if (GUILayout.Button(GUIContent.none, GUILayout.Width(btn), GUILayout.Height(btn)))
                {
                    Undo.RecordObject(level, "Paint Cell");

                    if (Event.current.button == 1)          // right-click → Void
                    {
                        typeProp.enumValueIndex   = (int)CellType.Void;
                        colourProp.enumValueIndex = 0;
                    }
                    else                                    // left-click → current brush
                    {
                        typeProp.enumValueIndex   = (int)brush.type;
                        colourProp.enumValueIndex = (int)brush.colour;
                    }
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

        // Empty & Void
        EditorGUILayout.BeginHorizontal();
        DrawBrush(CellType.Empty, 0,   "Empty");
        DrawBrush(CellType.Void,  0,   "Void");
        EditorGUILayout.EndHorizontal();

        // Passenger colours
        EditorGUILayout.BeginHorizontal();
        foreach (var kv in colorOf)
            DrawBrush(CellType.Passenger, kv.Key, kv.Key.ToString());
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBrush(CellType type, ColorId col, string label)
    {
        GUI.backgroundColor = ColorFor(type, col);

        bool pick = GUILayout.Toggle(
            brush.type   == type && brush.colour == col,
            label, "Button", GUILayout.Height(24));

        if (pick)
        {
            brush.type   = type;
            brush.colour = col;
        }

        GUI.backgroundColor = Color.white;
    }
    

    private void DrawBusQueue()
    {
        so.Update();
        busList?.DoLayoutList();
        so.ApplyModifiedProperties();
    }


    private static Color ColorFor(CellType type, ColorId id) =>
        type switch
        {
            CellType.Empty     => Color.white,
            CellType.Void      => VoidGrey,
            CellType.Passenger => colorOf[id],
            _                  => Color.magenta
        };
}
#endif

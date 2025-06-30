using System;
using System.Linq;
using UnityEngine;

/* ─────────────────────────  CELL DEFINITION  ───────────────────────── */

public enum CellType { Empty, Void, Passenger }

[Serializable]
public struct Cell
{
    public CellType type;   // type of the cell
    public ColorId  colour; // used only when type == Passenger
}

/* ─────────────────────────  LEVEL ASSET  ───────────────────────── */

[CreateAssetMenu(menuName = "BusJam/LevelData")]
public class LevelData : ScriptableObject
{
    [Min(1)] public int  timeLimit = 10;
    [Min(2)] public int waitingAreaSize = 5;
    [Min(1)] public int  rows      = 6;
    [Min(1)] public int  cols      = 6;

    public Cell[]   cells = new Cell[36];                // row-major grid
    public ColorId[] buses = { ColorId.Red, ColorId.Blue };

    private static readonly Cell EmptyCell = new() { type = CellType.Empty, colour = 0 };

    private void OnValidate()
    {
        int needed = Math.Max(1, rows) * Math.Max(1, cols);

        if (cells == null || cells.Length != needed)
            cells = Enumerable.Repeat(EmptyCell, needed).ToArray();
    }

    // helpers
    public int  Index(int r, int c) => r * cols + c;
    public Cell GetCell(int r, int c) => cells[Index(r, c)];
    public void SetCell(int r, int c, Cell v) => cells[Index(r, c)] = v;
}
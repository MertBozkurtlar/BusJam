using UnityEngine;

/// <summary>
/// Runtime state of the level grid. (0, 0) is the top-left cell.
/// </summary>
public enum CellState { Empty, Void, Passenger }

public sealed class GridModel
{
    public readonly int rows;
    public readonly int cols;

    private readonly CellState[,] cells;
    private int version;

    public int Version => version;

    public GridModel(int rows, int cols)
    {
        this.rows = rows;
        this.cols = cols;
        cells = new CellState[rows, cols];
    }

    public CellState this[int r, int c]
    {
        get => cells[r, c];
        set { cells[r, c] = value; version++; }
    }

    public bool InBounds(int r, int c) =>
        r >= 0 && r < rows && c >= 0 && c < cols;
}
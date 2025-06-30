using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller responsible for level logic:
/// Instantiates the level, owns the grid model, handles click events, finds paths and moves passengers.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level Asset")]
    public LevelData levelData;

    [Header("Scene Anchors")]
    public Transform gridAnchor;
    public Transform waitingAreaAnchor;
    public Transform busAnchor;

    [Header("Prefabs")]
    public GameObject gridTilePrefab;
    public GameObject waitingAreaTilePrefab;
    public GameObject passengerPrefab;
    public GameObject busPrefab;
    public GameObject gridPlane;

    [Header("Layout")]
    [SerializeField] float cellSize = 1f;
    [SerializeField] float busOffset = -3.5f;

    Transform gridParent, waitingParent, busParent;
    GridModel grid;

    readonly Dictionary<(int r, int c, int v), List<Vector2Int>> pathCache = new();
    readonly List<Passenger> passengers = new();

    readonly Queue<int> freeWaitingSlots = new();
    readonly List<Vector3> waitingPositions = new();
    readonly Dictionary<int, Passenger> waitingOccupancy = new();

    ColorId CurrentBusColour => levelData.buses.Length > 0 ? levelData.buses[0] : 0;

    void Start()
    {
        BuildParents();
        BuildGridAndPassengers();
        BuildWaitingArea();
        SpawnBuses();
        ResizeGridPlane();
    }

    void BuildParents()
    {
        gridParent = new GameObject("GridTiles").transform;
        waitingParent = new GameObject("WaitingArea").transform;
        busParent = new GameObject("Buses").transform;

        gridParent.SetParent(transform);
        waitingParent.SetParent(transform);
        busParent.SetParent(transform);
    }

    void BuildGridAndPassengers()
    {
        grid = new GridModel(levelData.rows, levelData.cols);

        for (int r = 0; r < levelData.rows; r++)
        {
            for (int c = 0; c < levelData.cols; c++)
            {
                Cell cellData = levelData.GetCell(r, c);
                switch (cellData.type)
                {
                    case CellType.Void:
                        grid[r, c] = CellState.Void;
                        break;

                    case CellType.Empty:
                        grid[r, c] = CellState.Empty;
                        Instantiate(gridTilePrefab, GridToWorld(r, c),
                                    Quaternion.identity, gridParent);
                        break;

                    case CellType.Passenger:
                        grid[r, c] = CellState.Passenger;
                        Instantiate(gridTilePrefab, GridToWorld(r, c),
                                    Quaternion.identity, gridParent);

                        var pObj = Instantiate(passengerPrefab, GridToWorld(r, c),
                                               Quaternion.identity, gridParent);
                        var pv = pObj.GetComponent<Passenger>();
                        pv.Init(this, r, c, cellData.colour);
                        passengers.Add(pv);
                        break;
                }
            }
        }
    }

    void BuildWaitingArea()
    {
        int n = levelData.waitingAreaSize;
        float half = (n - 1) / 2f;

        for (int i = 0; i < n; i++)
        {
            Vector3 pos = waitingAreaAnchor.position +
                          new Vector3((i - half) * cellSize, 0f, 0f);
            Instantiate(waitingAreaTilePrefab, pos, Quaternion.identity, waitingParent);

            freeWaitingSlots.Enqueue(i);
            waitingPositions.Add(pos);
            waitingOccupancy[i] = null;
        }
    }

    void SpawnBuses()
    {
        for (int i = 0; i < levelData.buses.Length; i++)
        {
            Vector3 pos = busAnchor.position + new Vector3(busOffset * i, 0f, 0f);
            var busObj = Instantiate(busPrefab, pos, Quaternion.identity, busParent);
            busObj.GetComponent<Bus>()?.SetColour(levelData.buses[i]);
        }
    }

    void ResizeGridPlane()
    {
        if (!gridPlane) return;
        gridPlane.transform.localScale =
            new Vector3((levelData.cols) * cellSize / 10f, 1f,
                        (levelData.rows + 1) * cellSize / 10f);
        gridPlane.transform.position = gridAnchor.position - new Vector3(0f, 0f, 1f + 0.5f * cellSize);
    }

    public Vector3 GridToWorld(int r, int c)
    {
        float half = (levelData.cols - 1) / 2f;
        return gridAnchor.position +
               new Vector3((c - half) * cellSize, 0f, -r * cellSize);
    }

    public void OnPassengerClicked(Passenger pv)
    {
        int versionBefore = grid.Version;
        var key = (pv.Row, pv.Col, versionBefore);

        if (!pathCache.TryGetValue(key, out var path))
        {
            path = FindPathToFirstRow(pv.Row, pv.Col);
            pathCache[key] = path;
        }

        // No path found at all → give up.
        if (path == null) return;

        // Free the starting cell so others can walk through it.
        grid[pv.Row, pv.Col] = CellState.Empty;

        // Already on the first row → go straight to bus / waiting area.
        if (path.Count == 0)
        {
            NotifyPassengerArrived(pv);
            return;
        }

        // Normal case: follow the computed path.
        pv.PlayPath(path);
    }


    List<Vector2Int> FindPathToFirstRow(int sr, int sc)
    {
        var dirs = new[]
        {
            new Vector2Int( 1, 0), new Vector2Int(-1, 0),
            new Vector2Int( 0, 1), new Vector2Int( 0,-1)
        };

        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var q = new Queue<Vector2Int>();
        var start = new Vector2Int(sr, sc);

        q.Enqueue(start);
        parent[start] = start;

        Vector2Int? exit = null;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur.x == 0)
            {
                exit = cur;
                break;
            }

            foreach (var d in dirs)
            {
                var nxt = cur + d;
                if (!grid.InBounds(nxt.x, nxt.y)) continue;
                if (parent.ContainsKey(nxt))       continue;
                if (grid[nxt.x, nxt.y] != CellState.Empty) continue;

                parent[nxt] = cur;
                q.Enqueue(nxt);
            }
        }

        if (!exit.HasValue) return null;

        var path = new List<Vector2Int>();
        for (var v = exit.Value; v != start; v = parent[v])
            path.Add(v);
        path.Reverse();
        return path;
    }

    public void NotifyPassengerArrived(Passenger pv)
    {
        if (pv.Row != 0) return;   // not on first row

        if (pv.Colour == CurrentBusColour)
        {
            pv.MoveToPoint(busAnchor.position, 4f,
                () =>
                {
                    passengers.Remove(pv);
                    Destroy(pv.gameObject);
                });
        }
        else if (freeWaitingSlots.Count > 0)
        {
            int slot = freeWaitingSlots.Dequeue();
            waitingOccupancy[slot] = pv;
            pv.MoveToPoint(waitingPositions[slot], 4f);
        }
    }
}

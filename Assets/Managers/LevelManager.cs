using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
    [SerializeField] int busCapacity = 3;

    Transform gridParent, waitingParent, busParent;
    GridModel grid;

    readonly Dictionary<(int r, int c, int v), List<Vector2Int>> pathCache = new();
    readonly List<Passenger> passengers = new();
    readonly Queue<Bus> buses = new();
    readonly Queue<ColorId> upcomingBusColors = new();
    readonly Queue<Passenger> arrivalQueue = new();

    readonly List<int> freeWaitingSlots = new();
    readonly List<Vector3> waitingPositions = new();
    readonly Dictionary<int, Passenger> waitingOccupancy = new();

    private bool isDepartureSequenceRunning = false;
    private int pendingDepartures = 0;
    private bool isGameOver = false;

    ColorId CurrentBusColour => buses.Count > 0 ? buses.Peek().Colour : 0;

    void Start()
    {
        BuildParents();
        BuildGridAndPassengers();
        BuildWaitingArea();
        SpawnInitialBuses();
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

            freeWaitingSlots.Add(i);
            waitingPositions.Add(pos);
            waitingOccupancy[i] = null;
        }
    }

    void SpawnInitialBuses()
    {
        foreach (var color in levelData.buses)
        {
            upcomingBusColors.Enqueue(color);
        }

        int initialBusCount = Mathf.Min(levelData.buses.Length, 3); // Spawn up to 3 buses initially
        for (int i = 0; i < initialBusCount; i++)
        {
            SpawnNextBus(i);
        }
    }

    void SpawnNextBus(int positionIndex)
    {
        if (upcomingBusColors.Count == 0) return;

        ColorId color = upcomingBusColors.Dequeue();
        Vector3 pos = busAnchor.position + new Vector3(busOffset * positionIndex, 0f, 0f);
        var busObj = Instantiate(busPrefab, pos, Quaternion.identity, busParent);
        var bus = busObj.GetComponent<Bus>();
        bus.SetColour(color);
        bus.busCapacity = busCapacity;
        buses.Enqueue(bus);
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
        if (isGameOver) return;

        var path = FindPathToFirstRow(pv.Row, pv.Col);
        if (path == null) return;

        grid[pv.Row, pv.Col] = CellState.Empty;

        if (path.Count == 0)
        {
            NotifyPassengerArrived(pv);
            return;
        }

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
        if (pv.Row != 0) return;
        arrivalQueue.Enqueue(pv);
        if (!isDepartureSequenceRunning) 
        {
            ProcessArrivalQueue();
        }
    }

    private void ProcessArrivalQueue()
    {
        if (isDepartureSequenceRunning || arrivalQueue.Count == 0) return;

        var pv = arrivalQueue.Dequeue();

        bool shouldBoard = buses.Count > 0 && 
                           pv.Colour == CurrentBusColour && 
                           !buses.Peek().IsFull;

        if (shouldBoard)
        {
            BoardBus(pv);
        }
        else
        {
            GoToWaitingArea(pv);
        }
    }

    private void BoardBus(Passenger pv, int waitingSlot = -1)
    {
        var currentBus = buses.Peek();
        pv.MoveToPoint(busAnchor.position, 4f,
            () =>
            {
                passengers.Remove(pv);
                currentBus.AddPassenger();
                Destroy(pv.gameObject);

                if (waitingSlot != -1)
                {
                    freeWaitingSlots.Add(waitingSlot);
                    freeWaitingSlots.Sort();
                }
                
                if (currentBus.IsFull)
                {
                    pendingDepartures++;
                    ProcessNextDeparture();
                }
            });
    }

    private void GoToWaitingArea(Passenger pv)
    {
        pv.SetWaiting();
        int slot = freeWaitingSlots[0];
        freeWaitingSlots.RemoveAt(0);

        waitingOccupancy[slot] = pv;
        pv.MoveToPoint(waitingPositions[slot], 4f, () => {
            CheckForGameLost();
        });
    }

    private void CheckForGameLost()
    {
        if (isGameOver) return; 

        if (freeWaitingSlots.Count > 0) return; 

        if (isDepartureSequenceRunning || pendingDepartures > 0)
        {
            return; 
        }

        Debug.Log("Game Lost: Waiting area full");
        isGameOver = true;
    }

    private void ProcessNextDeparture()
    {
        if (isDepartureSequenceRunning || pendingDepartures == 0) return;

        isDepartureSequenceRunning = true;
        pendingDepartures--;

        var departingBus = buses.Dequeue();

        if (buses.Count == 0 && upcomingBusColors.Count == 0)
        {
            Debug.Log("Game WON");
            isGameOver = true;
        }

        var sequence = DOTween.Sequence();

        Tween departureTween = departingBus.Depart();
        var moveUpSequence = DOTween.Sequence();
        
        int i = 0;
        foreach (var bus in buses)
        {
            moveUpSequence.Join(bus.transform.DOMoveX(busAnchor.position.x + busOffset * i, 0.5f));
            i++;
        }

        moveUpSequence.OnComplete(() =>
        {
            SpawnNextBus(buses.Count);
            isDepartureSequenceRunning = false;
            CheckWaitingArea();
            ProcessArrivalQueue();
        });

        sequence.Join(departureTween);
        sequence.Join(moveUpSequence);

        sequence.OnComplete(() =>
        {
            ProcessNextDeparture();
        });
    }

    private void CheckWaitingArea()
    {
        if (buses.Count == 0 || buses.Peek().IsFull) return;

        var currentBus = buses.Peek();
        var availableSlots = currentBus.busCapacity - currentBus.PassengerCount;
        var currentBusColor = CurrentBusColour;

        var matchingPassengers = waitingOccupancy
            .Where(kvp => kvp.Value != null && kvp.Value.Colour == currentBusColor)
            .Select(kvp => kvp.Value)
            .Take(availableSlots)
            .ToList();

        foreach (var passenger in matchingPassengers)
        {
            var slot = waitingOccupancy.First(kvp => kvp.Value == passenger).Key;
            waitingOccupancy[slot] = null;
            BoardBus(passenger, slot);
        }
    }
}

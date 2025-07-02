using UnityEngine;
using System.Collections.Generic;

namespace BusJam 
{
    // Internal representation of grid cell states for pathfinding
    enum CellState { Empty, Occupied, Void }
    
    /// <summary>
    /// Manages the grid layout, including building tiles and providing pathfinding on the grid.
    /// </summary>
    public class GridManager : MonoBehaviour 
    {
        [Header("Grid Settings")]
        [SerializeField] private Transform gridAnchor;
        [SerializeField] private GameObject gridTilePrefab;
        [SerializeField] private GameObject gridPlane;
        [SerializeField] private float cellSize = 1f;

        private int rows;
        private int cols;
        private CellState[,] gridState;

        /// <summary>Builds the grid tiles and internal grid model from the given level data.</summary>
        public void BuildGrid(LevelData levelData) 
        {
            rows = levelData.rows;
            cols = levelData.cols;
            gridState = new CellState[rows, cols];

            // Create a parent object to hold all grid tiles for organization
            Transform gridParent = new GameObject("GridTiles").transform;
            gridParent.SetParent(transform);

            // Instantiate grid tiles and set up grid state
            for (int r = 0; r < rows; r++) 
            {
                for (int c = 0; c < cols; c++) 
                {
                    var cellData = levelData.GetCell(r, c);
                    switch (cellData.type) 
                    {
                        case CellType.Void:
                            gridState[r, c] = CellState.Void;
                            break;
                        case CellType.Empty:
                            gridState[r, c] = CellState.Empty;
                            Instantiate(gridTilePrefab, GridToWorld(r, c), Quaternion.identity, gridParent);
                            break;
                        case CellType.Passenger:
                            // Place a floor tile and mark this cell occupied by a passenger
                            gridState[r, c] = CellState.Occupied;
                            Instantiate(gridTilePrefab, GridToWorld(r, c), Quaternion.identity, gridParent);
                            // (Passenger object will be spawned by PassengerManager)
                            break;
                    }
                }
            }
        }

        /// <summary>Converts grid coordinates (row, col) to a world position.</summary>
        public Vector3 GridToWorld(int r, int c) 
        {
            float half = (cols - 1) / 2f;
            // X offset is based on column index relative to center, Z offset moves negative for each row down
            return gridAnchor.position + new Vector3((c - half) * cellSize, 0f, -r * cellSize);
        }

        /// <summary>
        /// Finds a path from the given start cell to the top row of the grid using BFS. 
        /// Returns a list of grid coordinates to move through, or null if no path exists.
        /// </summary>
        public List<Vector2Int> FindPathToFirstRow(int startR, int startC) 
        {
            if (!InBounds(startR, startC)) return null;

            // Directions for four-way movement (up, down, left, right)
            Vector2Int[] directions = 
            {
                new Vector2Int(1, 0),   // move down
                new Vector2Int(-1, 0),  // move up
                new Vector2Int(0, 1),   // move right
                new Vector2Int(0, -1)   // move left
            };

            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            Vector2Int start = new Vector2Int(startR, startC);
            queue.Enqueue(start);
            parent[start] = start;
            Vector2Int? exitCell = null;

            // Breadth-first search
            while (queue.Count > 0) 
            {
                Vector2Int cur = queue.Dequeue();
                if (cur.x == 0) 
                {
                    exitCell = cur;
                    break;  // reached top row
                }
                foreach (var d in directions) 
                {
                    Vector2Int next = new Vector2Int(cur.x + d.x, cur.y + d.y);
                    if (!InBounds(next.x, next.y)) continue;
                    if (parent.ContainsKey(next)) continue;
                    if (gridState[next.x, next.y] != CellState.Empty) continue;  // only traverse empty cells
                    parent[next] = cur;
                    queue.Enqueue(next);
                }
            }
            if (!exitCell.HasValue) 
            {
                // No path to top row
                return null;
            }

            // Reconstruct path from exitCell back to start
            var path = new List<Vector2Int>();
            for (Vector2Int v = exitCell.Value; v != start; v = parent[v]) 
            {
                path.Add(v);
            }
            path.Reverse();
            return path;
        }

        /// <summary>Marks the given grid cell as empty (e.g., when a passenger leaves the cell).</summary>
        public void MarkCellEmpty(int r, int c) 
        {
            if (InBounds(r, c)) 
            {
                gridState[r, c] = CellState.Empty;
            }
        }

        /// <summary>Resizes and positions the ground plane to cover the grid and waiting area for visual purposes.</summary>
        public void ResizeGridPlane(int rows, int cols) 
        {
            if (gridPlane != null) 
            {
                // The Unity Plane primitive is 10 units x 10 units at scale=1
                gridPlane.transform.localScale = new Vector3(cols * cellSize / 10f, 1f, (rows + 1) * cellSize / 10f);
                gridPlane.transform.position = gridAnchor.position - new Vector3(0f, 0f, (rows - 1) * cellSize / 2f);
            }
        }

        private bool InBounds(int r, int c) 
        {
            return r >= 0 && r < rows && c >= 0 && c < cols;
        }
        
        // Expose cell size for use by other systems (e.g., PassengerManager waiting area layout)
        public float CellSize => cellSize;
    }
}

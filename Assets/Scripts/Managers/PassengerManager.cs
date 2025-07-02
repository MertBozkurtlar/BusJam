using UnityEngine;
using System.Collections.Generic;

namespace BusJam 
{
    /// <summary>
    /// Manages passenger creation, movement, and waiting area interactions (extracted from GameController).
    /// </summary>
    public class PassengerManager : MonoBehaviour 
    {
        [Header("Passenger Settings")]
        [SerializeField] private GameObject passengerPrefab;

        [Header("Waiting Area Settings")]
        [SerializeField] private Transform waitingAreaAnchor;
        [SerializeField] private GameObject waitingAreaTilePrefab;

        // Queue for arriving passengers (those who reach the exit row)
        private readonly Queue<Passenger> arrivalQueue = new Queue<Passenger>();
        // Waiting area tracking
        private readonly List<int> freeWaitingSlots = new List<int>();
        private readonly List<Vector3> waitingPositions = new List<Vector3>();
        private readonly Dictionary<int, Passenger> waitingOccupancy = new Dictionary<int, Passenger>();
        // Active passengers list (for tracking and cleanup)
        private readonly List<Passenger> activePassengers = new List<Passenger>();

        private void OnEnable() 
        {
            // Subscribe to passenger events
            Passenger.OnPassengerClicked += HandlePassengerClicked;
            Passenger.OnReachedExitRow += HandlePassengerArrived;
        }

        private void OnDisable() 
        {
            Passenger.OnPassengerClicked -= HandlePassengerClicked;
            Passenger.OnReachedExitRow -= HandlePassengerArrived;
        }

        /// <summary>Resets the PassengerManager, destroying all active passengers and clearing waiting area data.</summary>
        public void Reset()
        {
            // Destroy all active passenger GameObjects
            foreach (var passenger in activePassengers)
            {
                if (passenger != null) Destroy(passenger.gameObject);
            }
            activePassengers.Clear();

            // Clear waiting area data
            arrivalQueue.Clear();
            freeWaitingSlots.Clear();
            waitingPositions.Clear();
            waitingOccupancy.Clear();

            // Destroy waiting area tiles if they exist
            Transform waitingParent = transform.Find("WaitingArea");
            if (waitingParent != null)
            {
                Destroy(waitingParent.gameObject);
            }
        }

        /// <summary>Spawns all passengers defined in the level data onto the grid.</summary>
        public void SpawnPassengers(LevelData levelData) 
        {
            for (int r = 0; r < levelData.rows; r++) 
            {
                for (int c = 0; c < levelData.cols; c++) 
                {
                    var cell = levelData.GetCell(r, c);
                    if (cell.type == CellType.Passenger) 
                    {
                        // Instantiate passenger at the correct world position
                        Vector3 worldPos = GameStateManager.Instance.GridManager.GridToWorld(r, c);
                        GameObject pObj = Instantiate(passengerPrefab, worldPos, Quaternion.identity);
                        Passenger passenger = pObj.GetComponent<Passenger>();
                        // Initialize passenger data (no direct GameController reference needed)
                        passenger.Init(r, c, cell.colour, GameStateManager.Instance.GridManager);
                        activePassengers.Add(passenger);
                    }
                }
            }
        }

        /// <summary>Builds the waiting area tiles and initializes waiting slot data.</summary>
        public void BuildWaitingArea(int size) 
        {
            // Create a container for waiting area tiles
            Transform waitingParent = new GameObject("WaitingArea").transform;
            waitingParent.SetParent(transform);

            freeWaitingSlots.Clear();
            waitingPositions.Clear();
            waitingOccupancy.Clear();

            float half = (size - 1) / 2f;
            for (int i = 0; i < size; i++) 
            {
                // Compute world position for each waiting slot
                Vector3 pos = waitingAreaAnchor.position + new Vector3((i - half) * GameStateManager.Instance.GridManager.CellSize, 0f, 0f);
                Instantiate(waitingAreaTilePrefab, pos, Quaternion.identity, waitingParent);
                freeWaitingSlots.Add(i);
                waitingPositions.Add(pos);
                waitingOccupancy[i] = null;
            }
        }

        /// <summary>Handles a passenger being clicked by the player. Starts movement or game if needed.</summary>
        private void HandlePassengerClicked(Passenger passenger) 
        {
            // Prevent interaction if game is over
            if (GameStateManager.Instance && GameStateManager.Instance.IsGameOver) return;

            // Start the game on the first click (timer begins)
            GameStateManager.Instance.StartGame();

            // Find a path from the passenger's current position to the exit (top row)
            List<Vector2Int> path = GameStateManager.Instance.GridManager.FindPathToFirstRow(passenger.Row, passenger.Col);
            if (path == null) 
            {
                // No path found (blocked) – do nothing
                return;
            }

            // Free the grid cell the passenger is leaving
            GameStateManager.Instance.GridManager.MarkCellEmpty(passenger.Row, passenger.Col);

            if (path.Count == 0) 
            {
                // Passenger is already at the exit row, treat as arrived immediately
                HandlePassengerArrived(passenger);
            } 
            else 
            {
                // Move the passenger along the path towards the exit
                passenger.PlayPath(path);
            }
        }

        /// <summary>Handles a passenger reaching the exit row of the grid.</summary>
        private void HandlePassengerArrived(Passenger passenger) 
        {
            if (passenger.Row != 0) return;  // Only process if at first row (exit)

            arrivalQueue.Enqueue(passenger);
            // If no bus departure is in progress, immediately process this arrival
            if (!GameStateManager.Instance.BusManager.IsDepartureSequenceRunning) 
            {
                ProcessArrivalQueue();
            }
        }

        /// <summary>Processes the next arriving passenger in queue, either boarding a bus or moving to waiting area.</summary>
        internal void ProcessArrivalQueue() 
        {
            var busMgr = GameStateManager.Instance.BusManager;
            if (busMgr.IsDepartureSequenceRunning || arrivalQueue.Count == 0) return;

            Passenger p = arrivalQueue.Dequeue();
            bool shouldBoard = busMgr.HasBus && p.Colour == busMgr.CurrentBusColor && !busMgr.CurrentBusIsFull;
            if (shouldBoard) 
            {
                // Board the passenger onto the current bus
                busMgr.BoardPassengerOntoBus(p);
            } 
            else 
            {
                // Send the passenger to the waiting area
                GoToWaitingArea(p);
            }
        }

        /// <summary>Sends a passenger to the next available waiting slot (when they cannot board immediately).</summary>
        private void GoToWaitingArea(Passenger passenger) 
        {
            passenger.SetWaiting();
            // Assign the first free waiting slot
            freeWaitingSlots.Sort();
            int slot = freeWaitingSlots[0];
            freeWaitingSlots.RemoveAt(0);
            waitingOccupancy[slot] = passenger;
            // Move the passenger to the waiting position, then check for game over after arrival
            passenger.MoveToPoint(waitingPositions[slot], 4f, () =>
            {
                CheckForGameLost();
            });
        }

        /// <summary>Checks if the game is lost (no waiting slots left and no bus departing soon).</summary>
        private void CheckForGameLost() 
        {
            var busMgr = GameStateManager.Instance.BusManager;
            if (GameStateManager.Instance.IsGameOver) return;
            if (freeWaitingSlots.Count > 0) return;
            if (busMgr.IsDepartureSequenceRunning || busMgr.PendingDepartures > 0) return;
            // All waiting slots are full, and no bus departure will free a slot – trigger Game Over
            GameStateManager.Instance.TriggerGameLost();
        }

        /// <summary>Removes a passenger from active tracking (e.g., after boarding a bus).</summary>
        public void RemovePassenger(Passenger passenger) 
        {
            activePassengers.Remove(passenger);
        }

        /// <summary>
        /// Retrieves up to <paramref name="maxCount"/> waiting passengers of the given color for boarding, 
        /// freeing their waiting slots in the process.
        /// </summary>
        public List<Passenger> GetWaitingPassengers(ColorId color, int maxCount) 
        {
            var result = new List<Passenger>();
            int count = 0;
            // Iterate through waiting slots to find matching passengers
            for (int slot = 0; slot < waitingOccupancy.Count && count < maxCount; slot++) 
            {
                Passenger p = waitingOccupancy[slot];
                if (p != null && p.Colour == color) 
                {
                    result.Add(p);
                    // Free this slot
                    waitingOccupancy[slot] = null;
                    freeWaitingSlots.Add(slot);
                    count++;
                }
            }
            freeWaitingSlots.Sort();
            return result;
        }

        /// <summary>Frees a waiting area slot (called after a waiting passenger boards a bus).</summary>
        public void FreeWaitingSlot(int slot) 
        {
            waitingOccupancy[slot] = null;
            freeWaitingSlots.Add(slot);
            freeWaitingSlots.Sort();
        }
    }
}

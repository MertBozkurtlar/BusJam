using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace BusJam 
{
    /// <summary>
    /// Manages bus spawning, the bus queue, boarding of passengers, and bus departure sequences.
    /// </summary>
    public class BusManager : MonoBehaviour 
    {
        [Header("Bus Settings")]
        [SerializeField] private Transform busAnchor;
        [SerializeField] private GameObject busPrefab;
        [SerializeField] private float busOffset = -3.5f;
        [SerializeField] private int busCapacity = 3;

        private Queue<ColorId> upcomingBusColors = new Queue<ColorId>();
        private Queue<Bus> buses = new Queue<Bus>();
        private Transform busContainer;
        private bool isDepartureSequenceRunning = false;
        private int pendingDepartures = 0;
        private int boardingCount = 0;  // Number of passengers currently moving to the bus

        // Properties for other managers or GameStateManager to query the bus queue status
        public bool IsDepartureSequenceRunning => isDepartureSequenceRunning;
        public int PendingDepartures => pendingDepartures;
        public bool HasBus => buses.Count > 0;
        public ColorId CurrentBusColor => (buses.Count > 0 ? buses.Peek().Colour : 0);
        public bool CurrentBusIsFull => (buses.Count > 0 ? buses.Peek().IsFull : false);
        public bool HasSpaceInBus => (buses.Count > 0 && buses.Peek().PassengerCount + boardingCount < buses.Peek().busCapacity);

        /// <summary>Initializes the bus queue for a new level, spawning up to 3 initial buses.</summary>
        public void InitializeBuses(ColorId[] busColorSequence) 
        {
            Reset(); // Clear existing buses and queues

            // Create a container object for buses
            busContainer = new GameObject("Buses").transform;
            busContainer.SetParent(transform);

            if (busPrefab == null)
            {
                Debug.LogError("Bus Prefab is not assigned in BusManager!");
                return;
            }

            if (busColorSequence == null || busColorSequence.Length == 0)
            {
                Debug.LogWarning("Bus color sequence is empty. No buses will be spawned.");
                return;
            }

            foreach (var color in busColorSequence) 
            {
                upcomingBusColors.Enqueue(color);
            }
            // Spawn up to 3 buses initially (or fewer if the level has less)
            int initialCount = Mathf.Min(busColorSequence.Length, 3);
            for (int i = 0; i < initialCount; i++) 
            {
                SpawnNextBus(i);
            }
        }

        /// <summary>Spawns the next bus in the queue at the given queue position index.</summary>
        private void SpawnNextBus(int positionIndex) 
        {
            if (upcomingBusColors.Count == 0) return;
            ColorId color = upcomingBusColors.Dequeue();
            Vector3 pos = busAnchor.position + new Vector3(busOffset * positionIndex, 0f, 0f);
            GameObject busObj = Instantiate(busPrefab, pos, Quaternion.identity, busContainer);
            Bus bus = busObj.GetComponent<Bus>();
            bus.SetColour(color);
            bus.busCapacity = busCapacity;
            buses.Enqueue(bus);
        }

        /// <summary>
        /// Boards a passenger onto the current bus.
        /// If the bus becomes full, begins the departure sequence.
        /// </summary>
        public void BoardPassengerOntoBus(Passenger passenger, int waitingSlot = -1) 
        {
            if (buses.Count == 0) return;
            Bus currentBus = buses.Peek();
            // Start moving the passenger to the bus and reserve a spot
            boardingCount++;
            passenger.MoveToPoint(busAnchor.position, 4f, () =>
            {
                // Callback after the passenger reaches the bus
                GameStateManager.Instance.PassengerManager.RemovePassenger(passenger);
                currentBus.AddPassenger();
                Destroy(passenger.gameObject);
                if (waitingSlot != -1) 
                {
                    // Free the waiting slot if this passenger came from waiting area
                    GameStateManager.Instance.PassengerManager.FreeWaitingSlot(waitingSlot);
                }
                // If the bus is now full, schedule its departure
                if (currentBus.IsFull) 
                {
                    pendingDepartures++;
                    ProcessNextDeparture();
                }
                // Passenger finished boarding, free up the reserved spot
                boardingCount--;
            });
        }

        /// <summary>Processes the next pending bus departure (if any) in the queue.</summary>
        private void ProcessNextDeparture() 
        {
            if (isDepartureSequenceRunning || pendingDepartures == 0) return;
            isDepartureSequenceRunning = true;
            pendingDepartures--;

            Bus departingBus = buses.Dequeue();
            // Check win condition: no more buses remaining and none upcoming
            if (buses.Count == 0 && upcomingBusColors.Count == 0) 
            {
                GameStateManager.Instance.TriggerGameWon();
            }

            // Animate bus departure and shift queue using DOTween
            Sequence sequence = DOTween.Sequence();
            Tween departureTween = departingBus.Depart();  // bus drives off and destroys itself
            // Move remaining buses forward in the queue
            Sequence moveUpSequence = DOTween.Sequence();
            int i = 0;
            foreach (var bus in buses) 
            {
                moveUpSequence.Join(bus.transform.DOMoveX(busAnchor.position.x + busOffset * i, 0.5f));
                i++;
            }

            // After buses have moved up, spawn a new bus at the back of the queue (if any remain)
            moveUpSequence.OnComplete(() =>
            {
                SpawnNextBus(buses.Count);
                isDepartureSequenceRunning = false;
                // New bus has arrived – board any waiting passengers of matching color
                TryFillBusFromWaiting();
                // Process any passengers that arrived while the departure was in progress
                GameStateManager.Instance.PassengerManager.ProcessArrivalQueue();
            });

            // Play departure and queue shift simultaneously
            sequence.Join(departureTween);
            sequence.Join(moveUpSequence);
            sequence.OnComplete(() =>
            {
                // If additional departures are pending, process the next one
                ProcessNextDeparture();
            });
        }

        /// <summary>
        /// Attempts to board waiting passengers onto the current bus after a new bus arrives.
        /// </summary>
        private void TryFillBusFromWaiting() 
        {
            if (buses.Count == 0) return;
            Bus currentBus = buses.Peek();
            if (currentBus.IsFull) return;

            int availableSlots = currentBus.busCapacity - currentBus.PassengerCount;
            // Get up to 'availableSlots' waiting passengers of the current bus's color
            var waitingPassengers = GameStateManager.Instance.PassengerManager.GetWaitingPassengers(currentBus.Colour, availableSlots);
            foreach (var passenger in waitingPassengers) 
            {
                // Immediately board each freed waiting passenger onto the bus
                BoardPassengerOntoBus(passenger);
            }
        }

        /// <summary>Resets the BusManager, destroying all active buses and clearing queues.</summary>
        public void Reset()
        {
            // Destroy all existing bus GameObjects
            if (busContainer != null)
            {
                foreach (Transform child in busContainer)
                {
                    Destroy(child.gameObject);
                }
                Destroy(busContainer.gameObject);
                busContainer = null;
            }

            upcomingBusColors.Clear();
            buses.Clear();
            isDepartureSequenceRunning = false;
            pendingDepartures = 0;
            boardingCount = 0;
        }
    }
}

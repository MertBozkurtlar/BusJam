using UnityEngine;
using System;

namespace BusJam 
{
    enum GameState { Start, Running, GameOver }
    
    /// <summary>
    /// Singleton manager that handles overall game state, including level loading, timer management, and win/loss conditions.
    /// </summary>
    public class GameStateManager : Singleton<GameStateManager> 
    {
        [Header("Game Data")]
        [SerializeField] private SaveData saveData;  // ScriptableObject for persistent player progress (level, etc.)

        // References to other managers (assigned via inspector or automatically in Init)
        [SerializeField] private PassengerManager passengerManager;
        [SerializeField] private BusManager busManager;
        [SerializeField] private GridManager gridManager;

        private LevelData currentLevelData;
        private GameState state;
        private Coroutine timerCoroutine;
        public int TimeLeft { get; private set; }

        // Events for game state transitions (UI can subscribe to these)
        public event Action OnGameWon;
        public event Action OnGameLost;
        public event Action OnGameReset;
        
        // Public getters for other managers (global access over singleton)
        public PassengerManager PassengerManager => passengerManager;
        public BusManager BusManager => busManager;
        public GridManager GridManager => gridManager;
        public bool IsGameOver => state == GameState.GameOver;

        

        protected override void Init() 
        {
            // Ensure references to other managers (find in scene if not set in inspector)
            if (passengerManager == null) passengerManager = FindObjectOfType<PassengerManager>();
            if (busManager == null) busManager = FindObjectOfType<BusManager>();
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            // (DontDestroyOnLoad could be called here if GameStateManager should persist between scenes)
        }

        private void Start() 
        {
            if (saveData == null) 
            {
                Debug.LogError("SaveData is not assigned in GameStateManager!");
                return;
            }
            LoadLevel();
        }

        /// <summary>Loads the current level specified by SaveData, setting up the grid, passengers, waiting area, and buses.</summary>
        public void LoadLevel() 
        {
            string path = $"Levels/Level{saveData.CurrentLevel}";
            Debug.Log($"Loading level: {path}");
            currentLevelData = Resources.Load<LevelData>(path);
            if (currentLevelData == null) 
            {
                Debug.LogError($"Level data not found at {path}");
                return;
            }

            // Initialize game state for new level
            TimeLeft = currentLevelData.timeLimit;
            state = GameState.Start;

            // Reset managers before building new level elements
            passengerManager.Reset();
            busManager.Reset();
            gridManager.Reset(); // Assuming GridManager also needs a reset for its grid elements

            // Build the scene elements via the managers
            gridManager.BuildGrid(currentLevelData);
            passengerManager.SpawnPassengers(currentLevelData);
            passengerManager.BuildWaitingArea(currentLevelData.waitingAreaSize);
            busManager.InitializeBuses(currentLevelData.buses);
            gridManager.ResizeGridPlane(currentLevelData.rows, currentLevelData.cols);

            // Notify listeners that the game has been reset (e.g., update UI for new level)
            OnGameReset?.Invoke();
        }

        /// <summary>Begins the game (starts the timer) on the first player interaction.</summary>
        public void StartGame() 
        {
            if (state != GameState.Start) return;
            state = GameState.Running;
            // Start the countdown timer coroutine
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(TickTimer());
        }

        private System.Collections.IEnumerator TickTimer() 
        {
            while (TimeLeft > 0) 
            {
                yield return new WaitForSeconds(1f);
                TimeLeft--;
            }
            OnTimerEnd();
        }

        private void OnTimerEnd() 
        {
            Debug.Log("Timer ended.");
            TriggerGameLost();
        }

        /// <summary>Triggers the Game Won state (called when all buses have departed and all passengers are served).</summary>
        public void TriggerGameWon() 
        {
            if (state == GameState.GameOver) return;
            state = GameState.GameOver;
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            OnGameWon?.Invoke();
            if (saveData != null) saveData.NextLevel();  // Advance to the next level in save data
        }

        /// <summary>Triggers the Game Lost state.</summary>
        public void TriggerGameLost() 
        {
            if (state == GameState.GameOver) return;
            state = GameState.GameOver;
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            OnGameLost?.Invoke();
        }
    }
}

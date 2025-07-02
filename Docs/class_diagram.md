```mermaid
classDiagram
    direction LR
    class Singleton {
        +T Instance
        #void Awake()
        #void OnDestroy()
        #void Init()
        #void Destroy()
    }

    class GameStateManager {
        +PassengerManager PassengerManager
        +BusManager BusManager
        +GridManager GridManager
        +bool IsGameOver
        +int TimeLeft
        +event OnGameWon
        +event OnGameLost
        +event OnGameReset
        +void LoadLevel()
        +void StartGame()
        +void TriggerGameWon()
        +void TriggerGameLost()
    }

    class BusManager {
        +bool IsDepartureSequenceRunning
        +int PendingDepartures
        +bool HasBus
        +ColorId CurrentBusColor
        +bool CurrentBusIsFull
        +bool HasSpaceInBus
        +void InitializeBuses(ColorId[] busColorSequence)
        +void BoardPassengerOntoBus(Passenger passenger, int waitingSlot)
        +void Reset()
    }

    class PassengerManager {
        +void Reset()
        +void SpawnPassengers(LevelData levelData)
        +void BuildWaitingArea(int size)
        +void RemovePassenger(Passenger passenger)
        +List~Passenger~ GetWaitingPassengers(ColorId color, int maxCount)
        +void FreeWaitingSlot(int slot)
        ~void ProcessArrivalQueue()
    }

    class GridManager {
        +float CellSize
        +void BuildGrid(LevelData levelData)
        +Vector3 GridToWorld(int r, int c)
        +List~Vector2Int~ FindPathToFirstRow(int startR, int startC)
        +void MarkCellEmpty(int r, int c)
        +void ResizeGridPlane(int rows, int cols)
        +void Reset()
    }

    class Bus {
        +GameObject passengerHeadPrefab
        +int busCapacity
        +ColorId Colour
        +int PassengerCount
        +bool IsFull
        +void SetColour(ColorId id)
        +void AddPassenger()
        +Tween Depart()
    }

    class Passenger {
        +int Row
        +int Col
        +ColorId Colour
        +static event OnPassengerClicked
        +static event OnReachedExitRow
        +void Init(int row, int col, ColorId colour, GridManager gridMgr)
        +void SetWaiting()
        +void PlayPath(IReadOnlyList~Vector2Int~ path, float speed)
        +void MoveToPoint(Vector3 target, float speed, Action onDone)
    }

    class LevelData {
        +int timeLimit
        +int waitingAreaSize
        +int rows
        +int cols
        +Cell[] cells
        +ColorId[] buses
        +int Index(int r, int c)
        +Cell GetCell(int r, int c)
        +void SetCell(int r, int c, Cell v)
    }

    class SaveData {
        +int CurrentLevel
        +void NextLevel()
        +void Reset()
        +void SaveProgress()
        +void LoadProgress()
    }

    class GameUI {
        +void LoadMainMenu()
    }

    class MainMenu {
        +void NextLevel()
        +void RestartProgress()
    }

    class LevelDesignerWindow {
        +static void Open()
    }

    class Billboard {
        +Camera mainCamera
    }

    class DynamicTextureTiling {
        
    }

    enum BusColor {
        Red
        Green
        Blue
    }

    enum CellType {
        Empty
        Void
        Passenger
    }

    enum ColorId {
        Red
        Blue
        Green
        Yellow
    }

    class ColorUtil {
        +Color ToUnityColor(ColorId id)
    }

    Singleton <|-- GameStateManager
    GameStateManager --> BusManager : manages
    GameStateManager --> PassengerManager : manages
    GameStateManager --> GridManager : manages
    BusManager ..> Bus : creates
    BusManager ..> GameStateManager : interacts
    PassengerManager ..> Passenger : creates
    PassengerManager ..> GameStateManager : interacts
    PassengerManager ..> LevelData : uses
    GridManager ..> LevelData : uses
    GridManager ..> GameStateManager : interacts
    Passenger --> GridManager : uses
    Passenger --> ColorId : uses
    Bus --> ColorId : uses
    GameUI --> GameStateManager : uses
    GameUI --> SaveData : uses
    MainMenu --> SaveData : uses
    LevelDesignerWindow --> LevelData : edits
    LevelDesignerWindow --> ColorId : uses
    LevelDesignerWindow --> CellType : uses
    ColorUtil --> ColorId : converts
    LevelData --> CellType : contains
    LevelData --> ColorId : contains
    Cell --> CellType : has
    Cell --> ColorId : has
    ColorId o-- ColorMetaAttribute : has
```
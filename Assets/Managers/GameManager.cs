using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameStateOptions
{
    Menu,
    Running,
    Pause,
    GameOver,
}

public class GameManager : MonoBehaviour
{
    public GameStateOptions GameState {get; private set;}

    public void ChangeState(GameStateOptions newState) {
        GameState = newState;
    }
    
    public void LoadScene(string sceneName, GameStateOptions newState)
    {
        ChangeState(newState);
        SceneManager.LoadScene(name);
    }

}

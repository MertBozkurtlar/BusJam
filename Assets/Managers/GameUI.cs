using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI timerText;
    [SerializeField] private TMPro.TextMeshProUGUI levelText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject winPanel;
    [SerializeField] SaveData saveData;
    GameController gameController;

    private void Awake()
    {
        gameController = FindObjectOfType<GameController>();
        gameController.OnGameWon += OnGameWon;
        gameController.OnGameLost += OnGameLost;
        gameController.OnGameReset += OnGameReset;
    }

    private void Update()
    {
        timerText.text = gameController.TimeLeft.ToString();
    }
    
    private void OnDestroy()
    {
        gameController.OnGameWon -= OnGameWon;
        gameController.OnGameLost -= OnGameLost;
    }
    
    private void OnGameWon()
    {
        winPanel.SetActive(true);
    }
    
    private void OnGameLost()
    {
        gameOverPanel.SetActive(true);
    }

    private void OnGameReset()
    {
        winPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        levelText.text = $"Level {saveData.CurrentLevel}";
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");   
    }
}

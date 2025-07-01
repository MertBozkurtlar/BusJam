using System;
using System.Collections;
using System.Collections.Generic;
using BusJam;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI timerText;
    [SerializeField] private TMPro.TextMeshProUGUI levelText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject winPanel;
    [SerializeField] SaveData saveData;
    GameStateManager gameStateManager;

    private void Awake()
    {
        gameStateManager = FindObjectOfType<GameStateManager>();
        gameStateManager.OnGameWon += OnGameWon;
        gameStateManager.OnGameLost += OnGameLost;
        gameStateManager.OnGameReset += OnGameReset;
    }

    private void Update()
    {
        timerText.text = gameStateManager.TimeLeft.ToString();
    }
    
    private void OnDestroy()
    {
        gameStateManager.OnGameWon -= OnGameWon;
        gameStateManager.OnGameLost -= OnGameLost;
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

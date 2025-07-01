using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BusJam;

public class MainMenu : MonoBehaviour
{
    public SaveData saveData;
    public TMPro.TextMeshProUGUI text;
    
    public void Start()
    {
        text.text = $"Current Level: {saveData.CurrentLevel}";
    }


    public void NextLevel()
    {
        SceneManager.LoadScene("GreenArea");
    }

    public void RestartProgress()
    {
        saveData.Reset();
        text.text = $"Current Level: {saveData.CurrentLevel}";
    }
}

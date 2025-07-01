using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SaveData", menuName = "Save/SaveData", order = 1)]
public class SaveData : ScriptableObject
{
    private int currentLevel;
    
    public int CurrentLevel => currentLevel;

    public void NextLevel()
    {
        currentLevel++;
    }

    public void Reset()
    {
        currentLevel = 1;
    }
}

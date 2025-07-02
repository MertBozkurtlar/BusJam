using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam
{
    [CreateAssetMenu(fileName = "SaveData", menuName = "Save/SaveData", order = 1)]
    public class SaveData : ScriptableObject
    {
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private int currentLevel;
        private bool isLoaded = false;

        public int CurrentLevel
        {
            get
            {
                if (!isLoaded)
                {
                    LoadProgress();
                    isLoaded = true;
                }
                return currentLevel;
            }
        }

        public void NextLevel()
        {
            if (currentLevel >= maxLevel)
            {
                Debug.Log("Max  level reached. Resetting the progress for the Prototype.");
                currentLevel = 1;
            }
            else
                currentLevel++;
            SaveProgress();
        }

        public void Reset()
        {
            currentLevel = 1;
            SaveProgress();
        }
        
        public void SaveProgress()
        {
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);
            PlayerPrefs.Save();
        }

        public void LoadProgress()
        {
            currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam
{
    [CreateAssetMenu(fileName = "SaveData", menuName = "Save/SaveData", order = 1)]
    public class SaveData : ScriptableObject
    {
        [SerializeField] private int currentLevel;

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
}

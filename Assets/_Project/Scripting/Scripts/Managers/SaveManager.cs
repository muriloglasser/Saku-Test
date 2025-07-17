using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class SaveManager : Initializer
{
    [Space(5)]
    [Header("Character Maps")]
    public static Dictionary<string, CharacterMap> _allCharacterMaps = new Dictionary<string, CharacterMap>();

    [Space(5)]
    [Header("Save Data")]
    public GameSaveData _currentGameData;
    public GameSaveData CurrentGameData
    {
        get
        {
            if (_currentGameData == null)
                LoadOrCreateSave();
            return _currentGameData;
        }
    }

    private string saveFileName = "gameSave.json";
    private string backupSaveFileName = "gameSave_backup.json";

    #region Initialization
    /// <summary>
    /// Initializes the SaveManager and loads all character maps
    /// </summary>
    public override void InitializeWithCustomPlayer(Player player)
    {
        base.InitializeWithCustomPlayer(player);
        LoadAllCharacterMaps();
        LoadOrCreateSave();
    }

    /// <summary>
    /// Loads all character maps from Resources if not already loaded
    /// </summary>
    private void LoadAllCharacterMaps()
    {
        if (!PlayerIsOwner())
            return;

        if (_allCharacterMaps.Count == 0)
        {
            CharacterMap[] loadedMaps = Resources.LoadAll<CharacterMap>("CharacterMaps");
            foreach (CharacterMap map in loadedMaps)
            {
                if (!_allCharacterMaps.ContainsKey(map.name))
                {
                    _allCharacterMaps.Add(map.name, map);
                }
            }
        }
    }
    #endregion

    #region Save Management
    /// <summary>
    /// Loads existing save file or creates a new one if none exists
    /// </summary>
    private void LoadOrCreateSave()
    {
        if (!PlayerIsOwner())
            return;

        if (SaveFileExists())
        {
            _currentGameData = LoadData();
            Debug.Log("Save file loaded successfully");
        }
        else
        {
            CreateNewSave();
            Debug.Log("New save file created");
        }
    }

    /// <summary>
    /// Creates a new save file with default values
    /// </summary>
    private void CreateNewSave()
    {
        if (!PlayerIsOwner())
            return;
        _currentGameData = new GameSaveData();
        SaveData();
    }

    /// <summary>
    /// Checks if a save file exists
    /// </summary>
    private bool SaveFileExists()
    {
        string filePath = GetSaveFilePath(saveFileName);
        return File.Exists(filePath);
    }
    #endregion

    #region Character Map Operations
    /// <summary>
    /// Checks if a character map is unlocked
    /// </summary>
    public bool IsMapUnlocked(string characterMapName)
    {
        foreach (CharacterMapSaveData item in CurrentGameData.unlockedCharacterMaps)
        {
            if (item.name == characterMapName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the currently selected map for a specific body type
    /// </summary>
    public string GetSelectedMapByBodyType(CharacterMapType characterMapType)
    {
        string mapNameTemp = null;
        mapNameTemp = CurrentGameData.currentCharacterMaps.Where(c => c.characterMapType == characterMapType).First().name;
        return mapNameTemp;
    }

    /// <summary>
    /// Attempts to purchase and unlock a character map
    /// </summary>
    public CharacterMap TryToBuyMap(string mapName, CharacterMapType characterMapType)
    {
        CharacterMap characterMap = GetCharacterMap(mapName, characterMapType);

        if (CurrentGameData.currency < characterMap.price)
            return null;

        CharacterMapSaveData characterMapSaveData = new CharacterMapSaveData(mapName, characterMapType);

        if (!CurrentGameData.unlockedCharacterMaps.Contains(characterMapSaveData))
        {
            CurrentGameData.currency -= characterMap.price;
            CurrentGameData.unlockedCharacterMaps.Add(characterMapSaveData);
            ReplaceCharacterMap(characterMapSaveData);
            SaveData();
            return characterMap;
        }

        return null;
    }

    /// <summary>
    /// Attempts to equip an unlocked character map
    /// </summary>
    public CharacterMap TryToEquipMap(string mapName, CharacterMapType characterMapType)
    {
        CharacterMap characterMap = GetCharacterMap(mapName, characterMapType);
        CharacterMapSaveData characterMapSaveData = null;

        for (int i = 0; i < CurrentGameData.unlockedCharacterMaps.Count; i++)
        {
            if (CurrentGameData.unlockedCharacterMaps[i].name == mapName)
            {
                characterMapSaveData = CurrentGameData.unlockedCharacterMaps[i];
                break;
            }
        }

        if (characterMapSaveData == null)
            return null;

        if (CurrentGameData.unlockedCharacterMaps.Contains(characterMapSaveData))
        {
            if (!CurrentGameData.currentCharacterMaps.Contains(characterMapSaveData))
            {
                ReplaceCharacterMap(characterMapSaveData);
                SaveData();
                return characterMap;
            }
        }

        return null;
    }

    /// <summary>
    /// Replaces a character map in the current equipment
    /// </summary>
    public void ReplaceCharacterMap(CharacterMapSaveData characterMapSaveData)
    {
        for (int i = 0; i < CurrentGameData.currentCharacterMaps.Count; i++)
        {
            if (CurrentGameData.currentCharacterMaps[i].characterMapType == characterMapSaveData.characterMapType)
                CurrentGameData.currentCharacterMaps[i] = characterMapSaveData;
        }
    }

    /// <summary>
    /// Gets a character map by name and type
    /// </summary>
    public static CharacterMap GetCharacterMap(string mapName, CharacterMapType characterMapType)
    {
        CharacterMap characterMap = null;

        foreach (CharacterMap item in _allCharacterMaps.Values)
        {
            if (item.characterMapType != characterMapType)
                continue;

            if (item.name != mapName)
                continue;

            characterMap = item;
        }
        return characterMap;
    }

    /// <summary>
    /// Gets a character map by name only
    /// </summary>
    public static CharacterMap GetCharacterMap(string mapName)
    {
        CharacterMap characterMap = null;

        foreach (CharacterMap item in _allCharacterMaps.Values)
        {           
            if (item.name != mapName)
                continue;

            characterMap = item;
        }
        return characterMap;
    }
    #endregion

    #region Color Operations
    /// <summary>
    /// Gets the saved color index for a specific character map type
    /// </summary>
    public int GetSavedColorIndexByCharmapType(CharacterMapType characterMapType)
    {
        int index = CurrentGameData.colorSelections.Where(c => c.mapType == characterMapType).First().colorIndex;
        return index;
    }

    /// <summary>
    /// Saves a color selection for a specific character map type
    /// </summary>
    public void SaveColor(CharacterMapType characterMapType, int colorIndex)
    {
        ColorData colorToChange = _currentGameData.colorSelections.Where(c => c.mapType == characterMapType).FirstOrDefault();
        colorToChange.colorIndex = colorIndex;

        for (int i = 0; i < _currentGameData.colorSelections.Count; i++)
        {
            if (_currentGameData.colorSelections[i].mapType == characterMapType)
            {
                _currentGameData.colorSelections[i] = colorToChange;
                break;
            }
        }

        SaveData();
    }
    #endregion

    #region File Operations
    /// <summary>
    /// Saves the current game data to file with backup
    /// </summary>
    public void SaveData()
    {
        if (!PlayerIsOwner())
            return;
        try
        {
            string filePath = GetSaveFilePath(saveFileName);
            string backupFilePath = GetSaveFilePath(backupSaveFileName);

            if (File.Exists(filePath))
            {
                File.Copy(filePath, backupFilePath, true);
            }

            string jsonData = JsonUtility.ToJson(_currentGameData, true);
            File.WriteAllText(filePath, jsonData);

            PlayerPrefs.SetString("LastSave", DateTime.Now.ToString());
            PlayerPrefs.Save();

            Debug.Log("Game data saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save game data: " + e.Message);
        }
    }

    /// <summary>
    /// Loads game data from file or backup
    /// </summary>
    private GameSaveData LoadData()
    {
        if (!PlayerIsOwner())
            return null;

        try
        {
            string filePath = GetSaveFilePath(saveFileName);

            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                return JsonUtility.FromJson<GameSaveData>(jsonData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load game data: " + e.Message);

            try
            {
                string backupFilePath = GetSaveFilePath(backupSaveFileName);
                if (File.Exists(backupFilePath))
                {
                    string jsonData = File.ReadAllText(backupFilePath);
                    Debug.Log("Loaded game data from backup");
                    return JsonUtility.FromJson<GameSaveData>(jsonData);
                }
            }
            catch (Exception backupEx)
            {
                Debug.LogError("Failed to load backup game data: " + backupEx.Message);
            }
        }

        Debug.LogWarning("Loading failed, creating new save data");
        return new GameSaveData();
    }

    /// <summary>
    /// Gets the full path for a save file
    /// </summary>
    private string GetSaveFilePath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
    #endregion

    #region SubClasses
    [Serializable]
    public class GameSaveData
    {
        public int currency = 0;
        public List<CharacterMapSaveData> unlockedCharacterMaps = new List<CharacterMapSaveData>();
        public List<CharacterMapSaveData> currentCharacterMaps = new List<CharacterMapSaveData>();
        public List<ColorData> colorSelections = new List<ColorData>();

        public GameSaveData()
        {
            currency = 100000;
            unlockedCharacterMaps = new();
            currentCharacterMaps = new();
            colorSelections = new();

            CharacterMapSaveData[] defaultMaps = {
                new CharacterMapSaveData("CharacterMap_Cheek_V1", CharacterMapType.cheeks),
                new CharacterMapSaveData("CharacterMap_Eye_V1", CharacterMapType.eyes),
                new CharacterMapSaveData("CharacterMap_Hair_V1", CharacterMapType.hair),
                new CharacterMapSaveData("CharacterMap_Leg_V1", CharacterMapType.legs),
                new CharacterMapSaveData("CharacterMap_Mouth_V1", CharacterMapType.mouth),
                new CharacterMapSaveData("CharacterMap_Shoes_V1", CharacterMapType.feet),
                new CharacterMapSaveData("CharacterMap_Torso_V1", CharacterMapType.torso),
                new CharacterMapSaveData("CharacterMap_Skin_V1", CharacterMapType.skin),
            };

            foreach (CharacterMapSaveData map in defaultMaps)
            {
                unlockedCharacterMaps.Add(map);
                currentCharacterMaps.Add(map);
            }

            foreach (CharacterMapType type in Enum.GetValues(typeof(CharacterMapType)))
            {
                if (type != CharacterMapType.none)
                {
                    colorSelections.Add(new ColorData(type, 0));
                }
            }
        }
    }

    [Serializable]
    public class CharacterMapSaveData
    {
        public string name;
        public CharacterMapType characterMapType;

        public CharacterMapSaveData(string name, CharacterMapType characterMapType)
        {
            this.name = name;
            this.characterMapType = characterMapType;
        }
    }

    [Serializable]
    public class ColorData
    {
        public CharacterMapType mapType;
        public int colorIndex;

        public ColorData(CharacterMapType mapType, int colorIndex)
        {
            this.mapType = mapType;
            this.colorIndex = colorIndex;
        }
    }
    #endregion
}
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static SaveManager;

public class CharacterMapResolver : Initializer
{
    [Header("Settings")]
    [SerializeField] private CharacterMapSlot[] _slots;    
    [Space(5)]
    [Header("References")]
    public SaveManager saveManager;    
    [Space(5)]
    [Header("Default Maps")]
    private NetworkColorChanger cachedColorChanger;
    private NetworkMeshChanger cachedSkinedMeshChanger;
    private UICharacterInventory uICharacterInventory;
    private CharacterMapSlot cachedSlot;

    #region Initialization
    public override void InitializeWithCustomPlayer(Player player, Initializer initializer = null)
    {
        base.InitializeWithCustomPlayer(player, initializer);
        uICharacterInventory = initializer as UICharacterInventory;
        InitializeCharacterMeshes();
        InitializeCharacterColors();
    }
    #endregion
    #region Character Color Methods
    /// <summary>
    /// Initializes all character colors from saved data
    /// </summary>
    public void InitializeCharacterColors()
    {
        foreach (ColorData item in saveManager.CurrentGameData.colorSelections)
        {
            ApplyColorByCharacterMapType(item.mapType, uICharacterInventory.colors[item.colorIndex]);
        }
    }

    /// <summary>
    /// Applies a color to a specific character map type
    /// </summary>
    public void ApplyColorByCharacterMapType(CharacterMapType characterMapType, Color color)
    {
        cachedSlot = _slots.Where(c => c.slotType == characterMapType).First();
        cachedColorChanger = cachedSlot.colorChanger;
        cachedColorChanger.ChangeColor(color);
    }
    #endregion
    #region Character Mesh Methods
    /// <summary>
    /// Initializes all character meshes from saved data
    /// </summary>
    public void InitializeCharacterMeshes()
    {
        foreach (CharacterMapSaveData item in saveManager.CurrentGameData.currentCharacterMaps)
        {
            ApplyCharactermapByCharacterMapType(item.characterMapType, item.name);
        }
    }

    /// <summary>
    /// Applies a mesh to a specific character map type
    /// </summary>
    public void ApplyCharactermapByCharacterMapType(CharacterMapType characterMapType, string mapName)
    {
        cachedSlot = _slots.Where(c => c.slotType == characterMapType).First();
        cachedSkinedMeshChanger = cachedSlot.skinnedMeshChanger;
        cachedSkinedMeshChanger.ChangeMesh(mapName);
    }
    #endregion
}

[System.Serializable]
public class CharacterMapSlot
{
    public CharacterMapType slotType;
    public NetworkMeshChanger skinnedMeshChanger;
    public NetworkColorChanger colorChanger;
    public SkinnedMeshRenderer targetRenderer;
    public int materialIndex = 0;
}
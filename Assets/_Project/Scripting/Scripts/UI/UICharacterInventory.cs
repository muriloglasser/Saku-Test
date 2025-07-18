using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using NUnit.Framework;
using System;

public class UICharacterInventory : Initializer
{
    [Header("Inventory Camera")]
    [SerializeField] private Camera inventoryCamera;
    
    [Space(5)]
    [Header("Inventory Colors")]
    public Sprite lockedIcon;
    public List<BodyTypeButtonIcon> bodyTypeIcons = new();
    public List<Color> colors = new();
    
    [Space(5)]
    [Header("Inventory Containers")]
    [SerializeField] private Transform typeButtonsContainer;
    [SerializeField] private Transform colorButtonsContainer;
    [SerializeField] private Transform mapsContainer;
    
    [Space(5)]
    [Header("Inventory Prefabs")]
    [SerializeField] private GameObject typeButtonPrefab;
    [SerializeField] private GameObject mapButtonPrefab;
    [SerializeField] private GameObject colorButtonPrefab;
    
    [Space(5)]
    [Header("Texts")]
    [SerializeField] private TMP_Text mapNameText;
    [SerializeField] private TMP_Text mapPriceText;
    [SerializeField] private TMP_Text currencyText;
    
    [Space(5)]
    [Header("Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button acquiredButton;
    [SerializeField] private Button customizeButton;

    private SaveManager saveManager;
    private Dictionary<CharacterMapType, UIInventoryButton> bodyTypeButtons = new();
    private Dictionary<UIInventoryButton, CharacterMapType> mapTypeButtons = new();
    private Dictionary<int, UIInventoryButton> colorButtons = new();
    private List<GameObject> cachedThreshList = new();
    private List<GameObject> cachedContainerChildList = new();
    private CharacterMapType currentSelectedBodyType;

    #region Initialization
    public override void InitializeWithCustomPlayer(Player player, Initializer initializer = null)
    {
        base.InitializeWithCustomPlayer(player, initializer);
        saveManager = initializer as SaveManager;
        customizeButton.gameObject.SetActive(true);
    }
    #endregion
    #region Inventory Management
    /// <summary>
    /// Toggles the inventory visibility
    /// </summary>
    public void ToggleInventory()
    {
        if (!gameObject.activeSelf) OpenInventory();
        else CloseInventory();
    }

    /// <summary>
    /// Opens the inventory and spawns all buttons
    /// </summary>
    public void OpenInventory()
    {
        gameObject.SetActive(true);
        inventoryCamera.gameObject.SetActive(true);
        InstantiateInventoryButtons();
    }

    /// <summary>
    /// Closes the inventory
    /// </summary>
    public void CloseInventory()
    {
        gameObject.SetActive(false);
        inventoryCamera.gameObject.SetActive(false);
    }
    #endregion
    #region Button Management
    /// <summary>
    /// Instantiates all inventory buttons (body types, colors and maps)
    /// </summary>
    public void InstantiateInventoryButtons()
    {
        ClearButtonsContainer(typeButtonsContainer);
        bodyTypeButtons.Clear();

        foreach (CharacterMapType characterMapType in Enum.GetValues(typeof(CharacterMapType)))
        {
            if (characterMapType == CharacterMapType.none) continue;

            GameObject go = Instantiate(typeButtonPrefab, typeButtonsContainer.transform.position, Quaternion.identity);
            go.transform.SetParent(typeButtonsContainer.transform);
            UIInventoryButton uiCharacterMapButton = go.GetComponent<UIInventoryButton>();
            uiCharacterMapButton.button.onClick.AddListener(() => { OnBodyTypeButtonClicked(uiCharacterMapButton, characterMapType); });
            uiCharacterMapButton.SetIconImage(bodyTypeIcons.Where(c => c.characterMapType == characterMapType).First().icon);
            bodyTypeButtons.Add(characterMapType, uiCharacterMapButton);
        }

        ClearButtonsContainer(colorButtonsContainer);
        colorButtons.Clear();

        for (int i = 0; i < colors.Count; i++)
        {
            int colorIndex = i;
            GameObject go = Instantiate(colorButtonPrefab, colorButtonsContainer.transform.position, Quaternion.identity);
            go.transform.SetParent(colorButtonsContainer.transform);
            UIInventoryButton uiCharacterMapButton = go.GetComponent<UIInventoryButton>();
            uiCharacterMapButton.SetIconColor(colors[colorIndex]);
            uiCharacterMapButton.button.onClick.AddListener(() => { OnColorButtonClicked(uiCharacterMapButton, currentSelectedBodyType, colorIndex); });
            colorButtons.Add(colorIndex, uiCharacterMapButton);
        }

        OnBodyTypeButtonClicked(bodyTypeButtons.Values.First(), bodyTypeButtons.Keys.First());
    }

    /// <summary>
    /// Clears all children from a button container
    /// </summary>
    public void ClearButtonsContainer(Transform containerTransform)
    {
        cachedContainerChildList.Clear();
        cachedThreshList.Clear();

        for (int i = 0; i < containerTransform.childCount; i++)
        {
            cachedContainerChildList.Add(containerTransform.GetChild(i).gameObject);
        }

        cachedThreshList = new(cachedContainerChildList);
        if (cachedThreshList.Count > 0)
        {
            for (int i = 0; i < cachedContainerChildList.Count; i++)
            {
                if (cachedThreshList[i] == null) continue;
                Destroy(cachedThreshList[i]);
            }
        }
    }
    #endregion
    #region Button Actions
    /// <summary>
    /// Handles body type button click and updates the UI accordingly
    /// </summary>
    public void OnBodyTypeButtonClicked(UIInventoryButton button, CharacterMapType characterMapType)
    {
        currentSelectedBodyType = characterMapType;
        foreach (UIInventoryButton item in bodyTypeButtons.Values)
        {
            item.SetSelected(false);
        }

        button.SetSelected(true);

        int colorButtonSavedIndex = saveManager.GetSavedColorIndexByCharmapType(characterMapType);
        UIInventoryButton colorButtonTemp = colorButtons[colorButtonSavedIndex];
        OnColorButtonClicked(colorButtonTemp, characterMapType, colorButtonSavedIndex);

        ClearButtonsContainer(mapsContainer);
        mapTypeButtons.Clear();

        foreach (CharacterMap item in SaveManager._allCharacterMaps.Values)
        {
            if (item.characterMapType == CharacterMapType.none || item.characterMapType != characterMapType) continue;

            GameObject go = Instantiate(mapButtonPrefab, mapsContainer.transform.position, Quaternion.identity);
            go.transform.SetParent(mapsContainer.transform);
            UIInventoryButton uiCharacterMapButton = go.GetComponent<UIInventoryButton>();
            uiCharacterMapButton.button.onClick.AddListener(() => { OnMapButtonClicked(uiCharacterMapButton, characterMapType, item.name); });
            uiCharacterMapButton.SetCachedMapName(item.name);
            mapTypeButtons.Add(uiCharacterMapButton, characterMapType);
        }

        string selectedMapName = saveManager.GetSelectedMapByBodyType(characterMapType);
        UIInventoryButton mapButton = mapTypeButtons.Where(c => c.Key.mapName == selectedMapName).First().Key;
        OnMapButtonClicked(mapButton, characterMapType, selectedMapName);
    }

    /// <summary>
    /// Handles color button click and applies the selected color
    /// </summary>
    public void OnColorButtonClicked(UIInventoryButton button, CharacterMapType characterMapType, int colorIndex)
    {
        foreach (UIInventoryButton item in colorButtons.Values)
        {
            item.SetSelected(false);
        }

        button.SetSelected(true);
        saveManager.SaveColor(characterMapType, colorIndex);
        player.characterMapResolver.ApplyColorByCharacterMapType(characterMapType, colors[colorIndex]);
    }

    /// <summary>
    /// Handles map button click and updates the UI based on map unlock status
    /// </summary>
    public void OnMapButtonClicked(UIInventoryButton button, CharacterMapType characterMapType, string mapName)
    {
        foreach (UIInventoryButton item in mapTypeButtons.Keys)
        {
            item.SetSelected(false);
            bool isMapUnlocked = saveManager.IsMapUnlocked(item.mapName);
            if (!isMapUnlocked) item.SetIconImage(lockedIcon);
            else item.SetIconImage(bodyTypeIcons.Where(c => c.characterMapType == characterMapType).First().icon);
        }

        bool isSelectedMapUnlocked = saveManager.IsMapUnlocked(button.mapName);

        if (isSelectedMapUnlocked)
        {
            buyButton.gameObject.SetActive(false);
            acquiredButton.gameObject.SetActive(true);
            button.SetIconImage(bodyTypeIcons.Where(c => c.characterMapType == characterMapType).First().icon);
            player.characterMapResolver.ApplyCharactermapByCharacterMapType(characterMapType, mapName);
        }
        else
        {
            buyButton.gameObject.SetActive(true);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => { OnBuyButtonClicked(button, characterMapType, mapName); });
            acquiredButton.gameObject.SetActive(false);
            button.SetIconImage(lockedIcon);
        }

        button.SetSelected(true);
        saveManager.TryToEquipMap(mapName, characterMapType);
        CharacterMap selectedMap = SaveManager.GetCharacterMap(mapName, characterMapType);
        UpdateTexts(selectedMap.id, selectedMap.price);
    }
    #endregion
    #region Purchase System
    /// <summary>
    /// Handles purchase attempts for locked maps
    /// </summary>
    public void OnBuyButtonClicked(UIInventoryButton button, CharacterMapType characterMapType, string mapName)
    {
        CharacterMap boughtMap = saveManager.TryToBuyMap(mapName, characterMapType);
        if (boughtMap != null) OnMapButtonClicked(button, characterMapType, mapName);
    }

    /// <summary>
    /// Updates all inventory text fields
    /// </summary>
    public void UpdateTexts(string name, int price)
    {
        currencyText.text = "Currency: " + saveManager.CurrentGameData.currency;
        mapPriceText.text = "Price: " + price;
        mapNameText.text = "Name: " + name;
    }
    #endregion
}

[System.Serializable]
public struct BodyTypeButtonIcon
{
    public CharacterMapType characterMapType;
    public Sprite icon;
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventoryButton : MonoBehaviour
{
    [Header("UI")]
    [Space(5)]
    public Button button;
    [SerializeField] private Image selectedBackground;
    [SerializeField] private Image icon;
    [SerializeField] private Image lockedIcon;
    [SerializeField] private TMP_Text text;
    private InventoryButtonType inventoryButtonType;
    private string mapNameCached;
    public string mapName { get { return mapNameCached; } }

    #region Public Methods
    /// <summary>
    /// Sets the selected state of the button and updates the visual indicator
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectedBackground != null)
            selectedBackground.gameObject.SetActive(selected);
    }

    /// <summary>
    /// Sets the locked state of the button
    /// </summary>
    public void SetLocked(bool selected)
    {
        lockedIcon.gameObject.SetActive(selected);
    }

    /// <summary>
    /// Sets the text content and visibility of the button label
    /// </summary>
    public void SetText(bool state, string text)
    {
        this.text.gameObject.SetActive(state);
        this.text.text = text;
    }

    /// <summary>
    /// Changes the color of the button's icon
    /// </summary>
    public void SetIconColor(Color color)
    {
        icon.color = color;
    }

    /// <summary>
    /// Changes the sprite of the button's icon
    /// </summary>
    public void SetIconImage(Sprite sprite)
    {
        icon.sprite = sprite;
    }

    /// <summary>
    /// Changes the sprite of the locked state icon
    /// </summary>
    public void SetLockedIconImage(Sprite sprite)
    {
        lockedIcon.sprite = sprite;
    }

    /// <summary>
    /// Stores the map name associated with this button
    /// </summary>
    public void SetCachedMapName(string mapName)
    {
        mapNameCached = mapName;
    }
    #endregion
}

public enum InventoryButtonType
{
    Body,
    Map,
    Color
}
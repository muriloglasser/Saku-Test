using Unity.Netcode;
using UnityEngine;

public class NetworkColorChanger : NetworkBehaviour
{  
    private NetworkVariable<Color> networkColor = new NetworkVariable<Color>();
    private SkinnedMeshRenderer meshRenderer;
    private const string overrideColorPropertie = "_OverrideColor";

    #region Unity Callbacks
    private void Awake()
    {
        meshRenderer = GetComponent<SkinnedMeshRenderer>();
        networkColor.OnValueChanged += OnColorChanged;
    }
    private void Start()
    {
        ApplyCurrentColor();
    }
    #endregion
    #region Color Handling
    /// <summary>
    /// Applies the current network color to the material
    /// </summary>
    private void ApplyCurrentColor()
    {
        if (networkColor.Value != default)
        {
            meshRenderer.material.SetColor(overrideColorPropertie, networkColor.Value);
        }
    }

    /// <summary>
    /// Handles color changes from the network variable
    /// </summary>
    private void OnColorChanged(Color oldColor, Color newColor)
    {
        meshRenderer.material.SetColor(overrideColorPropertie, newColor);
    }
    #endregion
    #region Public Methods
    /// <summary>
    /// Changes the color across the network
    /// Server updates directly, clients use ServerRpc
    /// </summary>
    public void ChangeColor(Color newColor)
    {
        if (IsServer)
        {
            networkColor.Value = newColor;
        }
        else
        {
            ChangeColorServerRpc(newColor);
        }
    }
    #endregion
    #region Server RPC
    /// <summary>
    /// Server RPC to request a color change
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeColorServerRpc(Color newColor)
    {
        networkColor.Value = newColor;
    }
    #endregion
}
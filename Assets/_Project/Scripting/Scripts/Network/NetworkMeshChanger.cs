using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class NetworkMeshChanger : NetworkBehaviour
{  
    private NetworkVariable<FixedString64Bytes> networkMapName = new NetworkVariable<FixedString64Bytes>();
    private SkinnedMeshRenderer meshRenderer;

    #region Unity Callbacks
    private void Awake()
    {
        meshRenderer = GetComponent<SkinnedMeshRenderer>();
        networkMapName.OnValueChanged += OnMapChanged;
    }

    private void Start()
    {
        if (!networkMapName.Value.IsEmpty)
        {
            ApplyMap(networkMapName.Value.ToString());
        }
    }
    #endregion
    #region Map Handling
    /// <summary>
    /// Handles network map name changes and applies the new map
    /// </summary>
    private void OnMapChanged(FixedString64Bytes oldMapName, FixedString64Bytes newMapName)
    {
        ApplyMap(newMapName.ToString());
    }

    /// <summary>
    /// Applies the specified map's mesh and material to the renderer
    /// </summary>
    private void ApplyMap(string mapName)
    {
        if (string.IsNullOrEmpty(mapName)) return;
        
        CharacterMap mapTemp = SaveManager.GetCharacterMap(mapName);
        if (mapTemp != null && mapTemp.mesh != null)
        {
            meshRenderer.sharedMesh = mapTemp.mesh;
            Color lastColor = meshRenderer.material.GetColor("_OverrideColor");
            meshRenderer.material = mapTemp.material;
            meshRenderer.material.SetColor("_OverrideColor", lastColor);
        }
        else
        {
            Debug.LogWarning($"Mapa '{mapName}' não encontrado ou mesh inválido!");
        }
    }
    #endregion
    #region Public Methods
    /// <summary>
    /// Server RPC to change the map for all clients
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeMapServerRpc(FixedString64Bytes newMapName)
    {
        networkMapName.Value = newMapName;
    }

    /// <summary>
    /// Public method to change the mesh, handles both server and client cases
    /// </summary>
    public void ChangeMesh(string newMapName)
    {
        if (string.IsNullOrEmpty(newMapName))
        {
            Debug.LogError("Nome do mapa não pode ser vazio!");
            return;
        }

        var fixedString = new FixedString64Bytes(newMapName);

        if (IsServer)
        {
            networkMapName.Value = fixedString;
        }
        else
        {
            ChangeMapServerRpc(fixedString);
        }
    }
    #endregion
}
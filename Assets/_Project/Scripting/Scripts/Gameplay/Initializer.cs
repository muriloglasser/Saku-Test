using Unity.Netcode;
using UnityEngine;

public class Initializer : MonoBehaviour
{
    public Player player { get { return _player; } }
    private Player _player;

    #region Initialization Methods
    /// <summary>
    /// Initializes the class with a player reference
    /// </summary>
    public virtual void InitializeWithCustomPlayer(Player player)
    {
        this._player = player;
    }

    /// <summary>
    /// Initializes the class with a player and optional initializer reference
    /// </summary>
    public virtual void InitializeWithCustomPlayer(Player player, Initializer initializer = null)
    {
        this._player = player;
    }
    #endregion
    #region Player Validation
    /// <summary>
    /// Checks if the local player is the owner of this instance
    /// </summary>
    protected bool PlayerIsOwner()
    {
        return player.IsOwner;
    }
    #endregion
}
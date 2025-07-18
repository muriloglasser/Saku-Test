using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class NetworkLobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Space(5)]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject inGamePanel;
    [SerializeField] private GameObject loadingIndicator;
    ///
    private string joinCode;
    private const int MaxPlayers = 100;
    private const string joinCodePattern = "^[6789BCDFGHJKLMNPQRTWbcdfghjklmnpqrtw]{6,12}$";

    #region Unity Callbacks
    private void Awake()
    {
        CleanupNetworkCallbacks();
    }

    private async void Start()
    {
        InitializeUI();
        SetupButtonListeners();

        try
        {
            await InitializeUnityServices();
            UpdateStatus("Insert a valid join code or host a new game");
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Error: {e.Message}", true);
            Debug.LogError(e);
        }
    }

    private void OnDestroy()
    {
        CleanupNetworkCallbacks();
    }
    #endregion
    #region Initialization Methods
    /// <summary>
    /// Initializes all UI elements to their default state
    /// </summary>
    private void InitializeUI()
    {
        lobbyPanel.SetActive(true);
        inGamePanel.SetActive(false);
        loadingIndicator.SetActive(false);
        leaveButton.gameObject.SetActive(false);
        joinCodeText.text = "";
        joinCodeInputField.text = "";
    }

    /// <summary>
    /// Sets up all button click listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        leaveButton.onClick.AddListener(ShowLeaveConfirmation);
    }

    /// <summary>
    /// Initializes Unity Services including anonymous authentication
    /// </summary>
    private async Task InitializeUnityServices()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    #endregion
    #region Network Operations
    /// <summary>
    /// Starts a new game as host and creates a relay allocation
    /// </summary>
    private async void StartHost()
    {
        try
        {
            SetLoadingState(true);
            UpdateStatus("");

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);

            NetworkManager.Singleton.OnServerStarted += OnServerStarted;

            if (NetworkManager.Singleton.StartHost())
            {
                joinCodeText.text = $"Join Code: {joinCode}";
                UpdateStatus($"Hosting game with code: {joinCode}");
                SwitchToInGameUI();
            }
            else
            {
                UpdateStatus("Failed to start host", true);
            }
        }
        catch (RelayServiceException e)
        {
            UpdateStatus($"Relay Error: {e.Message}", true);
            Debug.LogError(e);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    /// <summary>
    /// Joins an existing game as client using the provided join code
    /// </summary>
    private async void StartClient()
    {
        string joinCode = joinCodeInputField.text.Trim().ToUpper();

        if (!ValidateJoinCode(joinCode)) return;

        try
        {
            SetLoadingState(true);
            UpdateStatus($"");

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData);

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (NetworkManager.Singleton.StartClient())
            {
                UpdateStatus($"Connected to game: {joinCode}");
                SwitchToInGameUI();
            }
            else
            {
                UpdateStatus("Connection failed", true);
            }
        }
        catch (RelayServiceException e)
        {
            UpdateStatus($"Connection Error: {e.Message}", true);
            Debug.LogError(e);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    /// <summary>
    /// Handles client disconnection events for both host and clients
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == 0 && NetworkManager.Singleton.IsServer)
        {
            NotifyHostDisconnectedClientRpc();
            ShutdownNetwork();
        }
        else if (!NetworkManager.Singleton.IsServer)
        {
            UpdateStatus("Disconnected from host", true);
            ShutdownNetwork();
        }
    }

    /// <summary>
    /// Notifies all clients when host disconnects
    /// </summary>
    [ClientRpc]
    private void NotifyHostDisconnectedClientRpc()
    {
        if (NetworkManager.Singleton.IsHost) return;
        
        UpdateStatus("Host has disconnected - Returning to lobby", true);
        ShutdownNetwork();
    }

    /// <summary>
    /// Cleans up all network callback subscriptions
    /// </summary>
    private void CleanupNetworkCallbacks()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    /// <summary>
    /// Shuts down the network connection and cleans up
    /// </summary>
    private void ShutdownNetwork()
    {
        try
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                CleanupNetworkCallbacks();
            }
            SwitchToLobbyUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error shutting down network: {e}");
            UpdateStatus("Error disconnecting", true);
        }
    }
    #endregion
    #region UI Methods
    /// <summary>
    /// Switches to the in-game UI view
    /// </summary>
    private void SwitchToInGameUI()
    {
        lobbyPanel.SetActive(false);
        inGamePanel.SetActive(true);
        leaveButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Switches to the lobby UI view
    /// </summary>
    private void SwitchToLobbyUI()
    {
        lobbyPanel.SetActive(true);
        inGamePanel.SetActive(false);
        leaveButton.gameObject.SetActive(false);
        joinCodeText.text = "";
        joinCodeInputField.text = "";
        UpdateStatus("Ready to play - enter a code or host a game");
    }

    /// <summary>
    /// Updates the status text with optional error coloring
    /// </summary>
    private void UpdateStatus(string message, bool isError = false)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.white;
        }
    }

    /// <summary>
    /// Sets the loading state for interactive UI elements
    /// </summary>
    private void SetLoadingState(bool isLoading)
    {
        loadingIndicator.SetActive(isLoading);
        hostButton.interactable = !isLoading;
        clientButton.interactable = !isLoading;
        joinCodeInputField.interactable = !isLoading;
    }
    #endregion
    #region Utility Methods
    /// <summary>
    /// Validates the format of a join code
    /// </summary>
    private bool IsValidJoinCode(string code)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(code, joinCodePattern);
    }

    /// <summary>
    /// Validates the join code input before attempting connection
    /// </summary>
    private bool ValidateJoinCode(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            UpdateStatus("Please enter a join code", true);
            return false;
        }

        if (!IsValidJoinCode(joinCode))
        {
            UpdateStatus("Invalid join code format", true);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles the host server started event
    /// </summary>
    private void OnServerStarted()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    /// <summary>
    /// Initiates the game leave process
    /// </summary>
    public void LeaveGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NotifyHostDisconnectedClientRpc();
        }
        
        ShutdownNetwork();
    }

    /// <summary>
    /// Shows confirmation for leaving the game (placeholder)
    /// </summary>
    private void ShowLeaveConfirmation()
    {
        LeaveGame();
    }
    #endregion
}
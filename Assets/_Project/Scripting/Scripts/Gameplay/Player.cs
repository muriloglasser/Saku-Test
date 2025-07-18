using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Player : NetworkBehaviour
{
    [Header("References")]
    [Space(5)]
    public UIJoystick uiJoystick;
    public SaveManager saveManager;
    public CharacterMapResolver characterMapResolver;
    public UICharacterInventory characterInventory;
    [Header("Movement Settings")]
    [Space(5)]
    [SerializeField] private float speed = 5f;
    [Header("Animation Settings")]
    [Space(5)]
    [SerializeField] private Animator animator;
    [SerializeField] private float movementThreshold = 0.1f;
    private readonly NetworkVariable<bool> netIsWalking = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<float> netMoveSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private Vector3 movementDirection;
    private bool isMoving;

    #region Network Methods
    /// <summary>
    /// Called when the player object is spawned on the network
    /// Initializes network variables and sets up callbacks
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            netIsWalking.OnValueChanged += OnIsWalkingChanged;
            netMoveSpeed.OnValueChanged += OnMoveSpeedChanged;
            UpdateAnimator(netIsWalking.Value, netMoveSpeed.Value);
        }

        if (IsOwner)
        {
            saveManager.InitializeWithCustomPlayer(this);
            characterMapResolver.InitializeWithCustomPlayer(this, characterInventory);
            characterInventory.InitializeWithCustomPlayer(this, saveManager);
        }
    }

    /// <summary>
    /// Called when the player object is despawned from the network
    /// Cleans up network variable callbacks
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            netIsWalking.OnValueChanged -= OnIsWalkingChanged;
            netMoveSpeed.OnValueChanged -= OnMoveSpeedChanged;
        }
    }
    #endregion
    #region Update Methods
    private void Update()
    {
        if (!IsOwner || !IsSpawned)
            return;

        if (characterInventory.gameObject.activeSelf)
        {
            UpdateAnimator(false, 0);
            uiJoystick.LockJoyStick(true);
            movementDirection.x = 0;
            movementDirection.z = 0;
            isMoving = false;
            return;
        }

        HandleInput();
        MovePlayer();

        if (isMoving != netIsWalking.Value ||
            Mathf.Abs(movementDirection.magnitude - netMoveSpeed.Value) > 0.01f)
        {
            netIsWalking.Value = isMoving;
            netMoveSpeed.Value = movementDirection.magnitude;
        }
    }

    /// <summary>
    /// Handles player input for movement
    /// </summary>
    private void HandleInput()
    {
        movementDirection = Vector3.zero;        
        uiJoystick.LockJoyStick(false);
        movementDirection.x = uiJoystick.Direction.x;
        movementDirection.z = uiJoystick.Direction.y;

        if (movementDirection.magnitude > 1)
        {
            movementDirection.Normalize();
        }

        isMoving = movementDirection.magnitude > movementThreshold;
    }

    /// <summary>
    /// Moves the player based on input direction
    /// </summary>
    private void MovePlayer()
    {
        if (isMoving)
        {
            transform.position += movementDirection * (speed * Time.deltaTime);

            if (movementDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(movementDirection);
            }
        }
    }
    #endregion
    #region Animation Methods
    /// <summary>
    /// Called when the network walking state changes
    /// Updates the animator with new values
    /// </summary>
    private void OnIsWalkingChanged(bool oldValue, bool newValue)
    {
        UpdateAnimator(newValue, netMoveSpeed.Value);
    }

    /// <summary>
    /// Called when the network move speed changes
    /// Updates the animator with new values
    /// </summary>
    private void OnMoveSpeedChanged(float oldValue, float newValue)
    {
        UpdateAnimator(netIsWalking.Value, newValue);
    }

    /// <summary>
    /// Updates animator parameters with current movement values
    /// </summary>
    private void UpdateAnimator(bool walking, float speed)
    {
        animator.SetBool(IsWalkingHash, walking);
        animator.SetFloat(MoveSpeedHash, speed);
    }
    #endregion
}
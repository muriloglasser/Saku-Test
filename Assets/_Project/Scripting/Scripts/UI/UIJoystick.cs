using UnityEngine;

public class UIJoystick : MonoBehaviour
{
     public Vector2 Direction => direction;
    [Header("Joystick Art")]
    [Space(5)]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    private Vector2 direction;
    private bool isJoystickActive = false;
    private bool lockJoystick = false;

    #region Unity Methods
    private void Start()
    {
        SetJoystickState(false);
    }

    private void Update()
    {
        CheckJoystickInput();
    }
    #endregion
    #region Joystick Control
    /// <summary>
    /// Handles all joystick input logic including touch detection and phase handling
    /// </summary>
    private void CheckJoystickInput()
    {
        if (lockJoystick) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPos = touch.position;
                joystickBackground.position = touchPos;
                SetJoystickState(true);
                SetJoystickPosition(touchPos);
            }
            else if (touch.phase == TouchPhase.Moved && isJoystickActive)
            {
                SetJoystickPosition(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                ResetJoystick();
            }
        }
    }

    /// <summary>
    /// Updates joystick handle position based on touch input and calculates direction
    /// </summary>
    private void SetJoystickPosition(Vector2 touchPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, touchPosition, null, out Vector2 localPoint);

        Vector2 clampedPosition = Vector2.ClampMagnitude(localPoint, joystickBackground.sizeDelta.x * 0.5f);
        joystickHandle.localPosition = clampedPosition;
        direction = clampedPosition;
    }

    /// <summary>
    /// Resets joystick to default state when touch ends
    /// </summary>
    private void ResetJoystick()
    {
        direction = Vector2.zero;
        SetJoystickState(false);
        joystickHandle.localPosition = Vector2.zero;
    }

    /// <summary>
    /// Toggles joystick visibility and active state
    /// </summary>
    private void SetJoystickState(bool state)
    {
        joystickBackground.gameObject.SetActive(state);
        isJoystickActive = state;
    }
    #endregion
    #region Public Methods
    /// <summary>
    /// Locks or unlocks joystick functionality
    /// </summary>
    public void LockJoyStick(bool state)
    {
        if (state)
        {
            direction = Vector2.zero;
            SetJoystickState(false);
        }
        lockJoystick = state;
    }
    #endregion
}
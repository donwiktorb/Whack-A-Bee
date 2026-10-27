
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class WhackInput : MonoBehaviour
{
    public enum DeviceKind
    {
        KeyboardMouse,
        Xbox,
        PlayStation,
        OtherGamepad
    }

    public static WhackInput Instance { get; private set; }

    [SerializeField] private InputActionAsset actions;
    [SerializeField] private TMP_Text deviceLabel;
    [SerializeField] private float cursorSpeed = 850f;
    [SerializeField] private float deadzone = 0.2f;

    private InputActionMap map;
    private InputAction pointer;
    private InputAction stick;
    private InputAction strike;
    private InputAction pause;

    private Vector2 cursorPosition;
    private bool usingGamepad;

    public DeviceKind ActiveDevice { get; private set; }
        = DeviceKind.KeyboardMouse;

    public event Action<DeviceKind> DeviceChanged;

    public Vector2 CursorPosition => cursorPosition;
    public bool UsingGamepad => usingGamepad;

    public bool StrikePressed =>
        strike != null && strike.WasPressedThisFrame();

    public bool PausePressed =>
        pause != null && pause.WasPressedThisFrame();

    private void Awake()
    {
        if (actions == null)
        {
            Debug.LogError("WhackInput: Actions not assigned!");
            enabled = false;
            return;
        }

        map = actions.FindActionMap("WhackABee");

        if (map == null)
        {
            Debug.LogError("WhackInput: Map not found!");
            enabled = false;
            return;
        }

        pointer = map.FindAction("PointerPosition");
        stick = map.FindAction("AimStick");
        strike = map.FindAction("Strike");
        pause = map.FindAction("Pause");

        if (pointer == null || stick == null ||
            strike == null || pause == null)
        {
            Debug.LogError("WhackInput: Missing actions!");
            enabled = false;
            return;
        }

        cursorPosition = new Vector2(
            Screen.width / 2f,
            Screen.height / 2f
        );

        if (Mouse.current != null)
            cursorPosition = Mouse.current.position.ReadValue();

        RefreshDeviceLabel();
    }

    private void OnEnable()
    {
        Instance = this;
        map?.Enable();
    }

    private void OnDisable()
    {
        map?.Disable();

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        bool mouseUsed = mouse != null &&
            (mouse.delta.ReadValue().sqrMagnitude > 9f ||
             mouse.leftButton.wasPressedThisFrame);

        bool keyboardUsed = keyboard != null &&
            keyboard.anyKey.wasPressedThisFrame;

        Vector2 movement = stick.ReadValue<Vector2>();

        bool gamepadAction =
            (strike.WasPressedThisFrame() &&
             strike.activeControl?.device is Gamepad) ||
            (pause.WasPressedThisFrame() &&
             pause.activeControl?.device is Gamepad);

        bool gamepadUsed =
            movement.sqrMagnitude > deadzone * deadzone ||
            gamepadAction;

        if (gamepad != null)
        {
            gamepadUsed |=
                gamepad.dpad.ReadValue().sqrMagnitude > 0.25f ||
                gamepad.buttonSouth.wasPressedThisFrame ||
                gamepad.buttonNorth.wasPressedThisFrame ||
                gamepad.buttonEast.wasPressedThisFrame ||
                gamepad.buttonWest.wasPressedThisFrame;
        }

        if (mouseUsed || keyboardUsed)
        {
            SetActiveDevice(DeviceKind.KeyboardMouse);
        }
        else if (gamepadUsed)
        {
            SetActiveDevice(IdentifyGamepad(gamepad));
        }

        if (usingGamepad)
        {
            cursorPosition += movement *
                cursorSpeed * Time.unscaledDeltaTime;
        }
        else if (mouse != null)
        {
            cursorPosition = pointer.ReadValue<Vector2>();
        }

        cursorPosition.x = Mathf.Clamp(
            cursorPosition.x, 0f, Screen.width
        );

        cursorPosition.y = Mathf.Clamp(
            cursorPosition.y, 0f, Screen.height
        );
    }

    private DeviceKind IdentifyGamepad(Gamepad gamepad)
    {
        if (gamepad == null)
            return DeviceKind.OtherGamepad;

        string info = (
            gamepad.description.manufacturer + " " +
            gamepad.description.product + " " +
            gamepad.layout
        ).ToLowerInvariant();

        if (info.Contains("dualsense") ||
            info.Contains("dualshock") ||
            info.Contains("playstation") ||
            info.Contains("sony") ||
            info.Contains("wireless controller"))
        {
            return DeviceKind.PlayStation;
        }

        if (info.Contains("xbox") ||
            info.Contains("xinput") ||
            info.Contains("microsoft"))
        {
            return DeviceKind.Xbox;
        }

        return DeviceKind.OtherGamepad;
    }

    private void SetActiveDevice(DeviceKind device)
    {
        if (ActiveDevice == device)
            return;

        ActiveDevice = device;
        usingGamepad = device != DeviceKind.KeyboardMouse;

        RefreshDeviceLabel();
        DeviceChanged?.Invoke(device);
    }

    private void RefreshDeviceLabel()
    {
        if (deviceLabel == null)
            return;

        deviceLabel.text = ActiveDevice switch
        {
            DeviceKind.Xbox => "CONTROLLER: XBOX",
            DeviceKind.PlayStation => "CONTROLLER: PLAYSTATION",
            DeviceKind.OtherGamepad => "CONTROLLER: GAMEPAD",
            _ => "MOUSE / KEYBOARD"
        };
    }
}

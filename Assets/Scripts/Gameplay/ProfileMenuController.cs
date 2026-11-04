
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class ProfileMenuController : MonoBehaviour
{
    public static ProfileMenuController Instance { get; private set; }

    [Header("Screens")]
    [SerializeField] private GameObject pressAnyPanel;
    [SerializeField] private GameObject profileSelectPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Profile Selection")]
    [SerializeField] private Transform profileList;
    [SerializeField] private Button profileButtonTemplate;
    [SerializeField] private TMP_InputField newProfileInput;
    [SerializeField] private TMP_Text errorText;

    [Header("Active Profile")]
    [SerializeField] private TMP_Text activeProfileText;
    [SerializeField] private TMP_Text primaryDeviceText;

    [Header("Platform Simulation")]
    [SerializeField] private GameObject offlineWarning;

    [Header("Game")]
    [SerializeField] private RoundManager roundManager;
    
    [Header("Controller Disconnect")]
    [SerializeField] private GameObject controllerDisconnectedWarning;

    private bool waitingForController;
    private bool resumeRoundAfterReconnect;

    public bool WaitingForController => waitingForController;
    private readonly List<Button> spawnedButtons = new();
    private List<LocalProfile> profiles;

    private bool waitingForInput;
    private InputDevice pendingPrimaryDevice;

    public LocalProfile ActiveProfile { get; private set; }
    public InputDevice PrimaryDevice { get; private set; }
    public bool IsLoggedIn => ActiveProfile != null;
    public bool IsNetworkAvailable { get; private set; } = true;

    [Header("Local Player 2")]
    [SerializeField] private GameObject playerTwoPickerPanel;
    [SerializeField] private Transform playerTwoProfileList;
    [SerializeField] private Button playerTwoButtonTemplate;
    [SerializeField] private TMP_Text playerTwoLabel;

    private readonly List<Button> playerTwoButtons = new();

    private InputDevice pendingPlayerTwoDevice;
    private bool selectingPlayerTwo;

    public LocalProfile SecondProfile { get; private set; }
    public InputDevice SecondDevice { get; private set; }
    private void Awake()
    {
        Instance = this;
        profiles = LocalProfileStore.Load();
    }

    private void Start()
    {
        ShowStartup();
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += HandleDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= HandleDeviceChange;
    }

    private void HandleDeviceChange(
        InputDevice device,
        InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            Debug.Log(
                $"[Input] {change}: {device.displayName} " +
                $"(ID: {device.deviceId})"
            );
        }

        bool removed =
            change == InputDeviceChange.Removed ||
            change == InputDeviceChange.Disconnected;

        if (!removed)
            return;

        if (device == SecondDevice)
        {
            Debug.LogWarning("[Input] Player 2 disconnected.");
            ResetSecondPlayer();
            return;
        }


        if (device == pendingPlayerTwoDevice)
        {
            Debug.LogWarning("[Input] Player 2 join cancelled.");
            CancelSecondPlayerJoin();
            return;
        }
        if (!IsLoggedIn && pendingPrimaryDevice == device)
        {
            ShowStartup();
            return;
        }

        if (!IsLoggedIn ||
            PrimaryDevice != device ||
            device is not Gamepad)
        {
            return;
        }

        Debug.LogWarning("[Input] Primary controller disconnected!");

        waitingForController = true;

        resumeRoundAfterReconnect =
            roundManager != null && roundManager.IsPlaying;

        if (resumeRoundAfterReconnect)
            roundManager.PauseRound();

        PrimaryDevice = null;

        WhackInput.Instance?.BlockGameplayInput();

        if (primaryDeviceText != null)
            primaryDeviceText.text = "DEVICE: DISCONNECTED";

        if (controllerDisconnectedWarning != null)
            controllerDisconnectedWarning.SetActive(true);
    }

    private void ReclaimController(Gamepad pad)
    {
        if (!waitingForController || pad == null || !pad.added)
            return;

        PrimaryDevice = pad;
        waitingForController = false;

        WhackInput.Instance?.SetPrimaryDevice(pad);

        if (primaryDeviceText != null)
            primaryDeviceText.text = "DEVICE: " + pad.displayName;

        if (controllerDisconnectedWarning != null)
            controllerDisconnectedWarning.SetActive(false);

        Debug.Log(
            $"[Input] Controller reclaimed: {pad.displayName}"
        );

        if (resumeRoundAfterReconnect && roundManager != null)
            roundManager.ResumeRound();

        resumeRoundAfterReconnect = false;
    }

    private InputDevice DetectSecondPlayerJoinDevice()
    {
        if (PrimaryDevice is Gamepad)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame))
            {
                return keyboard;
            }
        }

        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad == null || !pad.added)
                continue;

            if (pad == PrimaryDevice)
                continue;

            if (pad.buttonSouth.wasPressedThisFrame)
                return pad;
        }

        return null;
    }

    private void BeginSecondPlayerJoin(InputDevice device)
    {
        if (device == null || !IsLoggedIn)
            return;

        if (playerTwoPickerPanel == null ||
            playerTwoProfileList == null ||
            playerTwoButtonTemplate == null)
        {
            Debug.LogError("[Profiles] Player 2 UI is not configured!");
            return;
        }

        pendingPlayerTwoDevice = device;
        selectingPlayerTwo = true;

        foreach (Button button in playerTwoButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
        }

        playerTwoButtons.Clear();

        GameObject firstButton = null;

        foreach (LocalProfile profile in profiles)
        {
            if (profile.id == ActiveProfile.id)
                continue;

            LocalProfile selected = profile;

            Button button = Instantiate(
                playerTwoButtonTemplate,
                playerTwoProfileList
            );

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.text = profile.displayName;

            button.onClick.AddListener(
                () => SelectSecondPlayerProfile(selected)
            );

            button.gameObject.SetActive(true);
            playerTwoButtons.Add(button);

            if (firstButton == null)
                firstButton = button.gameObject;
        }

        playerTwoPickerPanel.SetActive(true);

        if (EventSystem.current != null && firstButton != null)
            EventSystem.current.SetSelectedGameObject(firstButton);

        Debug.Log("[Profiles] Second device requested to join: " +
                device.displayName);
    }

    public void SelectSecondPlayerProfile(LocalProfile profile)
    {
        if (!IsLoggedIn ||
            !selectingPlayerTwo ||
            pendingPlayerTwoDevice == null ||
            profile == null ||
            profile.id == ActiveProfile.id)
        {
            return;
        }

        if (!pendingPlayerTwoDevice.added)
        {
            CancelSecondPlayerJoin();
            return;
        }

        SecondProfile = profile;
        SecondDevice = pendingPlayerTwoDevice;

        CancelSecondPlayerJoin();

        if (playerTwoLabel != null)
        {
            playerTwoLabel.text =
                $"P2: {profile.displayName} ({SecondDevice.displayName})";

            playerTwoLabel.gameObject.SetActive(true);
        }

        Debug.Log(
            $"[Profiles] Player 2 joined: {profile.displayName} " +
            $"using {SecondDevice.displayName}"
        );
    }

    public void CancelSecondPlayerJoin()
    {
        selectingPlayerTwo = false;
        pendingPlayerTwoDevice = null;

        if (playerTwoPickerPanel != null)
            playerTwoPickerPanel.SetActive(false);
    }

    private void ResetSecondPlayer()
    {
        CancelSecondPlayerJoin();

        SecondProfile = null;
        SecondDevice = null;

        if (playerTwoLabel != null)
            playerTwoLabel.gameObject.SetActive(false);
    }

   private void Update()
    {
        if (waitingForInput)
        {
            InputDevice device = DetectStartDevice();

            if (device != null)
            {
                pendingPrimaryDevice = device;
                ShowProfileSelection();
            }

            return;
        }

        if (!IsLoggedIn)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.f8Key.wasPressedThisFrame)
            {
                SimulateLogout();
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                SetNetworkAvailable(!IsNetworkAvailable);
            }
        }

        if (waitingForController)
        {
            foreach (Gamepad pad in Gamepad.all)
            {
                if (pad == null || !pad.added)
                    continue;

                if (pad == SecondDevice)
                    continue;

                if (pad.buttonSouth.wasPressedThisFrame)
                {
                    ReclaimController(pad);
                    break;
                }
            }

            return;
        }

        if (selectingPlayerTwo || SecondProfile != null)
            return;

        InputDevice secondDevice = DetectSecondPlayerJoinDevice();

        if (secondDevice != null)
            BeginSecondPlayerJoin(secondDevice);
    }

    private static InputDevice DetectStartDevice()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            keyboard.anyKey.wasPressedThisFrame)
        {
            return keyboard;
        }

        Mouse mouse = Mouse.current;

        if (mouse != null &&
            (mouse.leftButton.wasPressedThisFrame ||
             mouse.rightButton.wasPressedThisFrame))
        {
            return mouse;
        }

        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad.buttonSouth.wasPressedThisFrame ||
                pad.buttonNorth.wasPressedThisFrame ||
                pad.buttonEast.wasPressedThisFrame ||
                pad.buttonWest.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame)
            {
                return pad;
            }
        }

        return null;
    }

    public void ShowStartup()
    {
        waitingForController = false;
        resumeRoundAfterReconnect = false;

        if (controllerDisconnectedWarning != null)
            controllerDisconnectedWarning.SetActive(false);
        waitingForInput = true;
        ResetSecondPlayer();
        ActiveProfile = null;
        PrimaryDevice = null;
        pendingPrimaryDevice = null;

        WhackInput.Instance?.SetPrimaryDevice(null);

        SetNetworkAvailable(true);

        pressAnyPanel.SetActive(true);
        profileSelectPanel.SetActive(false);
        mainMenuPanel.SetActive(false);

        if (activeProfileText != null)
            activeProfileText.gameObject.SetActive(false);

        if (primaryDeviceText != null)
            primaryDeviceText.gameObject.SetActive(false);
        
    }

    public void ShowProfileSelection()
    {
        waitingForInput = false;

        pressAnyPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
        profileSelectPanel.SetActive(true);

        if (errorText != null)
            errorText.text = "";

        RefreshProfiles();
    }

    private void RefreshProfiles()
    {
        foreach (Button button in spawnedButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        spawnedButtons.Clear();

        GameObject firstButton = null;

        foreach (LocalProfile profile in profiles)
        {
            LocalProfile selectedProfile = profile;

            Button button = Instantiate(
                profileButtonTemplate,
                profileList
            );

            button.gameObject.SetActive(true);

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.text = profile.displayName;

            button.onClick.AddListener(
                () => Login(selectedProfile)
            );

            spawnedButtons.Add(button);

            if (firstButton == null)
                firstButton = button.gameObject;
        }

        if (EventSystem.current != null && firstButton != null)
            EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void CreateProfile()
    {
        if (newProfileInput == null)
            return;

        bool created = LocalProfileStore.TryCreate(
            profiles,
            newProfileInput.text,
            out string error
        );

        if (!created)
        {
            if (errorText != null)
                errorText.text = error;

            return;
        }

        newProfileInput.text = "";

        if (errorText != null)
            errorText.text = "";

        RefreshProfiles();
    }

    public void Login(LocalProfile profile)
    {
        if (profile == null)
            return;
        ResetSecondPlayer();
        ActiveProfile = profile;
        waitingForInput = false;

        PrimaryDevice = pendingPrimaryDevice;

        if (PrimaryDevice == null)
        {
            if (Keyboard.current != null)
                PrimaryDevice = Keyboard.current;
            else if (Mouse.current != null)
                PrimaryDevice = Mouse.current;
            else
                PrimaryDevice = Gamepad.current;
        }

        pendingPrimaryDevice = null;

        WhackInput.Instance?.SetPrimaryDevice(PrimaryDevice);

        if (roundManager != null)
        {
            roundManager.ReturnToMenu();
            roundManager.LoadProfileScore(profile.id);
        }

        pressAnyPanel.SetActive(false);
        profileSelectPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        if (activeProfileText != null)
        {
            activeProfileText.gameObject.SetActive(true);
            activeProfileText.text = "PLAYER: " + profile.displayName;
        }

        if (primaryDeviceText != null)
        {
            primaryDeviceText.gameObject.SetActive(true);

            string deviceName =
                PrimaryDevice is Keyboard || PrimaryDevice is Mouse
                    ? "KEYBOARD + MOUSE"
                    : PrimaryDevice != null
                        ? PrimaryDevice.displayName
                        : "UNKNOWN";

            primaryDeviceText.text = "DEVICE: " + deviceName;
        }

        Debug.Log(
            $"[Profiles] {profile.displayName} logged in using " +
            $"{PrimaryDevice?.displayName ?? "Unknown"}"
        );
    }

    public void SwitchUser()
    {
        waitingForController = false;
        resumeRoundAfterReconnect = false;

        if (controllerDisconnectedWarning != null)
            controllerDisconnectedWarning.SetActive(false);
        StopCurrentActivity();
        ResetSecondPlayer();
        pendingPrimaryDevice = PrimaryDevice;

        ActiveProfile = null;
        PrimaryDevice = null;

        WhackInput.Instance?.SetPrimaryDevice(null);

        if (activeProfileText != null)
            activeProfileText.gameObject.SetActive(false);

        if (primaryDeviceText != null)
            primaryDeviceText.gameObject.SetActive(false);

        ShowProfileSelection();
    }

    public void SimulateLogout()
    {
        Debug.Log("[Platform] Primary user signed out (F8)");

        StopCurrentActivity();
        ShowStartup();
    }

    private void SetNetworkAvailable(bool available)
    {
        IsNetworkAvailable = available;

        if (offlineWarning != null)
            offlineWarning.SetActive(!available);

        Debug.Log(available
            ? "[Platform] Connection restored"
            : "[Platform] Connection lost (simulated)");
    }

    private void StopCurrentActivity()
    {
        if (roundManager != null)
            roundManager.ReturnToMenu();

        LANMenuController lan =
            FindFirstObjectByType<LANMenuController>();

        if (lan != null)
            lan.LeaveLobby();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

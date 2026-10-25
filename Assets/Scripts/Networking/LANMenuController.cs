
using Mirror;
using Mirror.Discovery;
using TMPro;
using UnityEngine;

public class LANMenuController : MonoBehaviour
{
    [Header("Networking")]
    [SerializeField] private NetworkDiscovery discovery;

    [Header("Menus")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lanMenuPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Status")]
    [SerializeField] private TMP_Text connectionStatusText;

    private bool searching;
    private bool joining;
    private bool lobbySession;

    private void OnEnable()
    {
        if (discovery != null)
            discovery.OnServerFound.AddListener(OnServerFound);
    }

    private void OnDisable()
    {
        if (discovery != null)
            discovery.OnServerFound.RemoveListener(OnServerFound);
    }

    public void OpenLanMenu()
    {
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        lanMenuPanel.SetActive(true);

        SetStatus("NOT CONNECTED");
    }

    public void HostGame()
    {
        if (NetworkServer.active || NetworkClient.active)
            return;

        if (discovery == null || NetworkManager.singleton == null)
            return;

        searching = false;
        joining = false;

        discovery.StopDiscovery();

        NetworkManager.singleton.StartHost();

        if (NetworkServer.active)
        {
            discovery.AdvertiseServer();
            ShowLobby();
        }
        else
        {
            SetStatus("FAILED TO HOST");
        }
    }

    public void FindGame()
    {
        if (NetworkServer.active || NetworkClient.active)
            return;

        if (discovery == null || NetworkManager.singleton == null)
            return;

        searching = true;
        joining = false;

        SetStatus("SEARCHING FOR HOST...");
        discovery.StartDiscovery();
    }

    private void OnServerFound(ServerResponse response)
    {
        if (!searching ||
            NetworkServer.active ||
            NetworkClient.active)
            return;

        searching = false;
        joining = true;

        discovery.StopDiscovery();

        SetStatus("CONNECTING...");
        NetworkManager.singleton.StartClient(response.uri);
    }

    private void Update()
    {
        if (joining)
        {
            if (NetworkClient.isConnected)
            {
                joining = false;
                ShowLobby();
            }
            else if (!NetworkClient.active)
            {
                joining = false;
                SetStatus("CONNECTION FAILED");
            }
        }

        if (lobbySession &&
            !NetworkServer.active &&
            !NetworkClient.active)
        {
            lobbySession = false;

            lobbyPanel.SetActive(false);
            lanMenuPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
    }

    private void ShowLobby()
    {
        lobbySession = true;

        mainMenuPanel.SetActive(false);
        lanMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public void Back()
    {
        LeaveLobby();
    }

    public void LeaveLobby()
    {
        searching = false;
        joining = false;
        lobbySession = false;

        if (discovery != null)
            discovery.StopDiscovery();

        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active)
                NetworkManager.singleton.StopHost();
            else if (NetworkClient.active)
                NetworkManager.singleton.StopClient();
        }

        lobbyPanel.SetActive(false);
        lanMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        SetStatus("NOT CONNECTED");
    }

    private void SetStatus(string message)
    {
        if (connectionStatusText != null)
            connectionStatusText.text = message;
    }
}


using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LANLobbyController : MonoBehaviour
{
    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TMP_Text player1Text;
    [SerializeField] private TMP_Text player2Text;
    [SerializeField] private TMP_Text lobbyStatusText;

    [Header("Ready Button")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;

    public void ToggleReady()
    {
        LANPlayer local = GetLocalPlayer();

        if (local != null)
            local.ToggleReady();
    }

    private LANPlayer GetLocalPlayer()
    {
        if (NetworkClient.localPlayer == null)
            return null;

        return NetworkClient.localPlayer
            .GetComponent<LANPlayer>();
    }

    private void Update()
    {
        if (lobbyPanel == null || !lobbyPanel.activeInHierarchy)
            return;

        LANPlayer host = null;
        LANPlayer client = null;

        foreach (NetworkIdentity identity
                 in NetworkClient.spawned.Values)
        {
            if (identity == null)
                continue;

            LANPlayer player = identity.GetComponent<LANPlayer>();

            if (player == null)
                continue;

            if (player.IsHostPlayer)
                host = player;
            else
                client = player;
        }

        UpdatePlayerText(player1Text, host, "PLAYER 1 (HOST)");
        UpdatePlayerText(player2Text, client, "PLAYER 2 (CLIENT)");

        LANPlayer localPlayer = GetLocalPlayer();

        if (readyButton != null)
            readyButton.interactable = localPlayer != null;

        if (readyButtonText != null)
        {
            readyButtonText.text =
                localPlayer != null && localPlayer.IsReady
                    ? "CANCEL READY"
                    : "READY";
        }

        if (lobbyStatusText == null)
            return;

        if (host == null)
        {
            lobbyStatusText.text = "CONNECTING...";
        }
        else if (client == null)
        {
            lobbyStatusText.text = "WAITING FOR PLAYER 2...";
        }
        else if (host.IsReady && client.IsReady)
        {
            lobbyStatusText.text = "BOTH PLAYERS READY!";
        }
        else
        {
            lobbyStatusText.text = "WAITING FOR BOTH PLAYERS...";
        }
    }

    private void UpdatePlayerText(
        TMP_Text text,
        LANPlayer player,
        string playerName)
    {
        if (text == null)
            return;

        if (player == null)
        {
            text.text = $"{playerName} - NOT CONNECTED";
            return;
        }

        string status = player.IsReady
            ? "READY"
            : "NOT READY";

        string you = player.isLocalPlayer ? " (YOU)" : "";

        text.text = $"{playerName}{you} - {status}";
    }
}

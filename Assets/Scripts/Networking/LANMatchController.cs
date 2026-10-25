
using System;
using Mirror;
using TMPro;
using UnityEngine;

public class LANMatchController : NetworkBehaviour
{
    private enum MatchPhase
    {
        Lobby,
        Countdown,
        Playing,
        Results
    }

    [Header("Round Settings")]
    [SerializeField] private float roundDuration = 60f;

    [Header("Local UI")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text opponentScoreText;

    [Header("LAN Results")]
    [SerializeField] private GameObject lanResultsPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultMyScoreText;
    [SerializeField] private TMP_Text resultRivalScoreText;
    [SerializeField] private TMP_Text rematchStatusText;
    [SerializeField] private TMP_Text rematchButtonText;

    [Header("Other References")]
    [SerializeField] private GameObject hammerVisual;
    [SerializeField] private LANMenuController lanMenuController;

    [SyncVar] private MatchPhase phase = MatchPhase.Lobby;
    [SyncVar] private double startTime;
    [SyncVar] private double endTime;
    [SyncVar] private int matchSeed;

    [SyncVar] private int finalHostScore;
    [SyncVar] private int finalClientScore;

    public const double SpawnIntervalSeconds = 1.2;

    public bool IsMatchPlaying => phase == MatchPhase.Playing;
    public bool CanChangeReady =>
        phase == MatchPhase.Lobby || phase == MatchPhase.Results;

    public int MatchSeed => matchSeed;
    public double MatchStartTime => startTime;
    public double MatchEndTime => endTime;

    private void Update()
    {
        if (isServer)
            ServerTick();

        if (isClient && NetworkClient.isConnected)
            UpdateClientUI();
    }

    [Server]
    private void ServerTick()
    {
        bool haveTwoPlayers = TryGetPlayers(
            out LANPlayer host,
            out LANPlayer client
        );

        if (!haveTwoPlayers)
        {
            if (phase != MatchPhase.Lobby)
                ResetToLobby();

            return;
        }

        if (phase == MatchPhase.Lobby ||
            phase == MatchPhase.Results)
        {
            if (host.IsReady && client.IsReady)
                BeginNetworkMatch(host, client);

            return;
        }

        double now = NetworkTime.time;

        if (phase == MatchPhase.Countdown &&
            now >= startTime)
        {
            phase = MatchPhase.Playing;
            Debug.Log("[LAN] Match started!");
        }

        if (phase == MatchPhase.Playing &&
            now >= endTime)
        {
            FinishNetworkMatch(host, client);
        }
    }

    [Server]
    private void BeginNetworkMatch(
        LANPlayer host,
        LANPlayer client)
    {
        host.ServerResetMatchScore();
        client.ServerResetMatchScore();

        host.ServerResetReady();
        client.ServerResetReady();

        matchSeed = UnityEngine.Random.Range(
            1, int.MaxValue
        );

        startTime = NetworkTime.time + 3.0;
        endTime = startTime + Math.Max(1f, roundDuration);

        phase = MatchPhase.Countdown;

        Debug.Log("[LAN] Countdown started!");
    }

    [Server]
    private void FinishNetworkMatch(
        LANPlayer host,
        LANPlayer client)
    {
        finalHostScore = host.MatchScore;
        finalClientScore = client.MatchScore;

        host.ServerResetReady();
        client.ServerResetReady();

        phase = MatchPhase.Results;

        Debug.Log(
            $"[LAN] Match finished: " +
            $"{finalHostScore} vs {finalClientScore}"
        );
    }

    [Server]
    private bool TryGetPlayers(
        out LANPlayer host,
        out LANPlayer client)
    {
        host = null;
        client = null;

        if (NetworkServer.connections.Count != 2)
            return false;

        foreach (NetworkConnectionToClient connection
                 in NetworkServer.connections.Values)
        {
            if (connection == null ||
                connection.identity == null)
                return false;

            LANPlayer player =
                connection.identity.GetComponent<LANPlayer>();

            if (player == null)
                return false;

            if (player.IsHostPlayer)
                host = player;
            else
                client = player;
        }

        return host != null && client != null;
    }

    [Server]
    private void ResetToLobby()
    {
        foreach (NetworkConnectionToClient connection
                 in NetworkServer.connections.Values)
        {
            if (connection?.identity == null)
                continue;

            LANPlayer player =
                connection.identity.GetComponent<LANPlayer>();

            if (player != null)
                player.ServerResetReady();
        }

        phase = MatchPhase.Lobby;
        startTime = 0;
        endTime = 0;

        Debug.Log("[LAN] Returned to lobby.");
    }

    private LANPlayer GetLocalPlayer()
    {
        if (NetworkClient.localPlayer == null)
            return null;

        return NetworkClient.localPlayer
            .GetComponent<LANPlayer>();
    }

    private LANPlayer GetOpponent(LANPlayer local)
    {
        foreach (NetworkIdentity identity
                 in NetworkClient.spawned.Values)
        {
            if (identity == null)
                continue;

            LANPlayer player =
                identity.GetComponent<LANPlayer>();

            if (player != null && player != local)
                return player;
        }

        return null;
    }

    private void UpdateClientUI()
    {
        bool inLobby = phase == MatchPhase.Lobby;
        bool inRound = phase == MatchPhase.Countdown ||
                       phase == MatchPhase.Playing;
        bool inResults = phase == MatchPhase.Results;

        SetVisible(lobbyPanel, inLobby);
        SetVisible(hudPanel, inRound);
        SetVisible(lanResultsPanel, inResults);
        SetVisible(hammerVisual, phase == MatchPhase.Playing);

        if (opponentScoreText != null)
            SetVisible(opponentScoreText.gameObject, inRound);

        if (inRound)
            UpdateRoundHUD();

        if (inResults)
            UpdateResultsUI();
    }

    private void UpdateRoundHUD()
    {
        LANPlayer local = GetLocalPlayer();
        LANPlayer rival = GetOpponent(local);

        if (scoreText != null)
            scoreText.text =
                $"SCORE: {(local != null ? local.MatchScore : 0):N0}";

        if (opponentScoreText != null)
            opponentScoreText.text =
                $"RIVAL: {(rival != null ? rival.MatchScore : 0):N0}";

        if (timeText == null)
            return;

        if (phase == MatchPhase.Countdown)
        {
            int seconds = Math.Max(1,
                (int)Math.Ceiling(startTime - NetworkTime.time));

            timeText.text = $"START IN: {seconds}";
        }
        else
        {
            int seconds = Math.Max(0,
                (int)Math.Ceiling(endTime - NetworkTime.time));

            timeText.text = $"TIME: {seconds}";
        }
    }

    private void UpdateResultsUI()
    {
        LANPlayer local = GetLocalPlayer();
        LANPlayer rival = GetOpponent(local);

        bool amHost = local != null && local.IsHostPlayer;

        int myScore = amHost ? finalHostScore : finalClientScore;
        int rivalScore = amHost ? finalClientScore : finalHostScore;

        if (resultTitleText != null)
        {
            resultTitleText.text = myScore > rivalScore
                ? "YOU WIN!"
                : myScore < rivalScore
                    ? "YOU LOSE!"
                    : "DRAW!";
        }

        if (resultMyScoreText != null)
            resultMyScoreText.text = $"YOUR SCORE: {myScore:N0}";

        if (resultRivalScoreText != null)
            resultRivalScoreText.text = $"RIVAL: {rivalScore:N0}";

        bool myReady = local != null && local.IsReady;
        bool rivalReady = rival != null && rival.IsReady;

        if (rematchButtonText != null)
            rematchButtonText.text = myReady
                ? "CANCEL REMATCH"
                : "REMATCH";

        if (rematchStatusText == null)
            return;

        if (myReady && rivalReady)
            rematchStatusText.text = "STARTING REMATCH!";
        else if (myReady)
            rematchStatusText.text = "WAITING FOR OPPONENT...";
        else if (rivalReady)
            rematchStatusText.text = "OPPONENT WANTS A REMATCH";
        else
            rematchStatusText.text = "PRESS REMATCH TO PLAY AGAIN";
    }

    public void RequestRematch()
    {
        if (!isClient || phase != MatchPhase.Results)
            return;

        LANPlayer local = GetLocalPlayer();

        if (local != null)
            local.ToggleReady();
    }

    public void LeaveMatch()
    {
        SetVisible(lanResultsPanel, false);

        if (lanMenuController != null)
            lanMenuController.LeaveLobby();
    }

    private static void SetVisible(GameObject obj, bool visible)
    {
        if (obj != null && obj.activeSelf != visible)
            obj.SetActive(visible);
    }

    public override void OnStopClient()
    {
        SetVisible(hudPanel, false);
        SetVisible(lanResultsPanel, false);
        SetVisible(hammerVisual, false);

        if (opponentScoreText != null)
            opponentScoreText.gameObject.SetActive(false);
    }

    public static uint BeatHash(int seed, int beat)
    {
        unchecked
        {
            uint x = (uint)seed ^
                     ((uint)beat * 0x9E3779B9u);

            x ^= x >> 16;
            x *= 0x7FEB352Du;
            x ^= x >> 15;
            x *= 0x846CA68Bu;
            x ^= x >> 16;

            return x;
        }
    }

    [Server]
    public bool ServerValidateHit(
        int submittedSeed,
        int beat,
        out int points)
    {
        points = 0;

        if (phase != MatchPhase.Playing ||
            submittedSeed != matchSeed ||
            beat < 0)
            return false;

        double scheduledTime =
            startTime + beat * SpawnIntervalSeconds;

        double now = NetworkTime.time;

        if (scheduledTime >= endTime ||
            now >= endTime)
            return false;

        if (now < scheduledTime + 0.15 ||
            now > scheduledTime + 1.65)
            return false;

        uint value = BeatHash(matchSeed, beat);
        bool isBee = ((value >> 8) % 100u) < 20u;

        points = isBee ? -200 : 100;
        return true;
    }
}

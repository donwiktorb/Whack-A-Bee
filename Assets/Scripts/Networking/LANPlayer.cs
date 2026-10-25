
using Mirror;
using UnityEngine;

using System.Collections.Generic;

public class LANPlayer : NetworkBehaviour
{
    [SyncVar]
    private bool readyState;

    [SyncVar]
    private bool hostPlayer;
    [SyncVar]
    private int matchScore;

    private readonly HashSet<int> acceptedHits = new();

    public int MatchScore => matchScore;

    public bool IsReady => readyState;
    public bool IsHostPlayer => hostPlayer;

    public override void OnStartServer()
    {
        // host player is the player which is host - me
        hostPlayer =
            connectionToClient == NetworkServer.localConnection;

        readyState = false;
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("[LAN] Local player spawned!");
    }

    public void ToggleReady()
    {
        if (!isLocalPlayer)
            return;

        CmdSetReady(!readyState);
    }

    [Command]
    private void CmdSetReady(bool newReadyState)
    {
        LANMatchController match = FindFirstObjectByType<LANMatchController>();

        if (match == null || !match.CanChangeReady)
            return;
            
        readyState = newReadyState;

        Debug.Log(
            $"[LAN] Player {netId} ready: {readyState}"
        );
    }


    [Server]
    public void ServerResetReady()
    {
        readyState = false;
    }


    [Server]
    public void ServerResetMatchScore()
    {
        matchScore = 0;
        acceptedHits.Clear();
    }

    public void RequestLANHit(int seed, int beat)
    {
        if (!isLocalPlayer)
            return;

        CmdRegisterLANHit(seed, beat);
    }

    [Command]
    private void CmdRegisterLANHit(int seed, int beat)
    {
        if (acceptedHits.Contains(beat))
            return;

        LANMatchController match =
            FindFirstObjectByType<LANMatchController>();

        if (match == null ||
            !match.ServerValidateHit(seed, beat, out int points))
            return;

        acceptedHits.Add(beat);
        matchScore = Mathf.Max(0, matchScore + points);
    }

}

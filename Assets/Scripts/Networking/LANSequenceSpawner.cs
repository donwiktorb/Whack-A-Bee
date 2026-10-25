
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LANSequenceSpawner : MonoBehaviour
{
    [Header("Network Match")]
    [SerializeField] private LANMatchController match;

    [Header("Board")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Targets")]
    [SerializeField] private WhackTarget monsterPrefab;
    [SerializeField] private WhackTarget beePrefab;

    [Header("Settings")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private float targetScale = 0.65f;

    private readonly List<WhackTarget> spawned = new();

    private bool wasPlaying;
    private int nextBeat;
    private int previousHole;

    private void Update()
    {
        bool playing = match != null &&
                       match.IsMatchPlaying &&
                       NetworkClient.isConnected;

        if (!playing)
        {
            if (wasPlaying)
                ClearTargets();

            wasPlaying = false;
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0 ||
            monsterPrefab == null || beePrefab == null)
            return;

        if (!wasPlaying)
        {
            ClearTargets();

            nextBeat = 0;
            previousHole = match.MatchSeed % spawnPoints.Length;
            wasPlaying = true;
        }

        double interval = LANMatchController.SpawnIntervalSeconds;
        double now = NetworkTime.time;

        while (match.MatchStartTime + nextBeat * interval <= now &&
               match.MatchStartTime + nextBeat * interval <
               match.MatchEndTime)
        {
            double scheduledTime =
                match.MatchStartTime + nextBeat * interval;

            uint value =
                LANMatchController.BeatHash(match.MatchSeed, nextBeat);

            int offset = 1 + (int)(value %
                (uint)Mathf.Max(1, spawnPoints.Length - 1));

            int hole = (previousHole + offset) %
                       spawnPoints.Length;

            previousHole = hole;

            bool isBee = ((value >> 8) % 100u) < 20u;

            if (now - scheduledTime < 0.5)
                Spawn(hole, isBee, nextBeat);

            nextBeat++;
        }
    }

    private void Spawn(int hole, bool isBee, int beat)
    {
        if (spawnPoints[hole] == null)
            return;

        WhackTarget prefab = isBee
            ? beePrefab
            : monsterPrefab;

        WhackTarget target = Instantiate(
            prefab,
            spawnPoints[hole],
            false
        );

        target.transform.localPosition = Vector3.zero;
        target.transform.localRotation = Quaternion.identity;
        target.transform.localScale =
            Vector3.one * targetScale;

        target.gameObject.SetActive(true);
        target.SetMultiplayerBeat(beat);
        spawned.Add(target);
        target.Begin();
    }

    private static uint Hash(int seed, int beat)
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

    private void ClearTargets()
    {
        foreach (WhackTarget target in spawned)
        {
            if (target != null)
                Destroy(target.gameObject);
        }

        spawned.Clear();
    }

    private void OnDisable()
    {
        ClearTargets();
        wasPlaying = false;
    }
}

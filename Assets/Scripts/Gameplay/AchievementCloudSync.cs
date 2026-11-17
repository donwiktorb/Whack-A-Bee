
using System;
using TMPro;
using UnityEngine;

public class AchievementCloudSync : MonoBehaviour
{
    public static AchievementCloudSync Instance { get; private set; }

    [SerializeField] private TMP_Text statusText;

    private const string CloudKey = "achievements_v1";

    private string activeUserId;
    private int generation;
    private int cloudVersion;
    private int conflictRetries;

    private bool busy;
    private bool queued;

    private void Awake()
    {
        Instance = this;
    }

    public void SelectProfile(LocalProfile profile)
    {
        EndSession();

        if (profile == null)
            return;

        activeUserId = profile.id;
        Synchronize();
    }

    public void EndSession()
    {
        generation++;
        activeUserId = null;
        cloudVersion = 0;
        conflictRetries = 0;
        busy = false;
        queued = false;
    }

    public void OnNetworkChanged(bool available)
    {
        if (available)
        {
            conflictRetries = 0;
            Synchronize();
        }
        else
        {
            SetStatus("OFFLINE - ACHIEVEMENTS SAVED LOCALLY");
        }
    }

    [ContextMenu("Achievements / Synchronize Now")]
    public void Synchronize()
    {
        AchievementManager manager = AchievementManager.Instance;

        if (string.IsNullOrEmpty(activeUserId) ||
            manager == null || !manager.HasUser)
            return;

        if (CloudSave.SimulateOffline)
        {
            SetStatus("OFFLINE - SYNC PENDING");
            return;
        }

        if (string.IsNullOrEmpty(CloudSave.Space))
        {
            SetStatus("CLOUD SPACE NOT CONFIGURED");
            return;
        }

        if (busy)
        {
            queued = true;
            return;
        }

        busy = true;

        int requestGeneration = generation;
        string user = activeUserId;

        CloudSave.Load(user, CloudKey, result =>
        {
            if (requestGeneration != generation)
                return;

            if (!result.ok)
            {
                busy = false;
                SetStatus("LOAD FAILED: " + result.error);
                return;
            }

            cloudVersion = result.found ? result.version : 0;

            AchievementSaveData remote = new AchievementSaveData();

            if (result.found)
            {
                if (!TryParse(result.value, out remote))
                {
                    busy = false;
                    SetStatus("INVALID CLOUD DATA");
                    return;
                }

                manager.MergeCloud(remote);
            }

            AchievementSaveData local = manager.CreateSnapshot();

            if (local.entries.Count == 0 ||
                RemoteCoversLocal(remote, local))
            {
                busy = false;
                conflictRetries = 0;

                SetStatus("SYNCED - VERSION " + cloudVersion);
                RunQueued();
                return;
            }

            Upload(local, requestGeneration);
        });
    }

    private void Upload(
        AchievementSaveData snapshot,
        int requestGeneration)
    {
        string json = JsonUtility.ToJson(snapshot);
        string user = activeUserId;
        int expectedVersion = cloudVersion;

        CloudSave.Save(
            user,
            CloudKey,
            json,
            result =>
            {
                if (requestGeneration != generation)
                    return;

                busy = false;

                if (result.ok)
                {
                    cloudVersion = result.version;
                    conflictRetries = 0;

                    SetStatus(
                        "SYNCED - VERSION " + result.version
                    );

                    AchievementManager manager =
                        AchievementManager.Instance;

                    string current = manager != null
                        ? JsonUtility.ToJson(manager.CreateSnapshot())
                        : json;

                    if (current != json || queued)
                    {
                        queued = false;
                        Synchronize();
                    }

                    return;
                }

                if (result.IsConflict)
                {
                    if (!TryParse(result.value, out var remote))
                    {
                        SetStatus("INVALID CONFLICT DATA");
                        return;
                    }

                    AchievementManager.Instance?.MergeCloud(remote);

                    conflictRetries++;

                    if (conflictRetries <= 4)
                    {
                        SetStatus("CONFLICT - MERGING");
                        Synchronize();
                    }
                    else
                    {
                        SetStatus("SYNC RETRY REQUIRED");
                    }

                    return;
                }

                SetStatus("UPLOAD PENDING: " + result.error);
            },
            expectedVersion
        );
    }

    private static bool TryParse(
        string json,
        out AchievementSaveData data)
    {
        data = null;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            data = JsonUtility.FromJson<AchievementSaveData>(json);

            return data != null &&
                   data.entries != null &&
                   data.entries.Count <= 1000;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool RemoteCoversLocal(
        AchievementSaveData remote,
        AchievementSaveData local)
    {
        foreach (var entry in local.entries)
        {
            int remoteProgress = 0;
            bool remoteUnlocked = false;

            foreach (var other in remote.entries)
            {
                if (other == null || other.id != entry.id)
                    continue;

                remoteProgress = Mathf.Max(
                    remoteProgress, other.current
                );

                remoteUnlocked |= other.unlocked;
            }

            if (entry.current > remoteProgress ||
                (entry.unlocked && !remoteUnlocked))
            {
                return false;
            }
        }

        return true;
    }

    private void RunQueued()
    {
        if (!queued || busy)
            return;

        queued = false;
        Synchronize();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[AchievementCloud] " + message);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            EndSession();
            Instance = null;
        }
    }
}

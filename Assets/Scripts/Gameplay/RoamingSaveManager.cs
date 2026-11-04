
using System;
using UnityEngine;

public class RoamingSaveManager : MonoBehaviour
{
    public static RoamingSaveManager Instance { get; private set; }

    [Header("DSW Cloud")]
    [SerializeField] private string cloudSpace = "";
    [SerializeField] private TMPro.TMP_Text statusText;

    private const string CloudKey = "solo_best_v1";
    private const string LocalPrefix = "WhackABee.Roaming.v1.";

    [Serializable]
    private class LocalData
    {
        public bool exists;
        public int bestScore;
        public int baseVersion;
        public bool pending;
        public long savedUtcTicks;
    }

    [Serializable]
    private class CloudData
    {
        public int schema = 1;
        public int bestScore;
    }

    private string activeUserId;
    private LocalData local;

    private int generation;
    private int revision;
    private bool busy;
    private bool queued;

    public bool HasConflict { get; private set; }
    public int ConflictCloudScore { get; private set; }
    public int ConflictCloudVersion { get; private set; }
    public long ConflictCloudUpdatedAt { get; private set; }

    public int LocalScore => local?.bestScore ?? 0;
    public long LocalSavedUtcTicks => local?.savedUtcTicks ?? 0;

    private void Awake()
    {
        Instance = this;

        if (!string.IsNullOrWhiteSpace(cloudSpace))
            CloudSave.Space = cloudSpace.Trim();
        else
            Debug.LogError("[Roaming] Cloud Space is missing!");
    }

    public void SelectProfile(LocalProfile profile)
    {
        if (profile == null)
            return;

        EndSession();

        activeUserId = profile.id;
        local = LoadLocal(activeUserId);

        if (local.exists)
            ApplyScore(local.bestScore);

        SetStatus("Checking cloud...");
        Synchronize();
    }

    public void EndSession()
    {
        generation++;

        activeUserId = null;
        local = null;

        busy = false;
        queued = false;
        revision = 0;

        HasConflict = false;
    }

    public void ClearLocalSaveForTesting()
    {
        if (string.IsNullOrEmpty(activeUserId) || local == null)
        {
            Debug.LogWarning("[Roaming] Log in first.");
            return;
        }

        if (busy || HasConflict || local.pending ||
            CloudSave.SimulateOffline)
        {
            Debug.LogWarning(
                "[Roaming] Cannot clear: wait for cloud sync first."
            );
            return;
        }

        string userId = activeUserId;

        EndSession();

        PlayerPrefs.DeleteKey(LocalPrefix + userId);

        PlayerPrefs.DeleteKey(
            LocalProfileStore.GetScoreKey(userId)
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[Roaming] Local save cleared. Cloud data untouched."
        );


        ProfileMenuController.Instance?.SimulateLogout();
    }

    private LocalData LoadLocal(string userId)
    {
        string json = PlayerPrefs.GetString(
            LocalPrefix + userId, ""
        );

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                LocalData data = JsonUtility.FromJson<LocalData>(json);

                if (data != null && data.exists)
                    return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Roaming] Local load: " + e.Message);
            }
        }

        LocalData fresh = new LocalData();

        string oldKey = LocalProfileStore.GetScoreKey(userId);

        if (PlayerPrefs.HasKey(oldKey))
        {
            fresh.exists = true;
            fresh.bestScore = Mathf.Max(0, PlayerPrefs.GetInt(oldKey));
            fresh.pending = true;
            fresh.savedUtcTicks = DateTime.UtcNow.Ticks;

            PlayerPrefs.SetString(
                LocalPrefix + userId,
                JsonUtility.ToJson(fresh)
            );
            PlayerPrefs.Save();
        }

        return fresh;
    }

    private void PersistLocal()
    {
        if (local == null || activeUserId == null)
            return;

        PlayerPrefs.SetString(
            LocalPrefix + activeUserId,
            JsonUtility.ToJson(local)
        );

        PlayerPrefs.Save();
    }

    public void SaveHighScore(int bestScore)
    {
        if (activeUserId == null || local == null)
            return;

        bestScore = Mathf.Max(0, bestScore);

        if (!local.exists || local.bestScore != bestScore)
        {
            local.exists = true;
            local.bestScore = bestScore;
            local.pending = true;
            local.savedUtcTicks = DateTime.UtcNow.Ticks;

            revision++;
            PersistLocal();

            Debug.Log("[Roaming] Saved locally: " + bestScore);
        }

        if (!HasConflict)
            Synchronize();
    }

    public void OnNetworkChanged(bool available)
    {
        if (available)
        {
            Synchronize();
        }
        else
        {
            SetStatus(local != null && local.pending
                ? "OFFLINE - SAVE PENDING"
                : "OFFLINE");
        }
    }

    public void Synchronize()
    {
        if (activeUserId == null || HasConflict)
            return;

        if (busy)
        {
            queued = true;
            return;
        }

        if (CloudSave.SimulateOffline)
        {
            SetStatus("OFFLINE - LOCAL SAVE ACTIVE");
            return;
        }

        busy = true;

        int requestGeneration = generation;
        string user = activeUserId;

        CloudSave.Load(user, CloudKey, result =>
        {
            if (requestGeneration != generation)
                return;

            busy = false;

            if (!result.ok)
            {
                SetStatus("Cloud unavailable: " + result.error);
                return;
            }

            ProcessCloudLoad(result);
            RunQueuedSync();
        });
    }

    private void ProcessCloudLoad(CloudSave.Result result)
    {
        if (!result.found)
        {
            if (local.exists)
            {
                local.baseVersion = 0;
                local.pending = true;
                PersistLocal();
                Upload();
            }
            else
            {
                SetStatus("NEW SAVE - NO CLOUD DATA");
            }

            return;
        }

        if (!TryParseCloud(result.value, out int cloudScore))
        {
            SetStatus("ERROR - INVALID CLOUD DATA");
            return;
        }

        if (!local.exists)
        {
            AcceptCloud(cloudScore, result.version);
            return;
        }

        if (result.version < local.baseVersion)
        {
            SetStatus("ERROR - CLOUD VERSION REGRESSED");
            return;
        }

        if (result.version == local.baseVersion)
        {
            if (local.pending)
                Upload();
            else
                SetStatus("SYNCED - VERSION " + result.version);

            return;
        }

        if (!local.pending || local.bestScore == cloudScore)
        {
            AcceptCloud(cloudScore, result.version);
        }
        else
        {
            RegisterConflict(result, cloudScore);
        }
    }

    private void Upload()
    {
        if (busy || HasConflict || local == null || !local.exists)
            return;

        if (CloudSave.SimulateOffline)
        {
            SetStatus("OFFLINE - SAVE PENDING");
            return;
        }

        busy = true;

        int requestGeneration = generation;
        int sentRevision = revision;
        int expectedVersion = local.baseVersion;

        CloudData payload = new CloudData
        {
            bestScore = local.bestScore
        };

        string json = JsonUtility.ToJson(payload);

        CloudSave.Save(activeUserId, CloudKey, json, result =>
        {
            if (requestGeneration != generation)
                return;

            busy = false;

            if (result.ok)
            {
                local.baseVersion = result.version;

                local.pending = revision != sentRevision;

                PersistLocal();

                SetStatus("SYNCED - VERSION " + result.version);

                if (local.pending)
                    Synchronize();
            }
            else if (result.IsConflict)
            {
                if (TryParseCloud(result.value, out int remoteScore))
                {
                    if (remoteScore == local.bestScore)
                        AcceptCloud(remoteScore, result.version);
                    else
                        RegisterConflict(result, remoteScore);
                }
                else
                {
                    SetStatus("ERROR - INVALID CONFLICT DATA");
                }
            }
            else
            {
                SetStatus("SAVE PENDING: " + result.error);
            }

            RunQueuedSync();
        }, expectedVersion);
    }

    private static bool TryParseCloud(string json, out int score)
    {
        score = 0;

        if (string.IsNullOrWhiteSpace(json) ||
            !json.Contains("\"bestScore\""))
            return false;

        try
        {
            CloudData data = JsonUtility.FromJson<CloudData>(json);

            if (data == null || data.schema != 1 ||
                data.bestScore < 0)
                return false;

            score = data.bestScore;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void AcceptCloud(int score, int version)
    {
        local.exists = true;
        local.bestScore = score;
        local.baseVersion = version;
        local.pending = false;
        local.savedUtcTicks = DateTime.UtcNow.Ticks;

        HasConflict = false;

        PersistLocal();
        ApplyScore(score);

        SetStatus("CLOUD RESTORED - VERSION " + version);
    }

    private void RegisterConflict(CloudSave.Result result, int score)
    {
        HasConflict = true;

        ConflictCloudScore = score;
        ConflictCloudVersion = result.version;
        ConflictCloudUpdatedAt = result.updatedAt;

        Debug.LogWarning(
            $"[Roaming] CONFLICT! Local={local.bestScore}, " +
            $"Cloud={score}, CloudVersion={result.version}"
        );

        SetStatus("SAVE CONFLICT - DECISION REQUIRED");
    }

    public void KeepLocal()
    {
        if (!HasConflict || local == null)
            return;

        local.baseVersion = ConflictCloudVersion;
        local.pending = true;

        HasConflict = false;
        PersistLocal();

        Upload();
    }

    public void KeepCloud()
    {
        if (!HasConflict || local == null)
            return;

        AcceptCloud(ConflictCloudScore, ConflictCloudVersion);
        RunQueuedSync();
    }

    private void RunQueuedSync()
    {
        if (!queued || busy || HasConflict)
            return;

        queued = false;
        Synchronize();
    }

    private void ApplyScore(int score)
    {
        RoundManager round = FindFirstObjectByType<RoundManager>();

        if (round != null)
            round.ApplyRestoredHighScore(score);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[Roaming] " + message);
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

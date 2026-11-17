
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class AchievementDefinition
{
    public string id;
    public string name;
    public string description;
    public int points;
    public string condition;
    public int target;
    public bool hidden;
}

[Serializable]
public class AchievementCatalog
{
    public AchievementDefinition[] achievements;
}

[Serializable]
public class AchievementProgress
{
    public string id;
    public int current;
    public bool unlocked;
}

[Serializable]
public class AchievementSaveData
{
    public List<AchievementProgress> entries = new();
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Achievement Data")]
    [SerializeField] private TextAsset definitionsJson;

    [Header("Unlock Notification")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TMP_Text toastText;

    private const string SavePrefix = "WhackABee.Achievements.v1.";

    private AchievementCatalog catalog;

    private readonly Dictionary<string, AchievementProgress>
        states = new();

    private readonly Queue<AchievementDefinition>
        toastQueue = new();

    private Coroutine toastRoutine;
    private string activeUserId;

    private int beesHitThisRound;
    private bool bombHitThisRound;

    public bool HasUser => !string.IsNullOrEmpty(activeUserId);

    public IReadOnlyList<AchievementDefinition> Definitions =>
        catalog?.achievements ?? Array.Empty<AchievementDefinition>();

    public event Action<AchievementDefinition> AchievementUnlocked;
    public event Action ProgressChanged;
    private AchievementDefinition activeToast;



    private void Start()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LanguageChanged +=
                RefreshActiveToast;
        }
    }

    private void RefreshActiveToast()
    {
        if (activeToast == null || toastText == null)
            return;

        toastText.text =
            Localized(
                "ACHIEVEMENT_UNLOCKED",
                "ACHIEVEMENT UNLOCKED!"
            ) + "\n" +
            GetLocalizedName(activeToast) + "\n" +
            Localized(
                "ACH_TOAST_POINTS",
                "+{0} POINTS",
                activeToast.points
            );
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (definitionsJson == null)
        {
            Debug.LogError("[Achievements] Missing JSON file!");
            return;
        }

        try
        {
            catalog = JsonUtility.FromJson<AchievementCatalog>(
                definitionsJson.text
            );
        }
        catch (Exception e)
        {
            Debug.LogError("[Achievements] JSON error: " + e.Message);
        }

        if (catalog?.achievements == null)
        {
            Debug.LogError("[Achievements] Invalid catalog.");
            return;
        }

        int totalPoints = 0;

        foreach (var definition in catalog.achievements)
            totalPoints += definition.points;

        Debug.Log(
            $"[Achievements] Loaded {catalog.achievements.Length} " +
            $"achievements worth {totalPoints} points."
        );

        if (toastPanel != null)
            toastPanel.SetActive(false);
    }

    public AchievementSaveData CreateSnapshot()
    {
        var snapshot = new AchievementSaveData();

        if (!HasUser || catalog?.achievements == null)
            return snapshot;

        foreach (var definition in catalog.achievements)
        {
            if (!states.TryGetValue(
                    definition.id, out AchievementProgress state))
                continue;

            int progress = Mathf.Clamp(
                state.current, 0, definition.target
            );

            bool unlocked = state.unlocked ||
                            progress >= definition.target;

            if (unlocked)
                progress = definition.target;

            if (progress == 0 && !unlocked)
                continue;

            snapshot.entries.Add(new AchievementProgress
            {
                id = definition.id,
                current = progress,
                unlocked = unlocked
            });
        }

        return snapshot;
    }

    public static string Localized(
        string key,
        string fallback,
        params object[] args)
    {
        LocalizationManager loc = LocalizationManager.Instance;

        if (loc != null)
        {
            string translated = loc.Translate(key, args);

            if (translated != "#" + key)
                return translated;
        }

        return args != null && args.Length > 0
            ? string.Format(fallback, args)
            : fallback;
    }

    public string GetLocalizedName(AchievementDefinition achievement)
    {
        return Localized(
            achievement.id + "_NAME",
            achievement.name
        );
    }

    public string GetLocalizedDescription(AchievementDefinition achievement)
    {
        return Localized(
            achievement.id + "_DESC",
            achievement.description
        );
    }

    public void MergeCloud(AchievementSaveData remote)
    {
        if (!HasUser || remote?.entries == null ||
            catalog?.achievements == null)
            return;

        bool changed = false;

        foreach (var entry in remote.entries)
        {
            if (entry == null)
                continue;

            AchievementDefinition definition = Array.Find(
                catalog.achievements,
                a => a.id == entry.id
            );

            if (definition == null || definition.target <= 0)
                continue;

            AchievementProgress local = GetState(entry.id);

            int remoteProgress = Mathf.Clamp(
                entry.current, 0, definition.target
            );

            if (entry.unlocked)
                remoteProgress = definition.target;

            int mergedProgress = Mathf.Max(
                local.current, remoteProgress
            );

            bool mergedUnlocked =
                local.unlocked ||
                entry.unlocked ||
                mergedProgress >= definition.target;

            if (local.current != mergedProgress ||
                local.unlocked != mergedUnlocked)
            {
                local.current = mergedProgress;
                local.unlocked = mergedUnlocked;
                changed = true;
            }
        }

        if (changed)
        {
            SaveLocal(true);
            ProgressChanged?.Invoke();

            Debug.Log("[Achievements] Cloud progress merged.");
        }
    }

    public void SelectProfile(LocalProfile profile)
    {
        EndSession();

        if (profile == null)
            return;

        activeUserId = profile.id;

        string json = PlayerPrefs.GetString(
            SavePrefix + activeUserId, ""
        );

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                AchievementSaveData save =
                    JsonUtility.FromJson<AchievementSaveData>(json);

                if (save?.entries != null)
                {
                    foreach (var entry in save.entries)
                    {
                        if (entry != null &&
                            !string.IsNullOrEmpty(entry.id))
                        {
                            states[entry.id] = entry;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "[Achievements] Local save error: " + e.Message
                );
            }
        }
        ProgressChanged?.Invoke();
        Debug.Log(
            $"[Achievements] Loaded profile: {profile.displayName}"
        );
    }

    public void EndSession()
    {
        activeToast = null;
        SaveLocal(true);

        activeUserId = null;
        states.Clear();

        beesHitThisRound = 0;
        bombHitThisRound = false;

        if (toastRoutine != null)
        {
            StopCoroutine(toastRoutine);
            toastRoutine = null;
        }

        toastQueue.Clear();

        if (toastPanel != null)
            toastPanel.SetActive(false);
        ProgressChanged?.Invoke();
    }

    private AchievementProgress GetState(string id)
    {
        if (!states.TryGetValue(id, out AchievementProgress state))
        {
            state = new AchievementProgress
            {
                id = id
            };

            states[id] = state;
        }

        return state;
    }

    public int GetProgress(string id)
    {
        return GetState(id).current;
    }

    public bool IsUnlocked(string id)
    {
        return GetState(id).unlocked;
    }

    private void Record(
        string condition,
        int value,
        bool absolute = false)
    {
        if (!HasUser || catalog?.achievements == null)
            return;

        bool changed = false;
        bool unlockedAny = false;

        foreach (var definition in catalog.achievements)
        {
            if (definition.condition != condition ||
                definition.target <= 0)
                continue;

            AchievementProgress state = GetState(definition.id);

            if (state.unlocked)
                continue;

            int next = absolute
                ? Mathf.Max(state.current, value)
                : state.current + Mathf.Max(0, value);

            next = Mathf.Clamp(next, 0, definition.target);

            if (next == state.current)
                continue;

            state.current = next;
            changed = true;

            if (state.current >= definition.target)
            {
                state.unlocked = true;
                unlockedAny = true;

                Debug.Log(
                    $"[Achievements] UNLOCKED: {definition.name}"
                );

                toastQueue.Enqueue(definition);
                AchievementUnlocked?.Invoke(definition);
            }
        }

        if (changed)
        {
            SaveLocal(unlockedAny);
            ProgressChanged?.Invoke();
        }

        if (unlockedAny)
            AchievementCloudSync.Instance?.Synchronize();

        if (toastRoutine == null && toastQueue.Count > 0)
            toastRoutine = StartCoroutine(ShowToasts());
    }

    public void RoundStarted()
    {
        if (!HasUser)
            return;

        beesHitThisRound = 0;
        bombHitThisRound = false;
    }

    public void TargetHit(int points)
    {
        if (!HasUser)
            return;

        if (points > 0)
        {
            beesHitThisRound++;
            Record("bee_hit", 1);
        }
        else if (points < 0)
        {
            bombHitThisRound = true;
        }
    }

    public void RoundFinished(int finalScore)
    {
        if (!HasUser)
            return;

        Record("round_completed", 1);
        Record("round_score", Mathf.Max(0, finalScore), true);

        if (!bombHitThisRound && beesHitThisRound > 0)
            Record("clean_round", 1);

        SaveLocal(true);
        AchievementCloudSync.Instance?.Synchronize();
    }

    public void SettingChanged()
    {
        Record("settings_change", 1);
    }

    private void SaveLocal(bool flush)
    {
        if (!HasUser)
            return;

        AchievementSaveData data = new AchievementSaveData();

        foreach (var state in states.Values)
            data.entries.Add(state);

        PlayerPrefs.SetString(
            SavePrefix + activeUserId,
            JsonUtility.ToJson(data)
        );

        if (flush)
            PlayerPrefs.Save();
    }

   
    private IEnumerator ShowToasts()
    {
        while (toastQueue.Count > 0)
        {
            activeToast = toastQueue.Dequeue();

            RefreshActiveToast();

            if (toastPanel != null)
                toastPanel.SetActive(true);

            yield return new WaitForSecondsRealtime(3f);

            if (toastPanel != null)
                toastPanel.SetActive(false);

            yield return new WaitForSecondsRealtime(0.3f);
        }

        activeToast = null;
        toastRoutine = null;
    }


    [ContextMenu("Achievements / Print Progress")]
    private void PrintProgress()
    {
        if (!HasUser || catalog?.achievements == null)
        {
            Debug.LogWarning("[Achievements] Log in first.");
            return;
        }

        foreach (var definition in catalog.achievements)
        {
            AchievementProgress state = GetState(definition.id);

            Debug.Log(
                $"[Achievements] {definition.id}: " +
                $"{state.current}/{definition.target}, " +
                $"unlocked={state.unlocked}"
            );
        }
    }

    [ContextMenu("Achievements / TEST Unlock All")]
    private void TestUnlockAll()
    {
        if (!HasUser || catalog?.achievements == null)
            return;

        foreach (var definition in catalog.achievements)
            Record(definition.condition, definition.target, true);
    }

    [ContextMenu("Achievements / TEST Reset Local Progress")]
    private void TestResetLocalProgress()
    {
        if (!HasUser)
            return;

        states.Clear();
        SaveLocal(true);
        ProgressChanged?.Invoke();
        Debug.Log("[Achievements] Local achievement progress reset.");
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveLocal(true);
    }

    private void OnApplicationQuit()
    {
        SaveLocal(true);
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LanguageChanged -=
                RefreshActiveToast;
        }
        if (Instance == this)
        {
            EndSession();
            Instance = null;
        }
    }
}

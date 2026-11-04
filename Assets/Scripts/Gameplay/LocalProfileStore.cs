
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalProfile
{
    public string id;
    public string displayName;

    public LocalProfile(string id, string displayName)
    {
        this.id = id;
        this.displayName = displayName;
    }
}

[Serializable]
public class ProfileCollection
{
    public List<LocalProfile> profiles = new();
}

public static class LocalProfileStore
{
    private const string ProfilesKey = "WhackABee.Profiles.v1";
    private const string ScorePrefix = "WhackABee.Score.";

    public static List<LocalProfile> Load()
    {
        string json = PlayerPrefs.GetString(ProfilesKey, "");

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var data = JsonUtility.FromJson<ProfileCollection>(json);

                if (data != null && data.profiles != null &&
                    data.profiles.Count > 0)
                {
                    return data.profiles;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Profiles] Load failed: " + e.Message);
            }
        }

        var defaults = new List<LocalProfile>
        {
            new("default-ala", "Ala"),
            new("default-olek", "Olek")
        };

        Save(defaults);
        return defaults;
    }

    public static void Save(List<LocalProfile> profiles)
    {
        var data = new ProfileCollection { profiles = profiles };

        PlayerPrefs.SetString(
            ProfilesKey,
            JsonUtility.ToJson(data)
        );

        PlayerPrefs.Save();
    }

    public static bool TryCreate(
        List<LocalProfile> profiles,
        string name,
        out string error)
    {
        name = (name ?? "").Trim();
        error = "";

        if (name.Length < 2 || name.Length > 18)
        {
            error = "Name must have 2-18 characters.";
            return false;
        }

        if (profiles.Exists(p =>
            string.Equals(p.displayName, name,
                StringComparison.OrdinalIgnoreCase)))
        {
            error = "Profile already exists.";
            return false;
        }

        profiles.Add(new LocalProfile(
            Guid.NewGuid().ToString("N"),
            name
        ));

        Save(profiles);
        return true;
    }

    public static string GetScoreKey(string profileId)
    {
        return ScorePrefix + profileId;
    }
}

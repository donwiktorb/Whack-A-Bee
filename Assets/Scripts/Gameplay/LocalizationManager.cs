
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class LocalizationEntry
{
    public string key;
    public string en;
    public string pl;
    public string de;
    public string ja;
}

[Serializable]
public class LocalizationTable
{
    public LocalizationEntry[] entries;
}

[DefaultExecutionOrder(-200)]
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [SerializeField] private TextAsset translationFile;

    private const string PreferenceKey = "WhackABee.Language";

    private readonly Dictionary<string, LocalizationEntry>
        translations = new();

    public string CurrentLanguage { get; private set; } = "en";

    public event Action LanguageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        LoadTranslations();

        string preferred = PlayerPrefs.HasKey(PreferenceKey)
            ? PlayerPrefs.GetString(PreferenceKey)
            : DetectSystemLanguage();

        CurrentLanguage = IsSupported(preferred)
            ? preferred
            : "en";

        Debug.Log("[Localization] Language: " + CurrentLanguage);
    }

    private void LoadTranslations()
    {
        if (translationFile == null)
        {
            Debug.LogError("[Localization] Missing translation JSON!");
            return;
        }

        try
        {
            LocalizationTable table =
                JsonUtility.FromJson<LocalizationTable>(
                    translationFile.text
                );

            if (table?.entries == null)
                return;

            foreach (LocalizationEntry entry in table.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key))
                    continue;

                translations[entry.key] = entry;
            }

            Debug.Log(
                $"[Localization] Loaded {translations.Count} keys."
            );
        }
        catch (Exception e)
        {
            Debug.LogError("[Localization] " + e.Message);
        }
    }

    private static string DetectSystemLanguage()
    {
        return Application.systemLanguage switch
        {
            SystemLanguage.Polish => "pl",
            SystemLanguage.German => "de",
            SystemLanguage.Japanese => "ja",
            _ => "en"
        };
    }

    private static bool IsSupported(string language)
    {
        return language == "en" ||
               language == "pl" ||
               language == "de" ||
               language == "ja";
    }

    public void SetLanguage(string language)
    {
        if (!IsSupported(language) ||
            CurrentLanguage == language)
            return;

        CurrentLanguage = language;

        PlayerPrefs.SetString(PreferenceKey, language);
        PlayerPrefs.Save();

        LanguageChanged?.Invoke();

        Debug.Log("[Localization] Changed to: " + language);
    }

    public string Translate(string key, params object[] args)
    {
        if (!translations.TryGetValue(key, out var entry))
            return "#" + key;

        string value = CurrentLanguage switch
        {
            "pl" => entry.pl,
            "de" => entry.de,
            "ja" => entry.ja,
            _ => entry.en
        };

        if (string.IsNullOrEmpty(value))
            value = entry.en;

        if (string.IsNullOrEmpty(value))
            return "#" + key;

        if (args == null || args.Length == 0)
            return value;

        try
        {
            CultureInfo culture =
                CultureInfo.GetCultureInfo(CurrentLanguage);

            return string.Format(culture, value, args);
        }
        catch (FormatException)
        {
            Debug.LogWarning(
                "[Localization] Invalid format: " + key
            );

            return value;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

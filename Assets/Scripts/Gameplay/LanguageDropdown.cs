
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class LanguageDropdown : MonoBehaviour
{
    private static readonly string[] Codes =
    {
        "en", "pl", "de", "ja"
    };

    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        dropdown.ClearOptions();

        dropdown.AddOptions(new List<string>
        {
            "English",
            "Polski",
            "Deutsch",
            "日本語"
        });
    }

    private void OnEnable()
    {
        dropdown.onValueChanged.AddListener(OnChanged);

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LanguageChanged += Refresh;
        }

        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        dropdown.onValueChanged.RemoveListener(OnChanged);

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LanguageChanged -= Refresh;
        }
    }

    private void OnChanged(int index)
    {
        if (index < 0 || index >= Codes.Length)
            return;

        LocalizationManager.Instance?.SetLanguage(Codes[index]);
    }

    private void Refresh()
    {
        if (LocalizationManager.Instance == null)
            return;

        string current = LocalizationManager.Instance.CurrentLanguage;

        for (int i = 0; i < Codes.Length; i++)
        {
            if (Codes[i] != current)
                continue;

            dropdown.SetValueWithoutNotify(i);
            dropdown.RefreshShownValue();
            break;
        }
    }
}

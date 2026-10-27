
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class VSyncToggle : MonoBehaviour
{
    private const string SaveKey = "VSync";
    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();

        bool enabled = PlayerPrefs.GetInt(
            SaveKey,
            QualitySettings.vSyncCount > 0 ? 1 : 0
        ) == 1;

        toggle.SetIsOnWithoutNotify(enabled);
        ApplyVSync(enabled);

        toggle.onValueChanged.AddListener(SetVSync);
    }

    private void SetVSync(bool enabled)
    {
        ApplyVSync(enabled);

        PlayerPrefs.SetInt(SaveKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(SetVSync);
    }
}

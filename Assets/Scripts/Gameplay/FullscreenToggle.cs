
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class FullscreenToggle : MonoBehaviour
{
    private const string SaveKey = "Fullscreen";
    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();

        bool fullscreen = PlayerPrefs.GetInt(
            SaveKey,
            Screen.fullScreen ? 1 : 0
        ) == 1;

        toggle.SetIsOnWithoutNotify(fullscreen);
        ApplyFullscreen(fullscreen);

        toggle.onValueChanged.AddListener(SetFullscreen);
    }

    private void SetFullscreen(bool fullscreen)
    {
        ApplyFullscreen(fullscreen);

        PlayerPrefs.SetInt(SaveKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyFullscreen(bool fullscreen)
    {
        Screen.fullScreenMode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(SetFullscreen);
    }
}

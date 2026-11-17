
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedTMPText : MonoBehaviour
{
    [SerializeField] private string translationKey;

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
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
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LanguageChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();

        if (LocalizationManager.Instance == null)
            return;

        label.text =
            LocalizationManager.Instance.Translate(translationKey);
    }
}

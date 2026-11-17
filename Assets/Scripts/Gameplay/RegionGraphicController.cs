
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionGraphicController : MonoBehaviour
{
    private const string SaveKey = "WhackABee.Region";

    private const string Global = "GLOBAL";
    private const string Restricted = "DEMO_RESTRICTED";

    [Header("Settings")]
    [SerializeField] private TMP_Dropdown regionDropdown;

    [Header("Regional Graphic")]
    [SerializeField] private Image regionalImage;
    [SerializeField] private Sprite originalSprite;
    [SerializeField] private Sprite restrictedSprite;

    public string CurrentRegion { get; private set; }

    private void Awake()
    {
        CurrentRegion = PlayerPrefs.GetString(
            SaveKey, Global
        );

        if (CurrentRegion != Global &&
            CurrentRegion != Restricted)
        {
            CurrentRegion = Global;
        }
    }

    private void Start()
    {
        if (regionDropdown != null)
        {
            regionDropdown.ClearOptions();

            regionDropdown.AddOptions(new List<string>
            {
                "Global (Original)",
                "Restricted Demo (Alternative)"
            });

            int selected = CurrentRegion == Restricted
                ? 1
                : 0;

            regionDropdown.SetValueWithoutNotify(selected);
            regionDropdown.RefreshShownValue();

            regionDropdown.onValueChanged.AddListener(
                OnRegionSelected
            );
        }

        ApplyRegionalGraphic();
    }

    private void OnRegionSelected(int index)
    {
        SetRegion(index == 1 ? Restricted : Global);
    }

    public void SetRegion(string region)
    {
        if (region != Global && region != Restricted)
            return;

        CurrentRegion = region;

        PlayerPrefs.SetString(SaveKey, region);
        PlayerPrefs.Save();

        ApplyRegionalGraphic();

        Debug.Log("[Region] Changed to: " + region);
    }

    private void ApplyRegionalGraphic()
    {
        if (regionalImage == null)
            return;

        regionalImage.sprite = CurrentRegion == Restricted
            ? restrictedSprite
            : originalSprite;

        regionalImage.preserveAspect = true;
    }

    private void OnDestroy()
    {
        if (regionDropdown != null)
        {
            regionDropdown.onValueChanged.RemoveListener(
                OnRegionSelected
            );
        }
    }
}

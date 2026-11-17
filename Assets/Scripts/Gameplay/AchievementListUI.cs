
using System.Text;
using TMPro;
using UnityEngine;

public class AchievementListUI : MonoBehaviour
{
    public static AchievementListUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [SerializeField] private TMP_Text achievementsText;
    [SerializeField] private TMP_Text summaryText;

    private AchievementManager manager;

    private void Awake()
    {
        Instance = this;

        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

    private void Start()
    {
        manager = AchievementManager.Instance;

        if (manager != null)
            manager.ProgressChanged += Refresh;
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LanguageChanged += Refresh;
        }
    }

    public void Open()
    {
        if (manager == null || !manager.HasUser)
            return;

        Refresh();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (achievementsPanel != null)
            achievementsPanel.SetActive(true);
    }

    public void Close()
    {
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void CloseForSessionEnd()
    {
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

   
    public void Refresh()
    {
        if (manager == null)
            return;

        StringBuilder builder = new StringBuilder();

        int earnedPoints = 0;
        int totalPoints = 0;
        int unlockedCount = 0;
        int totalCount = 0;

        foreach (AchievementDefinition achievement in manager.Definitions)
        {
            totalCount++;
            totalPoints += achievement.points;

            bool unlocked = manager.IsUnlocked(achievement.id);

            if (unlocked)
            {
                unlockedCount++;
                earnedPoints += achievement.points;
            }

            if (achievement.hidden && !unlocked)
            {
                builder.AppendLine(
                    "<color=#888888>" +
                    AchievementManager.Localized(
                        "ACH_STATUS_LOCKED", "LOCKED"
                    ) +
                    "  ???</color>"
                );

                builder.AppendLine(
                    AchievementManager.Localized(
                        "ACH_HIDDEN_DESC", "Hidden achievement"
                    )
                );

                builder.AppendLine();
                continue;
            }

            string status = unlocked
                ? "<color=#66DD88>" +
                AchievementManager.Localized(
                    "ACH_STATUS_UNLOCKED", "UNLOCKED"
                ) + "</color>"
                : "<color=#AAAAAA>" +
                AchievementManager.Localized(
                    "ACH_STATUS_LOCKED", "LOCKED"
                ) + "</color>";

            builder.AppendLine(
                status + "  " + manager.GetLocalizedName(achievement)
            );

            builder.AppendLine(
                manager.GetLocalizedDescription(achievement)
            );

            string points = AchievementManager.Localized(
                "ACH_POINTS", "{0} PTS", achievement.points
            );

            if (unlocked)
            {
                builder.AppendLine(
                    "<color=#66DD88>+" + points + "</color>"
                );
            }
            else
            {
                int progress = manager.GetProgress(achievement.id);

                string progressText = AchievementManager.Localized(
                    "ACH_PROGRESS",
                    "Progress: {0}/{1}",
                    progress,
                    achievement.target
                );

                builder.AppendLine(progressText + " | " + points);
            }

            builder.AppendLine();
        }

        if (achievementsText != null)
            achievementsText.text = builder.ToString();

        if (summaryText != null)
        {
            summaryText.text = AchievementManager.Localized(
                "ACH_SUMMARY",
                "ACHIEVEMENTS: {0}/{1}\nPOINTS: {2}/{3}",
                unlockedCount,
                totalCount,
                earnedPoints,
                totalPoints
            );
        }
    }


    private void OnDestroy()
    {
        if (manager != null)
            manager.ProgressChanged -= Refresh;

        if (Instance == this)
            Instance = null;
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.LanguageChanged -= Refresh;
        }
    }
}

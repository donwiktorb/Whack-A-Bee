
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoamingConflictUI : MonoBehaviour
{
    [Header("Conflict Dialog")]
    [SerializeField] private GameObject conflictPanel;
    [SerializeField] private TMP_Text comparisonText;

    [Header("Decision Buttons")]
    [SerializeField] private Button keepLocalButton;
    [SerializeField] private Button keepCloudButton;

    [Header("Game")]
    [SerializeField] private RoundManager roundManager;

    private bool visible;
    private bool pausedByConflict;

    private void Awake()
    {
        if (conflictPanel != null)
            conflictPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (keepLocalButton != null)
            keepLocalButton.onClick.AddListener(KeepLocal);

        if (keepCloudButton != null)
            keepCloudButton.onClick.AddListener(KeepCloud);
    }

    private void OnDisable()
    {
        if (keepLocalButton != null)
            keepLocalButton.onClick.RemoveListener(KeepLocal);

        if (keepCloudButton != null)
            keepCloudButton.onClick.RemoveListener(KeepCloud);

        if (pausedByConflict && roundManager != null)
            roundManager.ResumeRound();

        pausedByConflict = false;
        visible = false;

        if (conflictPanel != null)
            conflictPanel.SetActive(false);
    }

    private void Update()
    {
        RoamingSaveManager saves = RoamingSaveManager.Instance;

        bool hasConflict = saves != null && saves.HasConflict;

        if (hasConflict != visible)
        {
            visible = hasConflict;

            if (hasConflict)
            {
                pausedByConflict =
                    roundManager != null && roundManager.IsPlaying;

                if (pausedByConflict)
                    roundManager.PauseRound();

                if (conflictPanel != null)
                {
                    conflictPanel.SetActive(true);
                    conflictPanel.transform.SetAsLastSibling();
                }

                if (EventSystem.current != null &&
                    keepLocalButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(
                        keepLocalButton.gameObject
                    );
                }
            }
            else
            {
                if (conflictPanel != null)
                    conflictPanel.SetActive(false);

                if (pausedByConflict && roundManager != null)
                    roundManager.ResumeRound();

                pausedByConflict = false;
            }
        }

        if (hasConflict)
            UpdateComparison(saves);
    }

    private void UpdateComparison(RoamingSaveManager saves)
    {
        if (comparisonText == null)
            return;

        string localDate = "Unknown";

        if (saves.LocalSavedUtcTicks > 0)
        {
            localDate = new DateTime(
                saves.LocalSavedUtcTicks,
                DateTimeKind.Utc
            ).ToLocalTime().ToString("g");
        }

        string cloudDate = "Unknown";

        if (saves.ConflictCloudUpdatedAt > 0)
        {
            cloudDate = DateTimeOffset
                .FromUnixTimeMilliseconds(
                    saves.ConflictCloudUpdatedAt
                )
                .LocalDateTime.ToString("g");
        }

        comparisonText.text =
            "SAVE CONFLICT DETECTED!\n\n" +
            "THIS COMPUTER\n" +
            $"Score: {saves.LocalScore:N0}\n" +
            $"Saved: {localDate}\n\n" +
            "CLOUD\n" +
            $"Score: {saves.ConflictCloudScore:N0}\n" +
            $"Saved: {cloudDate}\n" +
            $"Cloud version: {saves.ConflictCloudVersion}\n\n" +
            "Which progress would you like to keep?";
    }

    private void KeepLocal()
    {
        RoamingSaveManager.Instance?.KeepLocal();
    }

    private void KeepCloud()
    {
        RoamingSaveManager.Instance?.KeepCloud();
    }
}

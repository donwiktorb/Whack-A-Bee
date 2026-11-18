
using System;
using TMPro;
using UnityEngine;

public class LicenseGate : MonoBehaviour
{
    public static LicenseGate Instance { get; private set; }

    [Header("Screens")]
    [SerializeField] private GameObject licensePanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("License UI")]
    [SerializeField] private TMP_InputField licenseInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text generatedKeyText;

    private const string LicensePrefix = "WhackABee.License.v1.";
    private const string VerifiedPrefix = "WhackABee.LicenseVerified.v1.";

    private static readonly TimeSpan OfflineGrace =
        TimeSpan.FromDays(7);

    private string activeUserId;
    private int generation;
    private bool busy;

    public bool IsLicensed { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (licensePanel != null)
            licensePanel.SetActive(false);
    }

    private static string LicenseKey(string user)
    {
        return LicensePrefix + user;
    }

    private static string VerifiedKey(string user)
    {
        return VerifiedPrefix + user;
    }

    public void SelectProfile(LocalProfile profile)
    {
        EndSession();

        if (profile == null)
            return;

        activeUserId = profile.id;

        if (generatedKeyText != null)
            generatedKeyText.text = "";

        if (licenseInput != null)
            licenseInput.text = "";

        ShowLocked("Checking game license...");
        CheckSavedLicense();
    }

    public void EndSession()
    {
        generation++;
        activeUserId = null;
        busy = false;
        IsLicensed = false;

        if (licensePanel != null)
            licensePanel.SetActive(false);
    }

    public void CheckSavedLicense()
    {
        if (busy || string.IsNullOrEmpty(activeUserId))
            return;

        string user = activeUserId;

        string key = PlayerPrefs.GetString(
            LicenseKey(user), ""
        ).Trim();

        if (string.IsNullOrEmpty(key))
        {
            ShowLocked(
                "No license found. Buy a demo key or activate one."
            );
            return;
        }

        busy = true;
        IsLicensed = false;

        ShowLocked("Verifying license...");

        int requestGeneration = generation;

        CloudSave.CheckLicense(key, user, result =>
        {
            if (requestGeneration != generation)
                return;

            busy = false;

            if (result.ok && result.valid)
            {
                PlayerPrefs.SetString(
                    VerifiedKey(user),
                    DateTime.UtcNow.Ticks.ToString()
                );

                PlayerPrefs.Save();

                GrantAccess("LICENSE VERIFIED - ACCESS GRANTED");

                return;
            }

            if (!result.ok && result.IsOffline)
            {
                if (HasRecentVerification(user))
                {
                    GrantAccess(
                        "OFFLINE LICENSE - TEMPORARY ACCESS"
                    );
                }
                else
                {
                    ShowLocked(
                        "Offline access unavailable. " +
                        "Connect to verify your license."
                    );
                }

                return;
            }

            ShowLocked(
                result.ok
                    ? "This license is not valid for this profile."
                    : "License check failed: " +
                      (result.message ?? result.error)
            );
        });
    }

    private static bool HasRecentVerification(string user)
    {
        string saved = PlayerPrefs.GetString(
            VerifiedKey(user), ""
        );

        if (!long.TryParse(saved, out long ticks))
            return false;

        try
        {
            DateTime verified = new DateTime(
                ticks, DateTimeKind.Utc
            );

            TimeSpan elapsed = DateTime.UtcNow - verified;

            return elapsed >= TimeSpan.Zero &&
                   elapsed <= OfflineGrace;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public void BuyDemoLicense()
    {
        if (busy || string.IsNullOrEmpty(activeUserId))
            return;

        busy = true;

        int requestGeneration = generation;
        string user = activeUserId;

        SetStatus("Generating demo license...");

        CloudSave.NewLicense(result =>
        {
            if (requestGeneration != generation)
                return;

            if (!result.ok ||
                string.IsNullOrWhiteSpace(result.license))
            {
                busy = false;

                SetStatus(
                    "Cannot generate license: " +
                    (result.message ?? result.error)
                );

                return;
            }

            string key = result.license.Trim();

            if (generatedKeyText != null)
                generatedKeyText.text = "NEW KEY: " + key;

            if (licenseInput != null)
                licenseInput.text = key;

            busy = false;

            ActivateKey(key, user);
        });
    }

    public void ActivateFromInput()
    {
        if (busy || string.IsNullOrEmpty(activeUserId))
            return;

        string key = licenseInput != null
            ? licenseInput.text.Trim()
            : "";

        if (string.IsNullOrEmpty(key))
        {
            SetStatus("Enter a license key first.");
            return;
        }

        ActivateKey(key, activeUserId);
    }

    private void ActivateKey(string key, string user)
    {
        if (busy)
            return;

        busy = true;

        int requestGeneration = generation;

        SetStatus("Activating license...");

        CloudSave.ActivateLicense(key, user, result =>
        {
            if (requestGeneration != generation)
                return;

            busy = false;

            if (!result.ok)
            {
                string message;

                switch (result.error)
                {
                    case "already_activated":
                        message =
                            "This key is activated on another account.";
                        break;

                    case "invalid_license":
                        message = "Invalid license key.";
                        break;

                    case "offline":
                    case "network":
                        message =
                            "Connection required to activate a license.";
                        break;

                    default:
                        message = result.message ??
                                  "License activation failed.";
                        break;
                }

                ShowLocked(message);
                return;
            }

            PlayerPrefs.SetString(LicenseKey(user), key);
            PlayerPrefs.Save();

            SetStatus("License activated. Verifying...");

            CheckSavedLicense();
        });
    }

    public void OnNetworkChanged(bool available)
    {
        if (available &&
            !IsLicensed &&
            !busy &&
            !string.IsNullOrEmpty(activeUserId))
        {
            CheckSavedLicense();
        }
    }

    private void GrantAccess(string message)
    {
        IsLicensed = true;

        if (licensePanel != null)
            licensePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(WebSitelock.IsAllowed);

        Debug.Log("[DRM] " + message);
    }

    private void ShowLocked(string message)
    {
        IsLicensed = false;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (licensePanel != null)
            licensePanel.SetActive(true);

        SetStatus(message);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[DRM] " + message);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            EndSession();
            Instance = null;
        }
    }


    [ContextMenu("DRM / Show Saved License")]
    public void ShowSavedLicense()
    {
        string user = activeUserId;

        if (string.IsNullOrEmpty(user))
        {
            Debug.LogWarning("[DRM] Log in first!");
            return;
        }

        string key = PlayerPrefs.GetString(
            LicensePrefix + user, ""
        );

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[DRM] No saved license found.");
            return;
        }

        Debug.Log($"[DRM TEST] Profile: {user} | License: {key}");
    }

}

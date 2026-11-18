
using System;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class WebSitelock : MonoBehaviour
{
    public static bool IsAllowed { get; private set; } = true;

    [Header("Blocked Screen")]
    [SerializeField] private GameObject blockedPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private TMP_Text messageText;

    [Header("Allowed Domains")]
    [SerializeField] private string[] allowedDomains =
    {
        "itch.io",
        "itch.zone",
        "localhost",
        "127.0.0.1"
    };

#if UNITY_EDITOR
    [Header("Editor Testing")]
    [SerializeField] private bool simulateBlockedDomain;
#endif

    private void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string url = Application.absoluteURL;
        IsAllowed = IsApprovedUrl(url);

        Debug.Log("[Sitelock] Current URL: " + url);
#elif UNITY_EDITOR
        IsAllowed = !simulateBlockedDomain;
#else
        IsAllowed = true;
#endif

        if (messageText != null)
        {
            messageText.text =
                "THIS GAME CANNOT RUN ON THIS WEBSITE.\n" +
                "This copy is not hosted on an approved domain.";
        }

        if (blockedPanel != null)
        {
            blockedPanel.SetActive(!IsAllowed);

            if (!IsAllowed)
                blockedPanel.transform.SetAsLastSibling();
        }

        ApplyBlockedUI();

        Debug.Log("[Sitelock] Access allowed: " + IsAllowed);
    }

    private bool IsApprovedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
            return false;

        string host = uri.Host;

        foreach (string entry in allowedDomains)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            string allowed = entry.Trim().TrimEnd('.');

            if (host.Equals(
                    allowed,
                    StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(
                    "." + allowed,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyBlockedUI()
    {
        if (IsAllowed)
            return;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (blockedPanel != null && !blockedPanel.activeSelf)
            blockedPanel.SetActive(true);
    }

    private void LateUpdate()
    {
        if (!IsAllowed)
            ApplyBlockedUI();
    }
}

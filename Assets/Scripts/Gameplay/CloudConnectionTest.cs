
using System;
using UnityEngine;

public class CloudConnectionTest : MonoBehaviour
{
    [Header("DSW Cloud Configuration")]
    [SerializeField] private string cloudSpace = "51794";

    private const string TestKey = "exercise4_test";

    [Serializable]
    private class TestPayload
    {
        public string message;
        public string nonce;
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(cloudSpace) ||
            cloudSpace == "CHANGE-ME")
        {
            Debug.LogError(
                "[Cloud] Configure your unique cloud space!"
            );
            return;
        }

        CloudSave.Space = cloudSpace.Trim();

        Debug.Log("[Cloud] Space configured.");
    }

    [ContextMenu("DSW Cloud / Ping")]
    public void TestPing()
    {
        if (!ValidateConfiguration())
            return;

        CloudSave.Ping(result =>
        {
            if (result.ok)
            {
                Debug.Log(
                    "[Cloud] PING OK! Server time: " +
                    result.serverTime
                );
            }
            else
            {
                Debug.LogError(
                    "[Cloud] PING FAILED: " +
                    result.error + " - " + result.message
                );
            }
        });
    }

    [ContextMenu("DSW Cloud / Test Save and Load")]
    public void TestSaveAndLoad()
    {
        if (!ValidateConfiguration())
            return;

        var session = ProfileMenuController.Instance;

        if (session == null || !session.IsLoggedIn)
        {
            Debug.LogWarning(
                "[Cloud] Select a player profile first."
            );
            return;
        }

        string userId = session.ActiveProfile.id;

        CloudSave.Load(userId, TestKey, before =>
        {
            if (!before.ok)
            {
                LogFailure("Initial load", before);
                return;
            }

            int expectedVersion =
                before.found ? before.version : 0;

            TestPayload data = new TestPayload
            {
                message = "Whack-A-Bee Exercise 4",
                nonce = Guid.NewGuid().ToString("N")
            };

            string json = JsonUtility.ToJson(data);

            CloudSave.Save(
                userId,
                TestKey,
                json,
                saved =>
                {
                    if (!saved.ok)
                    {
                        LogFailure("Save", saved);
                        return;
                    }

                    Debug.Log(
                        "[Cloud] SAVE OK! Version: " +
                        saved.version
                    );

                    CloudSave.Load(userId, TestKey, loaded =>
                    {
                        if (!loaded.ok)
                        {
                            LogFailure("Verification load", loaded);
                            return;
                        }

                        if (loaded.found && loaded.value == json)
                        {
                            Debug.Log(
                                "[Cloud] ROUND TRIP SUCCESS! " +
                                "Saved data matches loaded data. " +
                                "Version: " + loaded.version
                            );
                        }
                        else
                        {
                            Debug.LogError(
                                "[Cloud] Verification failed: " +
                                "data mismatch or missing save."
                            );
                        }
                    });
                },
                expectedVersion
            );
        });
    }

    private bool ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(CloudSave.Space))
        {
            Debug.LogError("[Cloud] Missing cloud space.");
            return false;
        }

        return true;
    }

    private static void LogFailure(
        string operation,
        CloudSave.Result result)
    {
        Debug.LogError(
            $"[Cloud] {operation} failed: " +
            $"{result.error} (HTTP {result.httpCode}) " +
            result.message
        );
    }
}

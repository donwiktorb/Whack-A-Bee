
using UnityEngine;

public class ServerAntiCheatTest : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("AntiCheat / Test Server Validation")]
    public void TestServerValidation()
    {
        var profile = ProfileMenuController.Instance?.ActiveProfile;

        if (profile == null)
        {
            Debug.LogWarning("[AntiCheat] Log in first.");
            return;
        }

        string user = profile.id;
        string key = "anticheat_counter_test_v1";

        CloudSave.Add(user, key, 50, valid =>
        {
            if (!valid.ok)
            {
                Debug.LogError(
                    "[AntiCheat] Valid +50 failed: " + valid.error
                );
                return;
            }

            Debug.Log(
                "[AntiCheat] +50 ACCEPTED | Value: " + valid.value
            );

            CloudSave.Add(user, key, 5000, invalid =>
            {
                if (!invalid.ok && invalid.error == "rejected")
                {
                    Debug.Log(
                        "[AntiCheat] +5000 REJECTED BY SERVER!"
                    );
                }
                else
                {
                    Debug.LogError(
                        "[AntiCheat] Unexpected result: " +
                        invalid.error
                    );
                }

                CloudSave.Load(user, key, final =>
                {
                    Debug.Log(final.ok && final.found
                        ? "[AntiCheat] Final server value: " + final.value
                        : "[AntiCheat] Could not read final value.");
                });
            });
        });
    }
#endif
}

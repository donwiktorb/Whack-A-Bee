
using Mirror;
using UnityEngine;

using UnityEngine.InputSystem;

public class HammerController : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private LANMatchController lanMatch;

    [Header("Floating Score")]
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private FloatingScorePopup floatingScorePrefab;

    [Header("Hit Effects")]
    [SerializeField] private ParticleSystem hitParticlesPrefab;

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void Update()
    {
        bool soloPlaying =
            roundManager != null && roundManager.IsPlaying;

        bool lanPlaying =
            lanMatch != null &&
            lanMatch.IsMatchPlaying &&
            NetworkClient.isConnected;

        if ((!soloPlaying && !lanPlaying) || gameCamera == null)
            return;


        WhackInput input = WhackInput.Instance;

        if (input == null || !input.StrikePressed)
            return;

        Vector2 cursorPosition = input.CursorPosition;

        Ray ray = gameCamera.ScreenPointToRay(
            new Vector3(cursorPosition.x, cursorPosition.y, 0f)
        );


        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return;

        WhackTarget target =
            hit.collider.GetComponentInParent<WhackTarget>();

        if (target == null)
            return;

        if (!target.TryHit(out int points))
            return;

        if (floatingScorePrefab != null && gameCanvas != null)
        {
            Vector3 screenPosition =
                gameCamera.WorldToScreenPoint(
                    hit.point + Vector3.up * 0.4f
                );

            if (screenPosition.z > 0f)
            {
                FloatingScorePopup popup = Instantiate(
                    floatingScorePrefab,
                    gameCanvas.transform,
                    false
                );

                popup.transform.position = screenPosition;
                popup.Initialize(points);
            }
        }

        if (GameAudio.Instance != null)
        {
            GameAudio.Instance.PlayHit(points);
        }

        if (hitParticlesPrefab != null)
        {
            ParticleSystem effect = Instantiate(
                hitParticlesPrefab,
                hit.point + Vector3.up * 0.15f,
                Quaternion.identity
            );

            var main = effect.main;

            main.startColor = points > 0
                ? new Color(1f, 0.8f, 0.15f)
                : new Color(1f, 0.2f, 0.2f);

            effect.Play();

            Destroy(effect.gameObject, 2f);
        }

        if (lanPlaying && target.MultiplayerBeat >= 0)
        {
            if (NetworkClient.localPlayer == null)
                return;

            LANPlayer player =
                NetworkClient.localPlayer.GetComponent<LANPlayer>();

            if (player != null)
            {
                player.RequestLANHit(
                    lanMatch.MatchSeed,
                    target.MultiplayerBeat
                );
            }
        }
        else if (soloPlaying && target.MultiplayerBeat < 0)
        {
            roundManager.AddPoints(points);
        }
    }
}

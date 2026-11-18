
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoundManager : MonoBehaviour
{
    private enum RoundState
    {
        Menu,
        Countdown,
        Playing,
        Paused,
        Finished
    }

    [Header("Gameplay")]
    [SerializeField] private TargetSpawner targetSpawner;
    [SerializeField] private float roundDuration = 60f;

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;

    [Header("HUD")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;

    [Header("Results")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private TMP_Text resultBestText;

    [Header("Visuals")]
    [SerializeField] private GameObject hammerVisual;

    private RoundState state = RoundState.Menu;
    private Coroutine roundRoutine;

    private float timeRemaining;
    private int countdownSeconds = 3;
    [Header("Anti-Cheat")]
    [SerializeField] private TMP_Text cheatWarningText;

    private ProtectedScore scoreProtection;
    private bool scoreTampered;

    public bool HasIntegrityViolation => scoreTampered;
    private int score;
    private int highScore;

    public bool IsPlaying => state == RoundState.Playing;



    private void Awake()
    {
        scoreProtection = new ProtectedScore();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.LanguageChanged += RefreshLanguage;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.LanguageChanged -= RefreshLanguage;
    }

    private string L(string key, string fallback, params object[] args)
    {
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.Translate(key, args);

        return string.Format(fallback, args);
    }

    private void VerifyScoreIntegrity()
    {
        if (scoreTampered || scoreProtection.IsValid(score))
            return;

        scoreTampered = true;

        score = 0;
        scoreProtection.Set(0);

        if (cheatWarningText != null)
        {
            cheatWarningText.text =
                "CHEAT DETECTED!\n" +
                "Score reset. Round invalid.";

            cheatWarningText.gameObject.SetActive(true);
        }

        Debug.LogError(
            "[AntiCheat] Score integrity violation detected! " +
            "Score reset. This round cannot save progress."
        );
    }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("AntiCheat / Simulate Score Tampering")]
    private void SimulateScoreTampering()
    {
        if (state != RoundState.Playing)
        {
            Debug.LogWarning(
                "[AntiCheat] Start a solo round first."
            );
            return;
        }

        scoreProtection.SimulateTampering();

        VerifyScoreIntegrity();
        UpdateHUD();
    }
    #endif

    private void RefreshLanguage()
    {
        UpdateHUD();
        UpdateResultTexts();
    }

    private void UpdateResultTexts()
    {
        if (resultScoreText != null)
            resultScoreText.text = L(
                "RESULT_SCORE", "SCORE: {0:N0}", score
            );

        if (resultBestText != null)
            resultBestText.text = L(
                "RESULT_BEST", "BEST: {0:N0}", highScore
            );
    }

    public void LoadProfileScore(string profileId)
    {
        highScore = PlayerPrefs.GetInt(
            LocalProfileStore.GetScoreKey(profileId),
            0
        );
    }
    public void ApplyRestoredHighScore(int restoredScore)
    {
        highScore = Mathf.Max(0, restoredScore);

        var profile = ProfileMenuController.Instance?.ActiveProfile;

        if (profile != null)
        {
            PlayerPrefs.SetInt(
                LocalProfileStore.GetScoreKey(profile.id),
                highScore
            );

            PlayerPrefs.Save();
        }

        UpdateResultTexts();

        Debug.Log("[Round] Restored best score: " + highScore);
    }
    private void Update()
    {
        WhackInput input = WhackInput.Instance;
        if (RoamingSaveManager.Instance != null &&
            RoamingSaveManager.Instance.HasConflict)
        {
            return;
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null &&
            Keyboard.current.f10Key.wasPressedThisFrame)
        {
            SimulateScoreTampering();
            return;
        }
        #endif
        if (input == null || !input.PausePressed)
            return;

        if (state == RoundState.Playing)
        {
            PauseRound();
        }
        else if (state == RoundState.Paused)
        {
            ResumeRound();
        }
        
    }

    private void Start()
    {
        #if DEVELOPMENT_BUILD
            Debug.Log("[DEV] Game initialized");
        #endif
        if (targetSpawner == null)
        {
            Debug.LogError("spawner hello?????????????");
            enabled = false;
            return;
        }

        highScore = 0;
        ReturnToMenu();
    }

    public void PlaySolo()
    {
        if (!WebSitelock.IsAllowed)
            return;
        if (RoamingSaveManager.Instance != null &&
            RoamingSaveManager.Instance.HasIntegrityFailure)
        {
            Debug.LogWarning(
                "[DRM] Local save failed integrity verification. " +
                "Connect to the cloud before playing."
            );
            return;
        }
        if (ProfileMenuController.Instance == null ||
            !ProfileMenuController.Instance.IsLoggedIn)
            return;
        if (state != RoundState.Menu)
            return;
        if (LicenseGate.Instance == null ||
            !LicenseGate.Instance.IsLicensed)
            return;
        BeginRound();
    }

    public void RestartRound()
    {
        if (!WebSitelock.IsAllowed)
            return;
        if (RoamingSaveManager.Instance != null &&
            RoamingSaveManager.Instance.HasIntegrityFailure)
        {
            Debug.LogWarning(
                "[DRM] Local save failed integrity verification. " +
                "Connect to the cloud before playing."
            );
            return;
        }
        if (state != RoundState.Finished)
            return;
        if (LicenseGate.Instance == null ||
            !LicenseGate.Instance.IsLicensed)
            return;
        BeginRound();
    }

    private void BeginRound()
    {
        #if DEVELOPMENT_BUILD
            Debug.Log("[DEV] Round started");
        #endif
        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }

        targetSpawner.StopSpawning();

        state = RoundState.Countdown;
        AchievementManager.Instance?.RoundStarted();
        score = 0;
        scoreTampered = false;
        scoreProtection.Set(0);

        if (cheatWarningText != null)
            cheatWarningText.gameObject.SetActive(false);
        timeRemaining = Mathf.Max(1f, roundDuration);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (hudPanel != null)
            hudPanel.SetActive(true);

        SetHammerVisible(false);
        UpdateHUD();

        roundRoutine = StartCoroutine(RunRound());
    }

    private IEnumerator RunRound()
    {
        for (int i = 3; i >= 1; i--)
        {
            countdownSeconds = i;
            UpdateHUD();

            yield return new WaitForSeconds(1f);
        }
        state = RoundState.Playing;

        LocalizedVoiceLine.Instance?.PlayRoundStart();

        SetHammerVisible(true);
        targetSpawner.StartSpawning();
        UpdateHUD();

        while (timeRemaining > 0f)
        {
            timeRemaining = Mathf.Max(
                0f,
                timeRemaining - Time.deltaTime
            );

            UpdateHUD();
            yield return null;
        }

        FinishRound();
        roundRoutine = null;
    }

    private void FinishRound()
    {
        VerifyScoreIntegrity();
        state = RoundState.Finished;
        if (!scoreTampered)
            AchievementManager.Instance?.RoundFinished(score);
        targetSpawner.StopSpawning();
        SetHammerVisible(false);

        if (!scoreTampered && score > highScore)
        {
            highScore = score;

            var profile = ProfileMenuController.Instance?.ActiveProfile;

            if (profile != null)
            {
                PlayerPrefs.SetInt(
                    LocalProfileStore.GetScoreKey(profile.id),
                    highScore
                );

                PlayerPrefs.Save();
                RoamingSaveManager.Instance?.SaveHighScore(highScore);
            }
        }

        UpdateResultTexts();

        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        UpdateHUD();
    }

    public void AddPoints(int points)
    {
        if (!IsPlaying)
            return;

        VerifyScoreIntegrity();

        if (scoreTampered)
            return;

        score = Mathf.Max(0, score + points);
        scoreProtection.Set(score);
        #if DEVELOPMENT_BUILD
            Debug.Log($"[DEV] Score changed: {points:+#;-#;0}, Total: {score}");
        #endif
        UpdateHUD();
    }

    public void ReturnToMenu()
    {

        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (roundRoutine != null)
        {
            StopCoroutine(roundRoutine);
            roundRoutine = null;
        }

        targetSpawner.StopSpawning();

        state = RoundState.Menu;
        score = 0;
        timeRemaining = roundDuration;

        SetHammerVisible(false);

        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (hudPanel != null)
            hudPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    private void UpdateHUD()
    {
        if (state == RoundState.Playing)
            VerifyScoreIntegrity();
        if (scoreText != null)
        {
            scoreText.text = L(
                "HUD_SCORE", "SCORE: {0:N0}", score
            );
        }

        if (timeText != null)
        {
            timeText.text = state == RoundState.Countdown
                ? L("HUD_COUNTDOWN", "START IN: {0}", countdownSeconds)
                : L("HUD_TIME", "TIME: {0}",
                    Mathf.CeilToInt(timeRemaining));
        }
    }


    private void SetHammerVisible(bool visible)
    {
        if (hammerVisual != null)
            hammerVisual.SetActive(visible);
    }


    public void PauseRound()
    {
        if (state != RoundState.Playing)
            return;

        state = RoundState.Paused;
        Time.timeScale = 0f;
        #if DEVELOPMENT_BUILD
            Debug.Log("[DEV] Game paused");
        #endif
        SetHammerVisible(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeRound()
    {
        if (state != RoundState.Paused)
            return;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        state = RoundState.Playing;

        SetHammerVisible(true);
    }

    public void RestartFromPause()
    {
        if (state != RoundState.Paused)
            return;

        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        BeginRound();
    }

}

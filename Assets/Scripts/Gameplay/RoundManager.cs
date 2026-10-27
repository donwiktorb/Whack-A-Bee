
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
    private int score;
    private int highScore;

    public bool IsPlaying => state == RoundState.Playing;

    private void Update()
    {
        WhackInput input = WhackInput.Instance;

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

        highScore = PlayerPrefs.GetInt("SoloHighScore", 0);

        ReturnToMenu();
    }

    public void PlaySolo()
    {
        if (state != RoundState.Menu)
            return;

        BeginRound();
    }

    public void RestartRound()
    {
        if (state != RoundState.Finished)
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
        score = 0;
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
            if (timeText != null)
                timeText.text = $"START IN: {i}";

            yield return new WaitForSeconds(1f);
        }

        state = RoundState.Playing;

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
        state = RoundState.Finished;

        targetSpawner.StopSpawning();
        SetHammerVisible(false);

        if (score > highScore)
        {
            highScore = score;

            PlayerPrefs.SetInt("SoloHighScore", highScore);
            PlayerPrefs.Save();
        }

        if (resultScoreText != null)
            resultScoreText.text = $"SCORE: {score:N0}";

        if (resultBestText != null)
            resultBestText.text = $"BEST: {highScore:N0}";

        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        UpdateHUD();
    }

    public void AddPoints(int points)
    {
        if (!IsPlaying)
            return;

        score = Mathf.Max(0, score + points);
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
        if (scoreText != null)
            scoreText.text = $"SCORE: {score:N0}";

        if (timeText != null)
            timeText.text =
                $"TIME: {Mathf.CeilToInt(timeRemaining)}";
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

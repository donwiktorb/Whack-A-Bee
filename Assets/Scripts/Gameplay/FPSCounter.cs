
using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCounter : MonoBehaviour
{
    private int frames;
    private float elapsedTime;
    private float fps;
    private bool visible = true;

    private bool measuring;
    private int benchmarkFrames;
    private float benchmarkTime;
    private float benchmarkFPS = -1f;

    private int previousVSync;
    private int previousFrameRate;

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;

        frames++;
        elapsedTime += delta;

        if (elapsedTime >= 0.5f)
        {
            fps = frames / elapsedTime;
            frames = 0;
            elapsedTime = 0f;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f3Key.wasPressedThisFrame)
                visible = !visible;

            if (keyboard.f4Key.wasPressedThisFrame && !measuring)
                StartBenchmark();
        }

        if (measuring)
        {
            benchmarkFrames++;
            benchmarkTime += delta;

            if (benchmarkTime >= 30f)
                FinishBenchmark();
        }
    }

    private void StartBenchmark()
    {
        previousVSync = QualitySettings.vSyncCount;
        previousFrameRate = Application.targetFrameRate;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        benchmarkFrames = 0;
        benchmarkTime = 0f;
        benchmarkFPS = -1f;
        measuring = true;
    }

    private void FinishBenchmark()
    {
        benchmarkFPS = benchmarkFrames / benchmarkTime;
        measuring = false;

        QualitySettings.vSyncCount = previousVSync;
        Application.targetFrameRate = previousFrameRate;

        Debug.Log($"[BENCHMARK] Average FPS: {benchmarkFPS:F2}");
    }

    private void OnDisable()
    {
        if (!measuring) return;

        QualitySettings.vSyncCount = previousVSync;
        Application.targetFrameRate = previousFrameRate;
        measuring = false;
    }

    private void OnGUI()
    {
        if (!visible) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };

        GUI.Box(
            new Rect(10, 10, 260, 40),
            $"{fps:F0} FPS ({1000f / Mathf.Max(fps, 0.01f):F2} ms)",
            style
        );

        string result = measuring
            ? $"MEASURING: {benchmarkTime:F1}/30s"
            : benchmarkFPS >= 0f
                ? $"AVERAGE: {benchmarkFPS:F2} FPS"
                : "F4 - START BENCHMARK";

        GUI.Box(
            new Rect(10, 55, 260, 40),
            result,
            style
        );
    }
}

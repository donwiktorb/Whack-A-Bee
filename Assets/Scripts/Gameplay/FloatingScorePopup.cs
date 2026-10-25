
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class FloatingScorePopup : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float lifetime = 0.65f;
    [SerializeField] private float riseSpeed = 90f;

    private TMP_Text scoreText;
    private Color originalColor;
    private float elapsed;

    private void Awake()
    {
        scoreText = GetComponent<TMP_Text>();
    }

    public void Initialize(int points)
    {
        scoreText.text = points > 0
            ? $"+{points}"
            : points.ToString();

        originalColor = points > 0
            ? new Color(1f, 0.82f, 0.2f, 1f)
            : new Color(1f, 0.3f, 0.3f, 1f);

        scoreText.color = originalColor;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        transform.position +=
            Vector3.up * riseSpeed * Time.unscaledDeltaTime;

        float progress = Mathf.Clamp01(
            elapsed / Mathf.Max(0.01f, lifetime)
        );

        Color color = originalColor;
        color.a = 1f - progress;

        scoreText.color = color;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}

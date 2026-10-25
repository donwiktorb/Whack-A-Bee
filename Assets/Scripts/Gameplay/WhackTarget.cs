
using System.Collections;
using UnityEngine;

public class WhackTarget : MonoBehaviour
{
    [Header("Target Type")]
    [SerializeField] private bool isBomb = false;

    [Header("Pop Animation")]
    [SerializeField] private float hiddenY = -0.9f;
    [SerializeField] private float visibleY = 0.45f;

    [SerializeField] private float riseTime = 0.25f;
    [SerializeField] private float stayTime = 1.2f;
    [SerializeField] private float fallTime = 0.2f;

    private bool canBeHit;
    private bool wasHit;
    private Coroutine popCoroutine;


    
    public int MultiplayerBeat { get; private set; } = -1;
    public void Begin()
    {
        canBeHit = false;
        wasHit = false;

        popCoroutine = StartCoroutine(PopRoutine());
    }

    public bool TryHit(out int points)
    {
        points = 0;

        if (!canBeHit || wasHit)
            return false;

        wasHit = true;
        canBeHit = false;

        points = isBomb ? -200 : 100;

        if (popCoroutine != null)
            StopCoroutine(popCoroutine);

        StartCoroutine(HitRoutine());

        return true;
    }

    private IEnumerator PopRoutine()
    {
        transform.localPosition = Vector3.up * hiddenY;

        yield return MoveY(hiddenY, visibleY, riseTime);

        canBeHit = true;

        yield return new WaitForSeconds(stayTime);

        canBeHit = false;

        yield return MoveY(visibleY, hiddenY, fallTime);

        Destroy(gameObject);
    }

    private IEnumerator HitRoutine()
    {
        float currentY = transform.localPosition.y;

        yield return MoveY(currentY, hiddenY, 0.15f);

        Destroy(gameObject);
    }

    private IEnumerator MoveY(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition =
                new Vector3(0f, Mathf.Lerp(from, to, t), 0f);

            yield return null;
        }

        transform.localPosition = new Vector3(0f, to, 0f);
    }

    public void SetMultiplayerBeat(int beat)
    {
        MultiplayerBeat = beat;
    }
}

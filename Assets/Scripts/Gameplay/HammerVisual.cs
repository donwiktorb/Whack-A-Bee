
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HammerVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform impactPoint;

    [Header("Cursor")]
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private Vector2 screenOffset = Vector2.zero;

    [Header("Resting Orientation")]
    [SerializeField] private Vector3 restingRotation =
        new Vector3(20f, -15f, -160f);

    [Header("Strike Animation")]
    [SerializeField, Range(0f, 80f)]
    private float liftAngle = 35f;

    [SerializeField] private float swingDownTime = 0.09f;
    [SerializeField] private float swingUpTime = 0.18f;

    private float currentLift;
    private Coroutine swingCoroutine;

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        currentLift = liftAngle;
    }

    private void OnEnable()
    {
        currentLift = liftAngle;
    }

    private void LateUpdate()
    {
        if (gameCamera == null || impactPoint == null)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 mousePos = mouse.position.ReadValue();

        Vector3 targetScreen = new Vector3(
            mousePos.x + screenOffset.x,
            mousePos.y + screenOffset.y,
            cameraDistance
        );

        Vector3 targetWorld =
            gameCamera.ScreenToWorldPoint(targetScreen);

        Quaternion contactRotation =
            gameCamera.transform.rotation *
            Quaternion.Euler(restingRotation);

        transform.rotation = contactRotation;

        Vector3 headOffset =
            impactPoint.position - transform.position;

        Vector3 gripPosition = targetWorld - headOffset;

        Vector3 axis =
            Vector3.Cross(headOffset, Vector3.up);

        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.Cross(
                headOffset,
                gameCamera.transform.up
            );
        }

        if (axis.sqrMagnitude < 0.0001f)
            axis = gameCamera.transform.right;

        axis.Normalize();

        transform.position = gripPosition;

        transform.rotation =
            Quaternion.AngleAxis(currentLift, axis) *
            contactRotation;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (swingCoroutine != null)
                StopCoroutine(swingCoroutine);

            swingCoroutine = StartCoroutine(Swing());
        }
    }

    private IEnumerator Swing()
    {
        yield return AnimateLift(
            currentLift, 0f, swingDownTime
        );

        yield return AnimateLift(
            0f, liftAngle, swingUpTime
        );

        swingCoroutine = null;
    }

    private IEnumerator AnimateLift(
        float from, float to, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.001f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            currentLift = Mathf.Lerp(from, to, t);
            yield return null;
        }

        currentLift = to;
    }

    private void OnDisable()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        currentLift = liftAngle;
    }
}

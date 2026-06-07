using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    // Since the camera is a child, its natural baseline center should always be 0 local X and Y
    private const float BASELINE_X = 0f;
    private const float BASELINE_Y = 0f;
    private float cameraZ;

    private Coroutine activeEffectsRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Remember the camera's baseline depth distance so we don't overwrite it
        cameraZ = transform.localPosition.z;
    }

    /// <summary>
    /// Standard Basara multi-hit chaotic rumble for cutting down massive lines of grunts.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (activeEffectsRoutine != null) StopCoroutine(activeEffectsRoutine);
        activeEffectsRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    /// <summary>
    /// Violent directional punch. Snaps the camera forward in the swing direction, then vibrates back.
    /// </summary>
    public void HitPunch(Vector2 attackDirection, float pushDistance, float duration)
    {
        if (activeEffectsRoutine != null) StopCoroutine(activeEffectsRoutine);
        activeEffectsRoutine = StartCoroutine(PunchRoutine(attackDirection.normalized, pushDistance, duration));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Calculate a random offset relative to the zeroed child center
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(BASELINE_X + x, BASELINE_Y + y, cameraZ);

            elapsed += Time.unscaledDeltaTime; // Process even when hit-lag is hard freezing the world
            yield return null;
        }

        // Snap safely back to dead center
        transform.localPosition = new Vector3(BASELINE_X, BASELINE_Y, cameraZ);
    }

    private IEnumerator PunchRoutine(Vector2 dir, float distance, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;

            // Violent initial snap forward, fading out quickly over time
            float currentDistance = Mathf.Lerp(distance, 0f, progress);

            // Add a frantic micro-vibration on top of the directional push
            float microShakeX = Random.Range(-0.06f, 0.06f) * (1f - progress);
            float microShakeY = Random.Range(-0.06f, 0.06f) * (1f - progress);

            float finalX = BASELINE_X + (dir.x * currentDistance) + microShakeX;
            float finalY = BASELINE_Y + (dir.y * currentDistance) + microShakeY;

            transform.localPosition = new Vector3(finalX, finalY, cameraZ);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Snap safely back to dead center
        transform.localPosition = new Vector3(BASELINE_X, BASELINE_Y, cameraZ);
    }
}
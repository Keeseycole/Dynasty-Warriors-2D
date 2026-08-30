using UnityEngine;
using UnityEngine.UI;

public class DamageNumber : MonoBehaviour
{
    [Header("Visual Assignments")]
    [SerializeField] private Text txtDisplay;

    [Header("Arcade Motion Curves")]
    public float upwardVelocity = 1f;
    public float lifetimeDuration = 0.8f;

    [Header("RPG Turn-Based Bounce Settings")]
    [Tooltip("How far sideways the number explosively snaps on impact")]
    public float bounceWidth = 0.5f;
    [Tooltip("How fast it bounces back and forth (Higher = snappier cycles)")]
    public float bounceFrequency = 22.0f;
    [Tooltip("How fast the back-and-forth spring effect dampens down to zero")]
    public float bounceDecaySpeed = 5.0f;

    private float currentAge = 0f;
    private Color startingColor;
    private Vector2 baselineWorldPosition;
    private float sideDirectionChoice = 1f; // Alternates left or right for text variance

    public void InitializePopup(int damageAmount, Color preferredColor)
    {
        if (txtDisplay == null) txtDisplay = GetComponentInChildren<Text>();
        if (txtDisplay == null) return;

        txtDisplay.horizontalOverflow = HorizontalWrapMode.Overflow;
        txtDisplay.verticalOverflow = VerticalWrapMode.Overflow;
        txtDisplay.alignment = TextAnchor.MiddleCenter;

        txtDisplay.text = damageAmount.ToString();
        startingColor = preferredColor;
        txtDisplay.color = startingColor;

        baselineWorldPosition = transform.position;
        currentAge = 0f;

        // RPG Variance: Randomly bounce left or right so stacked hits don't overlap completely
        sideDirectionChoice = Random.value > 0.5f ? 1f : -1f;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        currentAge += Time.deltaTime;

        // 1. Move our tracking baseline point steadily upward over time
        baselineWorldPosition += Vector2.up * upwardVelocity * Time.deltaTime;

        // ========================================================================
        // 🟩 THE RPG TURN-BASED SPRING BOUNCE (FIXED MATH):
        // Uses a decaying sine wave to create a snappy, rhythmic spring bounce vector
        // that bounces outward horizontally and dampens cleanly to a stop!
        // ========================================================================
        float bounceDampening = Mathf.Exp(-bounceDecaySpeed * currentAge); // Smooth exponential drop
        float horizontalBounceOffset = Mathf.Sin(currentAge * bounceFrequency) * bounceWidth * bounceDampening * sideDirectionChoice;

        // Offset only the horizontal axis cleanly to mimic classic RPG combat weight
        Vector2 structuralBounceVector = new Vector2(horizontalBounceOffset, 0f);

        // Apply our smooth vertical rise and RPG elastic spring together perfectly
        transform.position = baselineWorldPosition + structuralBounceVector;

        // 3. SMOOTH VISUAL FADE
        float lifeRatio = currentAge / lifetimeDuration;
        txtDisplay.color = Color.Lerp(startingColor, new Color(startingColor.r, startingColor.g, startingColor.b, 0f), lifeRatio);

        if (currentAge >= lifetimeDuration)
        {
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class DamageNumberFactory : MonoBehaviour
{
    public static DamageNumberFactory Instance;

    [Header("Prefab References")]
    [Tooltip("Drag your legacy Canvas Text prefab asset template here!")]
    [SerializeField] private GameObject damageTextPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Instantiates a fresh world-space canvas text instance exactly where your sword slices!
    /// </summary>
    public void BurstDamageNumber(int damageAmount, Vector2 worldSpaceSpawn, Color numbersColor)
    {
        if (damageTextPrefab == null) return;

        // 🔥 INSTANTIATE PASS: Spawns the clean legacy text canvas entity
        GameObject textClone = Instantiate(damageTextPrefab, worldSpaceSpawn, Quaternion.identity);

        DamageNumber textScript = textClone.GetComponent<DamageNumber>();
        if (textScript != null)
        {
            textScript.InitializePopup(damageAmount, numbersColor);
        }
    }
}
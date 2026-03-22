using UnityEngine;
using TMPro;
using UnityEngine.UI; // Use TextMeshPro for high-quality Musou-style text

public class KOCounter : MonoBehaviour
{
    public static KOCounter instance; // Singleton for easy access from any unit
    public Text koText;
    private int totalKOs = 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void AddKO()
    {
        totalKOs++;
        UpdateUI();

        // Optional: Trigger a "Morale Boost" or special effect every 50/100 KOs
        if (totalKOs % 50 == 0)
        {
            Debug.Log("Morale is rising!");
        }
    }

    void UpdateUI()
    {
        // Format it like the games: "1234 K.O."
        koText.text = totalKOs.ToString() + " K.O.";
    }
}

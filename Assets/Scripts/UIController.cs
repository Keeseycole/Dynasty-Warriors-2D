using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UIController : MonoBehaviour
{

    public static UIController instance;

    public Slider levelSlider;

    public TMP_Text expLevelText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
       instance = this; 
    }

    private void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateExp(int currentExp, int levelExp, int currentLevel)
    {
        levelSlider.maxValue = levelExp;
        levelSlider.value = currentExp;

        expLevelText.text = "Level: " + currentLevel;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectManager : MonoBehaviour
{
    public static CharacterSelectManager Instance { get; private set; }

    [Header("All Playable Characters")]
    public CharacterData[] availableCharacters;

    // Tracks the active selection index
    private int currentSelectionIndex = 0;

    private void Awake()
    {
        // Singleton pattern to keep this manager alive between scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public CharacterData GetSelectedCharacter()
    {
        return availableCharacters[currentSelectionIndex];
    }


    public void UpdateIndex(int newIndex)
    {
        if (newIndex >= 0 && newIndex < availableCharacters.Length)
        {
            currentSelectionIndex = newIndex; // Or whatever your index variable name is!
        }
    }

    public void LoadGameScene(string sceneName)
    {
        // Explicitly clear parent references if this object was holding anything
        transform.DetachChildren();

        SceneManager.LoadScene(sceneName);
    }

    // 🔥 NEW SINGLETON BRIDGE: Call this to force your UI sliders to watch the newly spawned character instance
    public void BindActivePlayerToUI(PlayerCombo dynamicPlayerInstance)
    {
        if (dynamicPlayerInstance == null) return;

        // 1. Find the Musou Slider in the current active combat scene
        GameObject musouGo = GameObject.Find("Musou Slider");
        if (musouGo == null) musouGo = GameObject.FindWithTag("MusouBar");

        if (musouGo != null)
        {
            UnityEngine.UI.Slider activeSlider = musouGo.GetComponent<UnityEngine.UI.Slider>();
            if (activeSlider != null)
            {
                // Set the slider bounds dynamically based on who just spawned
                activeSlider.maxValue = dynamicPlayerInstance.maxMusouEnergy;
                activeSlider.value = dynamicPlayerInstance.currentMusouEnergy;

                // Direct assignment back to the clone's runtime tracking variables
                dynamicPlayerInstance.myNativeAnimator = dynamicPlayerInstance.GetComponent<Animator>();

                Debug.Log($"<color=#00FF88>[UI CROSS-BIND SUCCESS]:</color> Musou UI successfully rerouted to active clone instance: <b>{dynamicPlayerInstance.gameObject.name}</b>");
            }
        }
    }
}
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
}
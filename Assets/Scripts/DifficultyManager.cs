using UnityEngine;

public enum DifficultyLevel { Easy, Normal, Hard, Chaos }


public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [Header("Game Difficulty")]
    public DifficultyLevel currentDifficulty = DifficultyLevel.Normal;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}

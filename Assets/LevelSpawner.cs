using UnityEngine;

public class LevelSpawner : MonoBehaviour
{
    [Header("Player Template")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        if (CharacterSelectManager.Instance == null)
        {
            Debug.LogWarning("No CharacterSelectManager found! Spawning default player configuration.");
            Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
            return;
        }

        // Get selection data
        CharacterData chosenChar = CharacterSelectManager.Instance.GetSelectedCharacter();

        // Spawn player instance
        GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);

        // Inject the selected character's unique settings
        Animator anim = playerObj.GetComponent<Animator>();
        if (anim != null)
        {
            anim.runtimeAnimatorController = chosenChar.animatorController;
        }

        PlayerController controller = playerObj.GetComponent<PlayerController>();
        if (controller != null)
        {
          //  controller.moveSpeed = chosenChar.baseMoveSpeed;
            // (If you add base damage or health hooks to PlayerController, apply them here!)
        }
    }
}

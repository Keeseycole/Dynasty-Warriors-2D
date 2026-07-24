using UnityEngine;

using System.Collections;

public class LevelSpawner : MonoBehaviour
{
    [Header("Player Template")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    private GameObject spawnedPlayer;

    private void Awake()
    {
        // 1. Spawn the player on Awake so their tag and layers exist BEFORE anything else runs Start()
        SpawnPlayerBody();
    }

    private void Start()
    {
        // 2. Wait until Start to link the enemies, giving the hierarchy a chance to settle
        StartCoroutine(LinkBattlefieldReferences());
    }

    private void SpawnPlayerBody()
    {
        if (CharacterSelectManager.Instance == null) return;

        CharacterData chosenChar = CharacterSelectManager.Instance.GetSelectedCharacter();
        if (chosenChar == null) return;

        GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        playerObj.tag = "Player";

        // 🔥 THE AUTOMATED OBJECT HOUSEKEEPING FORCE-RESET:
        // Forcefully locates and completely destroys any remnants of the enemy AI unit script 
        // on your newly spawned playable hero clone model, instantly freeing up the combat loop!
        MusouUnit rogueAIEngine = playerObj.GetComponent<MusouUnit>();
        if (rogueAIEngine == null) rogueAIEngine = playerObj.GetComponentInChildren<MusouUnit>();

        if (rogueAIEngine != null)
        {
            Destroy(rogueAIEngine);
            Debug.Log("<color=#FF007F>[LEVEL SPAWNER SAFETY]:</color> Rogue MusouUnit AI controller script forcefully unlinked from player clone instance.");
        }

        // 1. Inject Character Health Stats
        PlayerHealth playerHealthScript = playerObj.GetComponent<PlayerHealth>();
        if (playerHealthScript == null) playerHealthScript = playerObj.GetComponentInChildren<PlayerHealth>();
        if (playerHealthScript != null)
        {
            float universalMaxMenuCap = 250f;
            playerHealthScript.InitializeInjectedStats(chosenChar.maxHealth, universalMaxMenuCap);
        }

        // 🔥 2. THE WEAPON RANGE INJECTION:
        // Locate your combo script and feed it the unique weapon radius from our ScriptableObject asset!
        PlayerCombo comboScript = playerObj.GetComponent<PlayerCombo>();
        if (comboScript == null) comboScript = playerObj.GetComponentInChildren<PlayerCombo>();
        if (comboScript != null)
        {
            comboScript.InitializeCharacterRange(chosenChar.uniqueAttackRadius);
        }

        // 3. Setup animator & controllers
        Animator anim = playerObj.GetComponentInChildren<Animator>();
        if (anim != null && chosenChar.animatorController != null)
        {
            anim.runtimeAnimatorController = chosenChar.animatorController;
            anim.Rebind();
        }

        PlayerController controller = playerObj.GetComponent<PlayerController>();
        if (controller != null) controller.currentState = PlayerState.idle;
    }
    private IEnumerator LinkBattlefieldReferences()
    {
        // Give Unity exactly one physics frame to register the player's colliders/layers
        yield return new WaitForFixedUpdate();

        if (spawnedPlayer == null) yield break;

        // Force all existing Musou units in the scene to bind to the new player transform
        MusouUnit[] allUnits = FindObjectsByType<MusouUnit>(FindObjectsSortMode.None);
        foreach (MusouUnit unit in allUnits)
        {
            unit.playerTransform = spawnedPlayer.transform;

            // Force a target refresh now that the player is safely spawned
            unit.FindNearestTarget();
        }
    }
}
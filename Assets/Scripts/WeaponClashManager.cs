using UnityEngine;
using System.Collections;

public class WeaponClashManager : MonoBehaviour
{
    public static WeaponClashManager Instance { get; private set; }

    [Header("Clash State Settings")]
    public bool isClashing = false;
    [Range(0f, 1f)] public float clashBalance = 0.5f;
    public float clashTimer = 0f;
    public float maxClashDuration = 3.5f;

    [Header("Balancing Multipliers")]
    public float playerMashPower = 0.06f;
    public float aiPushStrength = 0.15f;
    public float scaleDrainRate = 0.05f;

    [Header("Tracked Combatants")]
    // 🔥 THE REAL PLAYER CONVERSION: 
    // Replaced MusouUnit with your actual active Player script class!
    private PlayerController activePlayerUnit;
    private MusouUnit activeEnemyOfficer;

    public System.Action<float> OnClashValueUpdated;
    public System.Action<bool> OnClashStateChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Call this when a Player and Enemy Officer hitbox collide on the exact same frame!
    /// </summary>
    public void InitiateClash(PlayerController player, MusouUnit enemyOfficer)
    {
        if (isClashing) return;

        isClashing = true;
        clashBalance = 0.5f;
        clashTimer = maxClashDuration;

        activePlayerUnit = player;
        activeEnemyOfficer = enemyOfficer;

        SetBattlefieldFreeze(true);

        Debug.Log($"<color=orange>[CLASH TRIGGERED]</color> {player.gameObject.name} vs {enemyOfficer.gameObject.name}!");

        OnClashStateChanged?.Invoke(true);
        OnClashValueUpdated?.Invoke(clashBalance);
    }

    private void Update()
    {
        if (!isClashing) return;

        clashTimer -= Time.deltaTime;
        if (clashTimer <= 0f)
        {
            ResolveClashDraw();
            return;
        }

        float dynamicAiPower = aiPushStrength + ((maxClashDuration - clashTimer) * scaleDrainRate);
        clashBalance -= dynamicAiPower * Time.deltaTime;

        if (Input.GetButtonDown("Fire1"))
        {
            clashBalance += playerMashPower;
        }

        clashBalance = Mathf.Clamp01(clashBalance);
        OnClashValueUpdated?.Invoke(clashBalance);

        if (clashBalance >= 1f) ResolvePlayerVictory();
        else if (clashBalance <= 0f) ResolvePlayerDefeat();
    }

    private void ResolvePlayerVictory()
    {
        Debug.Log("<color=green>[CLASH SUCCESS]</color> Player overpowered the Enemy Officer!");

        if (activeEnemyOfficer != null)
        {
            activeEnemyOfficer.ChangeState(EnemyState.Stagger);
            if (activeEnemyOfficer.animator != null) activeEnemyOfficer.animator.SetTrigger("isStaggered");

        }

        EndClashSequence();
    }

    private void ResolvePlayerDefeat()
    {
        Debug.Log("<color=red>[CLASH FAILED]</color> Enemy Officer overpowered the Player!");

        // 🔥 PLAYER HITSTUN CALL: Triggers your player's hurt state/animator parameter
        if (activePlayerUnit != null)
        {
            Animator playerAnim = activePlayerUnit.GetComponent<Animator>();
            if (playerAnim == null) playerAnim = activePlayerUnit.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("isHit");
        }

        EndClashSequence();
    }

    private void ResolveClashDraw()
    {
        Debug.Log("<color=yellow>[CLASH DRAW]</color> Weapons ricocheted!");

        // Rebound velocities smoothly using available components
        if (activePlayerUnit != null)
        {
            Rigidbody2D pRb = activePlayerUnit.GetComponent<Rigidbody2D>();
            if (pRb == null) pRb = activePlayerUnit.GetComponentInChildren<Rigidbody2D>();
            if (pRb != null) pRb.linearVelocity = Vector2.left * 6f;
        }
        if (activeEnemyOfficer != null && activeEnemyOfficer.rb != null)
        {
            activeEnemyOfficer.rb.linearVelocity = Vector2.right * 6f;
        }

        EndClashSequence();
    }

    private void EndClashSequence()
    {
        isClashing = false;
        SetBattlefieldFreeze(false);
        OnClashStateChanged?.Invoke(false);

        activePlayerUnit = null;
        activeEnemyOfficer = null;
    }

    private void SetBattlefieldFreeze(bool freeze)
    {
        MusouUnit[] allUnits = FindObjectsByType<MusouUnit>(FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            if (unit == activeEnemyOfficer) continue; // Keep enemy duelist awake

            if (freeze)
            {
                unit.enabled = false;
                if (unit.rb != null) unit.rb.linearVelocity = Vector2.zero;
                if (unit.animator != null) unit.animator.speed = 0f;
            }
            else
            {
                unit.enabled = true;
                if (unit.animator != null) unit.animator.speed = 1f;
            }
        }
    }
}

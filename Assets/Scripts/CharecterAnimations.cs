using UnityEngine;

public class CharecterAnimations : MonoBehaviour
{
    private Animator anim;
    private PlayerCombo comboScript;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Update()
    {
        // Re-cache safety gate for runtime character selection instantiations
        if (anim == null || comboScript == null)
        {
            InitializeComponents();
        }
    }

    private void InitializeComponents()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        comboScript = GetComponent<PlayerCombo>();
        if (comboScript == null) comboScript = GetComponentInParent<PlayerCombo>();
    }

    // 🔥 THE COMPREHENSIVE RECOVERY GATE:
    // This flips active attack Bools back to false or resets Triggers,
    // safely dropping any character back into their movement blend trees!
    // Call this via Animation Event on the LAST FRAME of every attack clip
    public void AnimationFinished()
    {
        if (comboScript == null) InitializeComponents();

        if (comboScript != null)
        {
            // 🟢 REDIRECT: Instead of just clearing variables, pass control to the buffer logic!
            // This checks if the player pressed Z early and triggers the next strike instantly.
            comboScript.FinishAttack();

            // Safety release for the movement state tracking if no attack was queued up
            if (!comboScript.isAttacking)
            {
                PlayerController controller = GetComponentInParent<PlayerController>();
                if (controller == null) controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.currentState = PlayerState.idle;
                }
            }
        }
    }

    private void SetAttackState(string parameterName)
    {
        if (anim == null) return;

        // 🔥 THE IMMEDIATE ENTRY RESET:
        // Hard-clears all attack values for a single frame BEFORE setting the new state.
        // This breaks any looping animation traps and unlocks infinite combo sequencing!
        ResetAllAttackStates();

        if (HasParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            anim.SetBool(parameterName, true);
        }
        else
        {
            anim.SetTrigger(parameterName);
        }
    }

    public void ResetAllAttackStates()
    {
        if (anim == null) return;

        string[] attackParams = new string[]
        {
            TagManager.Attack_1_Trigger, // "attack1"
            TagManager.Attack_2_Trigger, // "attack2"
            TagManager.Attack_3_Trigger, // "attack3"
            TagManager.Attack_4_Trigger, // "attack4"
            TagManager.Attack_5_Trigger  // "attack5"
        };

        foreach (string p in attackParams)
        {
            if (string.IsNullOrEmpty(p)) continue;

            if (HasParameter(p, AnimatorControllerParameterType.Bool))
            {
                anim.SetBool(p, false); // Flips checkbox OFF
            }
            else
            {
                anim.ResetTrigger(p);
            }
        }

        // 🔥 THE GRAPH RESET OVERRIDE:
        // Force the animator engine to re-evaluate the active state transitions immediately!
        // This stops the character from displaying a stale final strike frame sprite.
        anim.Update(0f);
    }


    // Helper method to look into the active runtime animator parameter configuration
    private bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName && param.type == type) return true;
        }
        return false;
    }

    public void Attack1() { SetAttackState(TagManager.Attack_1_Trigger); }
    public void Attack2() { SetAttackState(TagManager.Attack_2_Trigger); }
    public void Attack3() { SetAttackState(TagManager.Attack_3_Trigger); }
    public void Attack4() { SetAttackState(TagManager.Attack_4_Trigger); }
    public void Attack5() { SetAttackState(TagManager.Attack_5_Trigger); }

    public void Charge1() { SetAttackState("charge1"); }
    public void Charge2() { SetAttackState("charge2"); }
    public void Charge3() { SetAttackState("charge3"); }
    public void Charge4() { SetAttackState("charge4"); }
    public void Charge5() { SetAttackState("charge5"); }
}
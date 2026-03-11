using UnityEngine;

public class CharecterAnimations : MonoBehaviour
{

    private Animator anim;

    private PlayerCombo comboScript;

  
    // Call this via Animation Event on the LAST FRAME of every attack clip
  

    private void Awake()
    {
        anim= GetComponent<Animator>();
        comboScript = GetComponentInParent<PlayerCombo>();
    }

    public void AnimationFinished()
    {
        if (comboScript != null)
        {
            comboScript.isAttacking = false;
            // Optionally tell the combo script it's okay to accept the next input
        }
    }

    private void ResetAllAttackTriggers()
    {
        anim.ResetTrigger(TagManager.Attack_1_Trigger);
        anim.ResetTrigger(TagManager.Attack_2_Trigger);
        anim.ResetTrigger(TagManager.Attack_3_Trigger);
        anim.ResetTrigger(TagManager.Attack_4_Trigger);
        anim.ResetTrigger(TagManager.Attack_5_Trigger);
    }

    public void Attack1()
    {
        ResetAllAttackTriggers();
        anim.SetTrigger(TagManager.Attack_1_Trigger);
    }

    public void Attack2()
    {
        ResetAllAttackTriggers();
        anim.SetTrigger(TagManager.Attack_2_Trigger);
    }

    public void Attack3()
    {
        ResetAllAttackTriggers();
        anim.SetTrigger(TagManager.Attack_3_Trigger);
    }

    public void Attack4()
    {
        ResetAllAttackTriggers();
        anim.SetTrigger(TagManager.Attack_4_Trigger);
    }

    public void Attack5()
    {
        ResetAllAttackTriggers();
        anim.SetTrigger(TagManager.Attack_5_Trigger);
    }



}

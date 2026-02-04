using UnityEngine;

public class CharecterAnimations : MonoBehaviour
{

    private Animator anim;

  

    private void Awake()
    {
        anim= GetComponent<Animator>();
 
    }
    public void Attack1()
    {
        anim.SetTrigger(TagManager.Attack_1_Trigger);
    }

    public void Attack2()
    {
        anim.SetTrigger(TagManager.Attack_2_Trigger);
    }

    public void Attack3()
    {
        anim.SetTrigger(TagManager.Attack_3_Trigger);
    }

    public void Attack4()
    {
        anim.SetTrigger(TagManager.Attack_4_Trigger);
    }

    public void Attack5()
    {
        anim.SetTrigger(TagManager.Attack_5_Trigger);
    }

  
    public void EnemyAttack(int attack)
    {
        if(attack == 0)
        {
            anim.SetTrigger(TagManager.Attack_1_Trigger);
        }

        if (attack == 1)
        {
            anim.SetTrigger(TagManager.Attack_2_Trigger);
        }


        if (attack == 2)
        {
            anim.SetTrigger(TagManager.Attack_3_Trigger);
        }


    }

    public void PlayEnemyHitAnimation()
    {
        anim.Play(TagManager.Hit_Trigger);
    }

    public void PlayIdleAnimation()
    {
        anim.Play(TagManager.Idle_Trigger);
    }

}

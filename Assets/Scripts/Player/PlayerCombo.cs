using UnityEngine;
using UnityEngine.InputSystem;
public enum ComboState
{
    None,
    Attack1,
    Attack2, 
    Attack3, 
    Attack4,
    Attack5

}
public class PlayerCombo : MonoBehaviour
{
    PlayerState playerState;

    PlayerController playerController;


    private CharecterAnimations attackAnim;

    private bool ActivateresetTimer;

    private float defultComboTimer = .6f;

    private float currentComboTimer;

    private ComboState currentComboState;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        attackAnim= GetComponent<CharecterAnimations>();

        PlayerController playerController = GetComponent<PlayerController>();
    }


    private void Start()
    {
        currentComboTimer = defultComboTimer;

        currentComboState = ComboState.None;
    }

    // Update is called once per frame
    void Update()
    {
        ComboAttacks();
        ResetComboState();
    }

    public void ComboAttacks()
    {

        
        if (Input.GetKeyDown(KeyCode.Z))
        {
          
            currentComboState++;

            ActivateresetTimer = true;

            currentComboTimer = defultComboTimer;

            if(currentComboState == ComboState.Attack1)
            {
                attackAnim.Attack1();
            }


            if (currentComboState == ComboState.Attack2)
            {
                attackAnim.Attack2();
            }

            if (currentComboState == ComboState.Attack3)
            {
                attackAnim.Attack3();
            }

            if (currentComboState == ComboState.Attack4)
            {
                attackAnim.Attack4();
            }

            if (currentComboState == ComboState.Attack5)
            {
                attackAnim.Attack5();
            }
        }
      
    }
   public void ResetComboState()
    {
        if (ActivateresetTimer)
        {
            currentComboTimer -= Time.deltaTime;

            if (currentComboTimer <= 0)
            {
                currentComboState = ComboState.None;

                ActivateresetTimer = false;

                currentComboTimer = defultComboTimer;
            }
        }
    }
}

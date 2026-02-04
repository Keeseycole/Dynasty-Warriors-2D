using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;



public enum PlayerState
{
    walk,
    attack,
    idle,
    stagger
}
public class PlayerController : MonoBehaviour 
{

    public PlayerState currentState;

    [Header("movement")]

    public float moveSpeed;

    [Header("Animation")]

    [SerializeField] bool snapToDirAnim = true;

    private Animator anim;

    Vector3 moveInput = new Vector3(0f, 0f, 0f);

    private Vector2 lastLookDir;

    public float pickupRange = 1.5f;

    private void Awake()
    {
        anim= GetComponent<Animator>();
    }
 
    private void Update()
    {
        Move();

        if (currentState == PlayerState.walk || currentState == PlayerState.idle)
        {
            AnimateChar();
        }     
    }

    private void AnimateChar()
    {

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        Vector2 animDir = moveInput;

        if (isMoving)
        {
            if (snapToDirAnim)
            {
                if (Mathf.Abs(animDir.x) >= Mathf.Abs(animDir.y))
                {
                    animDir = new Vector2(Mathf.Sign(animDir.x), 0f);
                }
                else
                {
                    animDir = new Vector2(0f, Mathf.Sign(animDir.y));
                }
            }
            else
            {
              animDir = animDir.normalized;
            }

            lastLookDir = animDir;
        } 
        else
        {
           animDir = lastLookDir;
        }

        anim.SetBool("isMoving", isMoving);
        anim.SetFloat("moveX", animDir.x);
        anim.SetFloat("moveY", animDir.y);

    }
     public void Move()
    {
      
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        moveInput.Normalize();

        transform.position += moveInput * moveSpeed * Time.deltaTime;
    }
}
using UnityEngine;

public class ExpPickup : MonoBehaviour
{

    public int expValue;

    private bool isMovingtoPlayer;

    public float moveSpeed;

    public float timeBetweenCheck = .2f;

    private float CheckCounter;

    public PlayerController playerController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isMovingtoPlayer == true)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerController.transform.position, moveSpeed * Time.deltaTime);
        }
        else
        {
            CheckCounter = Time.deltaTime;

            if (CheckCounter <= 0)
            {
                CheckCounter = timeBetweenCheck;

                if (Vector3.Distance(transform.position, playerController.transform.position) < playerController.pickupRange)
                {

                    isMovingtoPlayer = true;

                    moveSpeed += playerController.moveSpeed;

                }
            }
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            ExperianceController.instance.GetExp(expValue);

            Destroy(gameObject);
        }
    }
}

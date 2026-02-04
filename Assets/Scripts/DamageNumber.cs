using UnityEngine;


public class DamageNumber : MonoBehaviour
{

    public float lifetime;
    private float lifeCounter;

    public float floatSpeed;

  

    // Update is called once per frame
    void Update()
    {
        if (lifeCounter > 0) 
        {

            lifeCounter -= Time.deltaTime;

            if (lifeCounter <= 0)
            {

                //Destroy(gameObject);

                DamageNumberController.instance.placeinPool(this);

            }

        }

        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            
    }

    public void setUp(int damageDesplay)
    {
        lifeCounter = lifetime;

        //damageText.text = damageDesplay.ToString();
    }
}

using UnityEngine;
using System.Collections.Generic;
public class DestroyOverTime : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, .3f);
    }

 
}

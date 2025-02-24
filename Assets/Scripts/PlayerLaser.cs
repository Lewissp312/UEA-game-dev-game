// using System.Collections;
// using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;

public class PlayerLaser : MonoBehaviour
{
    private Vector3 fireDirection;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += fireDirection * Time.deltaTime;
    }

    public void SetFireDirection(Vector3 fireDirection){
        this.fireDirection = fireDirection;
    }
}

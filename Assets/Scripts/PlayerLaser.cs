// using System.Collections;
// using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;

public class PlayerLaser : MonoBehaviour
{
    private int speed;
    private Vector3 enemyToAttackPosition;
    private Vector3 fireDirection;
    // Start is called before the first frame update
    void Start()
    {
        speed = 15;
        fireDirection = (enemyToAttackPosition - transform.position).normalized * speed;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += fireDirection * Time.deltaTime;
    }

    public void SetEnemyToAttack(Vector3 enemyToAttackPosition){
        this.enemyToAttackPosition = enemyToAttackPosition;
    }
}

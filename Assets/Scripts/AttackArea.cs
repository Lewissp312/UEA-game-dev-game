using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class AttackArea : MonoBehaviour
{
    private PlayerController parentScript;
    // Start is called before the first frame update
    void Start()
    {
        parentScript = transform.parent.gameObject.GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        print(collision);
        // Debug.Log($"Collision: {collision}");
        if (collision.gameObject.CompareTag("Enemy")){
            Debug.Log("Enemy collision");
        }
    }

    void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("Enemy")){
            int enemyNum = other.gameObject.GetComponent<EnemyController>().GetEnemyID();
            parentScript.AddToEnemyList(enemyNum,other.gameObject);
        }
    }

    void OnTriggerExit(Collider other){
        if (other.gameObject.CompareTag("Enemy")){
            EnemyController enemyScript = other.gameObject.GetComponent<EnemyController>();
            int enemyNum = enemyScript.GetEnemyID();
            parentScript.RemoveFromEnemyList(enemyNum);
            enemyScript.StopAttackingPlayer(transform.parent.gameObject);
        }
    }
}

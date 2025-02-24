using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AttackArea : MonoBehaviour
{
    private CubeMovement parentScript;
    // Start is called before the first frame update
    void Start()
    {
        parentScript = transform.parent.gameObject.GetComponent<CubeMovement>();
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
        print(other);
        if (other.gameObject.CompareTag("Enemy")){
            print("Enemy Collision");
            int enemyNum = other.gameObject.GetComponent<Enemy>().GetEnemyID();
            parentScript.AddToEnemyList(enemyNum,other.gameObject);
            // Debug.Log($"Enemy number: {other.gameObject.GetComponent<Enemy>().GetEnemyNum()}");
        }
        //Could have some sort of dictionary to store the other collisions, passing that information from here
    }

    void OnTriggerExit(Collider other){
        if (other.gameObject.CompareTag("Enemy")){
            int enemyNum = other.gameObject.GetComponent<Enemy>().GetEnemyID();
            parentScript.RemoveFromEnemyList(enemyNum);
            // Debug.Log($"Enemy number: {other.gameObject.GetComponent<Enemy>().GetEnemyNum()}");
        }
        //have code to remove the item from the dictionary here, this will call a method in player script 
        // that uses some sort of ID to get rid of the entry
    }
}

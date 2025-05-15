using UnityEngine;

/// <summary>
/// Controls the behaviour for the yellow areas around players. This area handles keeping track of enemies near the player
/// </summary>
public class AttackArea : MonoBehaviour
{
    private PlayerController parentScript;
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Unity methods
    void Start()
    {
        parentScript = transform.parent.gameObject.GetComponent<PlayerController>();
    }

    void OnCollisionEnter(Collision collision)
    {
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

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private int enemyID;
    private int timesHit;
    private bool isDead;
    private bool canTakeDamageFromMelee;
    private GameManager gameManager;
    private Animator enemyAnim;
    // Start is called before the first frame update
    void Start()
    {
        canTakeDamageFromMelee = true;
        timesHit = 0;
        enemyAnim = GetComponent<Animator>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        enemyID = gameManager.GetEnemyID();
        gameManager.IncreaseEnemyNum();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public int GetEnemyID(){
        return enemyID;
    }

    // public Vector3 GetCurrentPosition(){
    //     return transform.position;
    // }

    public void OnTriggerEnter(Collider other){
        if ((other.gameObject.CompareTag("Fist") && canTakeDamageFromMelee) || other.gameObject.CompareTag("Laser")){
            // print($"Collided with fist, ouch");
            timesHit++;
            if (timesHit%3 == 0){
                print("Knocked back");
                //TODO: insert effect
                enemyAnim.SetTrigger("Death_trig");
                isDead = true;
                StartCoroutine(WaitForDeath());
            }
            if (other.gameObject.CompareTag("Fist")){
                canTakeDamageFromMelee = false;
                StartCoroutine(WaitToTakeMeleeDamage());
            }
            if (other.gameObject.CompareTag("Laser")){
                Destroy(other.gameObject);
            }
        }
    }

    public bool GetIsDead(){
        return isDead;
    }

    IEnumerator WaitToTakeMeleeDamage(){
        yield return new WaitForSeconds(0.5f);
        canTakeDamageFromMelee = true;
    }

    IEnumerator WaitForDeath(){
        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }

}
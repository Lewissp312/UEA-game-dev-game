using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private int enemyID;
    private int timesHit;
    private bool isDead;
    private GameManager gameManager;
    private Animator enemyAnim;
    // Start is called before the first frame update
    void Start()
    {
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
        if (other.gameObject.CompareTag("Fist")){
            print($"Collided with fist, ouch");
            timesHit++;
            if (timesHit%3 == 0){
                print("Knocked back");
                enemyAnim.SetTrigger("Death_trig");
                isDead = true;
                StartCoroutine(WaitForDeath());
            }
        }
    }

    public bool GetIsDead(){
        return isDead;
    }

    IEnumerator WaitForDeath(){
        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }

}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private bool isOverPlayer;
    private bool isMovingToPosition;
    private bool isPlayerClicked;
    private bool isLockedOntoEnemy;
    private bool isMovingToEnemy;
    private bool canAttack;
    private enum PlayerType{MELEE,SMALL_RANGED}
    private int enemyToAttackID;
    private int points;
    private Animator playerAnim;
    private BoxCollider leftHandCollider;
    private BoxCollider rightHandCollider;
    private Color ringColor;
    private Color ringColorTrans;
    private Dictionary<int,GameObject> enemyInfo;
    private EnemyController enemyToAttackScript;
    private GameManager gameManager;
    private GameObject enemyToAttack;
    private ParticleSystem selectedEffect; 
    private Quaternion laserRotation;
    private Renderer ringRenderer;
    private Vector3 mousePosition;
    private Vector3 positionToMoveTo;
    private Vector3 lastGoodMousePos;
    private Vector3 laserPosition;
    private Vector3 laserHeight;
    private Vector3 enemyPositionForLaser;
    [SerializeField] private float attackCooldownTime;
    [SerializeField] private float playerSpeed;
    [SerializeField] private int health;
    [SerializeField] private GameObject laser;
    [SerializeField] private PlayerType playerType;
    // Start is called before the first frame update
    void Start()
    {
        canAttack = true;
        playerAnim = GetComponent<Animator>();
        //TODO: find better way to loop through children and grandchildren
        leftHandCollider = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).GetComponent<BoxCollider>();
        rightHandCollider = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).transform.GetChild(2).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).GetComponent<BoxCollider>();
        ringColor = new Color(0.96f,0.96f,0.51f,0.3f);
        ringColorTrans = new Color(0.96f,0.96f,0.51f,0f);
        enemyInfo = new();
        selectedEffect = GetComponent<ParticleSystem>();
        ringRenderer = transform.GetChild(2).gameObject.GetComponent<Renderer>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(3)){
            if (!isMovingToPosition){
                mousePosition = GetMouseOnBoardPosition(out isOverPlayer);
                if (isOverPlayer){
                    isPlayerClicked = !isPlayerClicked;
                    if (selectedEffect.isPlaying){
                        selectedEffect.Clear();
                        selectedEffect.Stop();
                    } else{
                        selectedEffect.Play();
                    }
                } else if (isPlayerClicked){
                    positionToMoveTo = mousePosition;
                    isMovingToPosition = true;
                    isLockedOntoEnemy = false;
                    isMovingToEnemy = false;
                    canAttack = false;
                    playerAnim.ResetTrigger("Punch_trig");
                    playerAnim.ResetTrigger("Shoot_small_trig");
                    playerAnim.SetTrigger("Run_trig");
                    isPlayerClicked = false;
                    selectedEffect.Clear();
                    selectedEffect.Stop();
                }
            }
        }

        if (!isMovingToPosition){ 
            if (isLockedOntoEnemy){
                if (isMovingToEnemy){
                    MoveToPosition(enemyToAttack.transform.position);
                    if (Vector3.Distance(transform.position,enemyToAttack.transform.position) <= 1.4f){
                        isMovingToEnemy = false;
                        playerAnim.ResetTrigger("Run_trig");
                        canAttack = true;
                    }
                } else if(canAttack){
                    if (enemyToAttackScript.GetIsDead() || enemyToAttack.IsDestroyed()){
                        canAttack = false;
                        isLockedOntoEnemy = false;
                        enemyInfo.Remove(enemyToAttackID);
                    } else if(
                            playerType == PlayerType.MELEE && 
                            Vector3.Distance(transform.position,enemyToAttack.transform.position) > 1.4f
                        ){
                        isMovingToEnemy = true;
                        canAttack = false;
                        playerAnim.SetTrigger("Run_trig");
                    } else{
                        transform.LookAt(enemyToAttack.transform.position);
                        AttackEnemy();
                    }
                }
            } else if (enemyInfo.Count > 0){
                enemyToAttack = GetClosestEnemy();
                if (enemyToAttack != gameObject){
                    enemyToAttackScript = enemyToAttack.GetComponent<EnemyController>();
                    enemyToAttackID = enemyToAttackScript.GetEnemyID();
                    isLockedOntoEnemy = true;
                    switch(playerType){
                        case PlayerType.MELEE:
                            isMovingToEnemy = true;
                            playerAnim.SetTrigger("Run_trig");
                            break;
                        case PlayerType.SMALL_RANGED:
                            canAttack = true;
                            break;
                    }
                    if (playerType == PlayerType.MELEE){
                        isMovingToEnemy = true;
                        playerAnim.SetTrigger("Run_trig");
                    }
                }
            }   
        }
        if (isMovingToPosition){
            MoveToPosition(positionToMoveTo);
            if (transform.position == positionToMoveTo){
                isMovingToPosition = false;
                playerAnim.ResetTrigger("Run_trig");
            }
        }     
    }

    private void OnMouseOver(){
        ringRenderer.material.color = ringColor;
        //245,245,131,128
    }

    private void OnMouseExit(){
        ringRenderer.material.color = ringColorTrans;
    }

    private void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("EnemyMelee")){
            gameManager.IncreaseEnemyPoints(1);
        }
    }

    private void OnCollisionEnter(Collision other){
    }

    private GameObject GetClosestEnemy(){
        float lowestDistance = 100;
        float tempDistance;
        int[] enemiesToRemove = new int[20];
        int enemiesToRemoveIndex = 0;
        GameObject lowestDistanceEnemy = gameObject;
        foreach(KeyValuePair<int, GameObject> enemy in enemyInfo){
            if (enemy.Value.IsDestroyed()){
                enemiesToRemove[enemiesToRemoveIndex] = enemy.Key;
                enemiesToRemoveIndex++;
            } else{
                tempDistance = Vector3.Distance(transform.position,enemy.Value.transform.position);
                if (tempDistance < lowestDistance){
                    lowestDistance = tempDistance;
                    lowestDistanceEnemy = enemy.Value;
                }
            }
        }
        if (enemiesToRemoveIndex > 0){
            foreach (int enemyID in enemiesToRemove){
                enemyInfo.Remove(enemyID);
            }
        }
        if (lowestDistance == 100){
            return gameObject;
        }
        return lowestDistanceEnemy;
    }

    public void AddToEnemyList(int enemyID, GameObject enemy){
        enemyInfo.Add(enemyID,enemy);
    }

    public void RemoveFromEnemyList(int enemyID){
        enemyInfo.Remove(enemyID);
    }

    private void MoveToPosition(Vector3 positionToMoveTo){
        transform.SetPositionAndRotation(
            Vector3.MoveTowards(
                transform.position,
                positionToMoveTo,
                playerSpeed
            ), 
            Quaternion.RotateTowards(
                transform.rotation, 
                Quaternion.LookRotation(
                    positionToMoveTo - transform.position
                ), 
                850 * Time.deltaTime
            )
        );
    }



    private Vector3 GetMouseOnBoardPosition(out bool isOverPlayer){
        Ray ray;
        ray =  Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 playerPos = transform.position;
        //If the ray hits a collider
        if(Physics.Raycast(ray,out RaycastHit hitData)){
            bool isFarEnough = Vector3.Distance(hitData.point, playerPos) > 1f;
            if (!hitData.collider.gameObject.CompareTag("Player") && isFarEnough){
                //needs to compare against all players to check that it didn't hit any
                //however, this makes the collision true for all players if it hits a player

                //The y position solution here which stops the player from going upwards would 
                //need to be fixed if the multiple levels are introduced.
                //The rigidbody use gravity function would probably help
                lastGoodMousePos = new(hitData.point.x,playerPos.y, hitData.point.z);
                isOverPlayer = false;
                return lastGoodMousePos;
            } else if (hitData.collider.gameObject == gameObject){
                print($"Collided with {hitData.collider.gameObject}");
                isOverPlayer = true;
                return lastGoodMousePos;
            } else{
                isOverPlayer = false;
                return lastGoodMousePos;
            }
        } 
        else{
            isOverPlayer = true;
            return lastGoodMousePos;
        }
    }

    private void AttackEnemy(){
        switch(playerType){
            case PlayerType.MELEE:
                //Checks if the enemy is either doing their death animation or the enemy object is destroyed
                //TODO: have different combat animations here, could set incrementing int variable to determine what attack to use
                playerAnim.SetTrigger("Punch_trig");
                //TODO: enable/disable certain colliders depending on what melee attack is being used 
                // (e.g right hand for normal punch, right leg for kick)
                // leftHandCollider.enabled = true;
                rightHandCollider.enabled = true;
                StartCoroutine(AttackCooldown());
                break;
            case PlayerType.SMALL_RANGED:
                transform.LookAt(enemyToAttack.transform.position);
                //start firing animation
                //TODO: change this to object pooling to be more effecient
                laserHeight = new Vector3(transform.position.x,transform.position.y + 2,transform.position.z);
                laserPosition = (transform.forward * 2) + laserHeight;
                laserRotation = transform.rotation * Quaternion.Euler(90,0,0);
                enemyPositionForLaser = new Vector3(enemyToAttack.transform.position.x,
                    enemyToAttack.transform.position.y + 2, 
                    enemyToAttack.transform.position.z
                );
                GameObject newLaser = Instantiate(laser,laserPosition,laserRotation);
                newLaser.GetComponent<PlayerLaser>().SetEnemyToAttack(enemyPositionForLaser);
                playerAnim.SetTrigger("Shoot_small_trig");
                StartCoroutine(AttackCooldown());
                break;
        }

    }

    // private void CheckEnemyIsDead(){
    //     if (enemyToAttackScript.GetIsDead() || enemyToAttack.IsDestroyed()){
    //         canAttack = false;
    //         isLockedOntoEnemy = false;
    //         enemyInfo.Remove(enemyToAttackID);
    //     }
    // }

    IEnumerator AttackCooldown(){ //could probably make this into a generic "AttackCooldown"
    //time to wait would be a variable that is established on start
        // playerAnim.SetTrigger("Punch_trig");
        //deactivate hand colliders here
        canAttack = false;
        yield return new WaitForSeconds(attackCooldownTime);
        switch(playerType){
            case PlayerType.MELEE:
                // leftHandCollider.enabled = false;
                rightHandCollider.enabled = false;
                playerAnim.ResetTrigger("Punch_trig");
                break;
            case PlayerType.SMALL_RANGED:
                playerAnim.ResetTrigger("Shoot_small_trig");
                //stop animation
                break;
        }
        if (enemyToAttackScript.GetIsDead() || enemyToAttack.IsDestroyed()){
            canAttack = false;
            isLockedOntoEnemy = false;
            enemyInfo.Remove(enemyToAttackID);
        } else{
            canAttack = true;
        }
    }
}


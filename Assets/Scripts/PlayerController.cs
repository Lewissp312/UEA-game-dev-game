using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private bool isOverPlayer;
    private bool isMovingToPosition;
    private bool isPlayerClicked;
    private bool isLockedOntoEnemy;
    private bool isMovingToEnemy;
    private bool canAttack;
    private enum PlayerType{MELEE,SWORD,HEAVY,GUN,ROCKET};
    private int enemyToAttackID;
    private int attackAnimationNamesLen;
    private float distanceAttackingEnemy;
    private string attackAnimationName;
    private readonly string[] attackAnimationNames = {"Attack_1_trig","Attack_2_trig","Attack_3_trig"};
    private Animator playerAnim;
    private Color ringColor;
    private Color ringColorTrans;
    private Dictionary<int,GameObject> enemyInfo;
    private Dictionary<string,BoxCollider> meleeBoxColliders;
    private Dictionary<string,BoxCollider> swordBoxColliders;
    private Dictionary<string,BoxCollider> heavyBoxColliders;
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
    private NavMeshAgent playerAgent;
    private System.Random random;
    [SerializeField] private float attackCooldownTime;
    [SerializeField] private float playerSpeed;
    [SerializeField] private int health;
    [SerializeField] private GameObject laser;
    [SerializeField] private PlayerType playerType;
    [SerializeField] private BoxCollider leftHand;
    [SerializeField] private BoxCollider rightHand;
    [SerializeField] private BoxCollider leftFoot;
    // Start is called before the first frame update
    void Start()
    {
        canAttack = true;
        attackAnimationNamesLen = attackAnimationNames.Length;
        playerAnim = GetComponent<Animator>();
        ringColor = new Color(0.96f,0.96f,0.51f,0.3f);
        ringColorTrans = new Color(0.96f,0.96f,0.51f,0f);
        selectedEffect = GetComponent<ParticleSystem>();
        ringRenderer = transform.GetChild(2).gameObject.GetComponent<Renderer>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerAgent = GetComponent<NavMeshAgent>();
        meleeBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",leftFoot},{"Attack_2_trig",rightHand},{"Attack_3_trig",leftHand}};
        swordBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",rightHand},{"Attack_2_trig",rightHand},{"Attack_3_trig",rightHand}};
        heavyBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",leftHand},{"Attack_2_trig",rightHand},{"Attack_3_trig",leftHand}};
        enemyInfo = new();
        attackAnimationName = "Attack_1_trig";
        random = new System.Random();
        distanceAttackingEnemy = playerType switch
        {
            PlayerType.GUN or PlayerType.ROCKET => 8f,
            _ => 1.4f,
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)){
            if (!isMovingToPosition){
                mousePosition = GetMouseOnBoardPosition(out isOverPlayer);
                //if the player clicks the player character, handle the effects accordingly
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
                    playerAnim.SetTrigger("Run_trig");
                    isPlayerClicked = false;
                    selectedEffect.Clear();
                    selectedEffect.Stop();
                    playerAgent.SetDestination(positionToMoveTo);
                }
            }
        }

        if (!isMovingToPosition){ 
            if (isLockedOntoEnemy){
                if (isMovingToEnemy){
                    if (enemyToAttackScript.GetIsDead() || enemyToAttack.IsDestroyed()){
                        canAttack = false;
                        isMovingToEnemy = false;
                        isLockedOntoEnemy = false;
                        enemyInfo.Remove(enemyToAttackID);
                    } else{
                        playerAgent.SetDestination(enemyToAttack.transform.position);
                        if (Vector3.Distance(transform.position,enemyToAttack.transform.position) <= distanceAttackingEnemy){
                            playerAgent.ResetPath();
                            isMovingToEnemy = false;
                            playerAnim.ResetTrigger("Run_trig");
                            canAttack = true;
                        }
                    }
                } else if(canAttack){
                    if (enemyToAttackScript.GetIsDead() || enemyToAttack.IsDestroyed()){
                        canAttack = false;
                        isMovingToEnemy = false;
                        isLockedOntoEnemy = false;
                        enemyInfo.Remove(enemyToAttackID);
                    } else{
                        if(Vector3.Distance(transform.position,enemyToAttack.transform.position) > distanceAttackingEnemy){
                            isMovingToEnemy = true;
                            canAttack = false;
                            playerAnim.SetTrigger("Run_trig");
                        }
                        if (canAttack){
                            transform.LookAt(enemyToAttack.transform.position);
                            AttackEnemy();
                        }
                    }
                }
            } else if (enemyInfo.Count > 0){
                enemyToAttack = GetClosestEnemy();
                //Enemy to attack is set to the player gameobject when there is no enemy to attack 
                if (enemyToAttack != gameObject){
                    enemyToAttackScript = enemyToAttack.GetComponent<EnemyController>();
                    enemyToAttackID = enemyToAttackScript.GetEnemyID();
                    isLockedOntoEnemy = true;
                    isMovingToEnemy = true;
                    playerAnim.SetTrigger("Run_trig");
                }
            }   
        }
        if (isMovingToPosition){
            if (Vector3.Distance(transform.position,positionToMoveTo) <= 0){
                playerAgent.ResetPath();
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

    //Melee Health = 300
    //Gun Health = 375
    //Sword Health = 425
    //Heavy Health = 500
    private void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("EnemyMelee") || 
            other.gameObject.CompareTag("EnemyLaser") || 
            other.gameObject.CompareTag("EnemySword") || 
            other.gameObject.CompareTag("EnemyHeavy"))
        {
            gameManager.IncreaseEnemyPoints(1);
        }
        if (other.gameObject.CompareTag("EnemyMelee")){
            health -= 5; 
        } else if(other.gameObject.CompareTag("EnemyLaser")){
            health -= 10;
        } else if(other.gameObject.CompareTag("EnemySword")){
            health -= 20;
        } else if(other.gameObject.CompareTag("EnemyHeavy")){
            health -= 30;
        //TODO: Put death proceedures here
        }
    }

    private GameObject GetClosestEnemy(){
        float lowestDistance = 100;
        float tempDistance;
        int[] enemiesToRemove = new int[296];
        int enemiesToRemoveIndex = 0;
        GameObject lowestDistanceEnemy = gameObject;
        foreach(KeyValuePair<int, GameObject> enemy in enemyInfo){
            if (enemy.Value.IsDestroyed() || enemy.Value.GetComponent<EnemyController>().GetIsDead()){
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
        return lowestDistanceEnemy;
    }

    public void AddToEnemyList(int enemyID, GameObject enemy){
        enemyInfo.Add(enemyID,enemy);
    }

    public void RemoveFromEnemyList(int enemyID){
        enemyInfo.Remove(enemyID);
    }



    private Vector3 GetMouseOnBoardPosition(out bool isOverPlayer){
        //TODO: Change this so that more bad mouse positions are flagged up  
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
                lastGoodMousePos = new(hitData.point.x,playerPos.y,hitData.point.z);
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
        int randomNum = random.Next(0,attackAnimationNamesLen);
        print(randomNum);
        attackAnimationName = attackAnimationNames[randomNum];
        playerAnim.SetTrigger(attackAnimationName);
        print($"Now setting {attackAnimationName}");
        switch(playerType){
            case PlayerType.MELEE:
                meleeBoxColliders[attackAnimationName].enabled = true;
                break;
            case PlayerType.SWORD:
                swordBoxColliders[attackAnimationName].enabled = true;
                break;
            case PlayerType.HEAVY:
                heavyBoxColliders[attackAnimationName].enabled = true;
                break;
            case PlayerType.GUN:
                transform.LookAt(enemyToAttack.transform.position);
                //TODO: change this to object pooling to be more effecient
                laserHeight = new Vector3(transform.position.x,transform.position.y + 2,transform.position.z);
                laserPosition = (transform.forward * 2) + laserHeight;
                laserRotation = transform.rotation * Quaternion.Euler(90,0,0);
                enemyPositionForLaser = new Vector3(enemyToAttack.transform.position.x,
                    enemyToAttack.transform.position.y + 2, 
                    enemyToAttack.transform.position.z
                );
                GameObject newLaser = Instantiate(laser,laserPosition,laserRotation);
                PlayerLaser laserScript = newLaser.GetComponent<PlayerLaser>();
                laserScript.SetPositionToAttack(enemyPositionForLaser);
                laserScript.SetParentGameObject(gameObject);
                break;
        }
        StartCoroutine(AttackCooldown());

    }

    IEnumerator AttackCooldown(){
        canAttack = false;
        yield return new WaitForSeconds(attackCooldownTime);
        playerAnim.ResetTrigger(attackAnimationName);
        switch(playerType){
            case PlayerType.MELEE:
                meleeBoxColliders[attackAnimationName].enabled = false;
                break;
            case PlayerType.SWORD:
                swordBoxColliders[attackAnimationName].enabled = false;
                break;
            case PlayerType.HEAVY:
                heavyBoxColliders[attackAnimationName].enabled = false;
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


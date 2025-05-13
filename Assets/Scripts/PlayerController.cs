using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.XR;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    private bool isMousePosOverPlayer;
    private bool isMousePosOnGround;
    private bool isMovingToPosition;
    private bool isPlayerClicked;
    private bool isLockedOntoEnemy;
    private bool isMovingToEnemy;
    private bool isDead;
    private bool isItemSquarePlayer;
    private bool canAttack;
    private enum PlayerType{MELEE,SWORD,HEAVY,GUN,ROCKET};
    private int enemyToAttackID;
    private int attackAnimationNamesLen;
    private int maximumHealth;
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
    private Vector3 laserPosition;
    private Vector3 laserHeight;
    private Vector3 enemyPositionForLaser;
    private NavMeshAgent playerAgent;
    private System.Random random;
    [SerializeField] private float attackCooldownTime;
    [SerializeField] private int health;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private GameObject laser;
    [SerializeField] private PlayerType playerType;
    [SerializeField] private BoxCollider leftHand;
    [SerializeField] private BoxCollider rightHand;
    [SerializeField] private BoxCollider leftFoot;
    [SerializeField] private Material blue;
    // Start is called before the first frame update
    void Start()
    {
        canAttack = true;
        attackAnimationNamesLen = attackAnimationNames.Length;
        maximumHealth = health;
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
        if (!isDead){
            if (Input.GetMouseButtonDown(0) && !gameManager.GetIsMenuActive() && !isDead){
                if (!isMovingToPosition){
                    mousePosition = GetMouseOnBoardPosition(out isMousePosOverPlayer, out isMousePosOnGround);
                    //if the player clicks the player character, handle the effects accordingly
                    if (isMousePosOverPlayer || (isPlayerClicked && !isMousePosOnGround)){
                        isPlayerClicked = !isPlayerClicked;
                        if (selectedEffect.isPlaying){
                            selectedEffect.Clear();
                            selectedEffect.Stop();
                        } else{
                            selectedEffect.Play();
                        }
                    } else if (isPlayerClicked && isMousePosOnGround){
                        positionToMoveTo = mousePosition;
                        isMovingToPosition = true;
                        isLockedOntoEnemy = false;
                        isMovingToEnemy = false;
                        canAttack = false;
                        playerAnim.ResetTrigger(attackAnimationName);
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
                        IsEnemyDeadCheck(out bool isEnemyDead);
                        if (!isEnemyDead){
                            playerAgent.SetDestination(enemyToAttack.transform.position);
                            if (Vector3.Distance(transform.position,enemyToAttack.transform.position) <= distanceAttackingEnemy){
                                playerAgent.ResetPath();
                                isMovingToEnemy = false;
                                playerAnim.ResetTrigger("Run_trig");
                                canAttack = true;
                            }
                        }
                    } else if(canAttack){
                        IsEnemyDeadCheck(out bool isEnemyDead);
                        if (!isEnemyDead){
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
            } else{
                if (Vector3.Distance(transform.position,positionToMoveTo) <= 0){
                    playerAgent.ResetPath();
                    isMovingToPosition = false;
                    playerAnim.ResetTrigger("Run_trig");
                }
            }
        }
    }

    private void OnMouseOver(){
        if (!gameManager.GetIsMenuActive()){
            ringRenderer.material.color = ringColor;
        }
        //245,245,131,128
    }

    private void OnMouseExit(){
        ringRenderer.material.color = ringColorTrans;
    }

    private void OnTriggerEnter(Collider other){
        if (other.CompareTag("AttackEffectArea")){
            print("Hit an enemy attack area");
            ItemSpaceController itemSpaceScript = other.transform.parent.GetComponent<ItemSpaceController>();
            if (itemSpaceScript.GetItemSpaceOwner() == GameManager.ItemSpaceOwner.ENEMY && 
                itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
                GetComponent<NavMeshAgent>().speed -= 3;
            }
        }
        GameObject enemyGameobject = other.gameObject;
        if (enemyGameobject.CompareTag("EnemyMelee") || 
        enemyGameobject.CompareTag("EnemyLaser") || 
        enemyGameobject.CompareTag("EnemySword") || 
        enemyGameobject.CompareTag("EnemyHeavy"))
        {
            gameManager.IncreaseEnemyPoints(1);
        }
        if (enemyGameobject.CompareTag("EnemyMelee")){
            health -= 5;
            healthBar.UpdateHealth(health,maximumHealth);
        } else if(enemyGameobject.CompareTag("EnemyLaser")){
            GameObject shooterObject = enemyGameobject.GetComponent<PlayerLaser>().GetShooterGameObject();
            if (!shooterObject.IsDestroyed()){
                if (shooterObject.GetComponent<EnemyController>().GetIsItemSquareEnemy()){
                    health -= 20;
                } else{
                    health -= 10;
                }
            } else{
                health -= 10;
            }
            healthBar.UpdateHealth(health,maximumHealth);
            Destroy(enemyGameobject);
        } else if(enemyGameobject.CompareTag("EnemySword")){
            health -= 20;
            healthBar.UpdateHealth(health,maximumHealth);
        } else if(enemyGameobject.CompareTag("EnemyHeavy")){
            health -= 30;
            healthBar.UpdateHealth(health,maximumHealth);
        }
        if (health <= 0 && !isDead){
            DeathProcedure();
        }
    }

    private void OnTriggerExit(Collider other){
        if (other.CompareTag("AttackEffectArea")){
            ItemSpaceController itemSpaceScript = other.transform.parent.GetComponent<ItemSpaceController>();
            if (itemSpaceScript.GetItemSpaceOwner() == GameManager.ItemSpaceOwner.ENEMY && 
                itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
                GetComponent<NavMeshAgent>().speed += 3;
            }
        }
    }

    private void OnCollisionEnter(Collision other){
        if (other.gameObject.CompareTag("AttackEffectArea")){
            // print("Hit an enemy attack area");
            ItemSpaceController itemSpaceScript = other.transform.parent.GetComponent<ItemSpaceController>();
            if (itemSpaceScript.GetItemSpaceOwner() == GameManager.ItemSpaceOwner.ENEMY && 
                itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
                GetComponent<NavMeshAgent>().speed -= 3;
            }
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

    public void DecreaseHealth(int healthToTakeAway){
        health -= healthToTakeAway;
        healthBar.UpdateHealth(health,maximumHealth);
        if (health <= 0){
            DeathProcedure();
        }
    }

    public void AddToEnemyList(int enemyID, GameObject enemy){
        enemyInfo.Add(enemyID,enemy);
    }

    public void RemoveFromEnemyList(int enemyID){
        enemyInfo.Remove(enemyID);
    }

    public void SetAsItemSquarePlayer(){
        isItemSquarePlayer = true;
        transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().materials[0] = blue;
    }

    public bool CanPlayerBeAttacked(){
        int numofEnemiesAttackingPlayer = 0;
        foreach(KeyValuePair<int, GameObject> enemy in enemyInfo){
            if (enemy.Value.IsDestroyed() || enemy.Value.GetComponent<EnemyController>().GetIsDead()){
                continue;
            }
            if (enemy.Value.GetComponent<EnemyController>().GetObjectOfInterest() == gameObject){
                numofEnemiesAttackingPlayer++;
                if (numofEnemiesAttackingPlayer >= 4){
                    return false;
                }
            }
        }
        return true;
    }

    public bool GetIsDead(){
        return isDead;
    }

    public bool GetIsItemSquarePlayer(){
        return isItemSquarePlayer;
    }



    private Vector3 GetMouseOnBoardPosition(out bool isMousePosOverPlayer, out bool isMousePosOnGround){
        //TODO: Change this so that more bad mouse positions are flagged up  
        Ray ray;
        ray =  Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 playerPos = transform.position;
        Vector3 mousePos = transform.position;
        if(Physics.Raycast(ray,out RaycastHit hitData, 10000)){
            //Hit the ground
            //TODO: Add item point colliders here when done
            GameObject hitObject = hitData.collider.gameObject;
            if (hitObject.CompareTag("Ground") || hitObject.CompareTag("Enemy") || hitObject.CompareTag("AttackArea") || hitObject.CompareTag("AttackEffectArea")){
                mousePos = new(hitData.point.x,playerPos.y,hitData.point.z);
                isMousePosOverPlayer = false;
                isMousePosOnGround = true;
                return mousePos;
            //Hit the player with this script
            } else if (hitObject == gameObject){
                isMousePosOverPlayer = true;
                isMousePosOnGround = false;
                return mousePos;
            //Hit something else
            } else {
                isMousePosOverPlayer = false;
                isMousePosOnGround = false;
                return mousePos;
            }
        } 
        //Hit nothing
        else{
            isMousePosOverPlayer = false;
            isMousePosOnGround = false;
            return mousePos;
        }
    }

    private void AttackEnemy(){
        int randomNum = random.Next(0,attackAnimationNamesLen);
        attackAnimationName = attackAnimationNames[randomNum];
        playerAnim.SetTrigger(attackAnimationName);
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
                laserScript.SetShooterGameObject(gameObject);
                break;
        }
        StartCoroutine(AttackCooldown());

    }

    private void IsEnemyDeadCheck(out bool isEnemyDead){
        if (enemyToAttackScript.GetIsDead() || enemyToAttack.IsDestroyed()){
            canAttack = false;
            isMovingToEnemy = false;
            isLockedOntoEnemy = false;
            enemyInfo.Remove(enemyToAttackID);
            isEnemyDead = true;
        } else{
            isEnemyDead = false;
        }
    }

    public void DeathProcedure(){
        //TODO: Add removing players from player list in GameManager
        isDead = true;
        StopAllCoroutines();
        playerAnim.ResetTrigger(attackAnimationName);
        playerAnim.ResetTrigger("Run_trig");
        playerAnim.SetTrigger("Death_trig");
        StartCoroutine(WaitForDeath());
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

    IEnumerator WaitForDeath(){
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}


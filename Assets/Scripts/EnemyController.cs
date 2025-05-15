using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
/// <summary>
/// Controls all enemy behaviour
/// </summary>
public class EnemyController : MonoBehaviour
{
    private bool canAttack;
    private bool isDead;
    private bool isMovingToObject;
    private bool isAttackingObject;
    private bool isItemSquareEnemy;
    private int enemyID;
    private int pointNum;
    private int attackAnimationNamesLen;
    private int maximumHealth;
    private int filesLen;
    private float distanceToAttackPlayerFrom;
    private string attackAnimationName;
    private readonly string[] attackAnimationNames = {"Attack_1_trig","Attack_2_trig","Attack_3_trig"};
    private Animator anim;
    private enum EnemyType{MELEE,SWORD,HEAVY,GUN,ROCKET}
    private enum ObjectType{FILE,PLAYER}
    private ObjectType objectType;
    private GameManager gameManager;
    private GameObject[] filesToAttack;
    private GameObject[] players;
    private GameObject objectToMoveTo;
    private NavMeshAgent enemyAgent;
    private LayerMask fileMask;
    private Dictionary<string,BoxCollider> meleeBoxColliders;
    private Dictionary<string,BoxCollider> swordBoxColliders;
    private Dictionary<string,BoxCollider> heavyBoxColliders;
    private System.Random random;
    private PlayerController playerScript;
    [SerializeField] private int health;
    [SerializeField] private float attackCooldownTime;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private GameObject laser;
    [SerializeField] private BoxCollider rightHand;
    [SerializeField] private BoxCollider leftHand;
    [SerializeField] private BoxCollider leftFoot;
    [SerializeField] private Material green;

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Unity methods
    void Start()
    {
        canAttack = true;
        objectType = ObjectType.FILE;
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        anim = GetComponent<Animator>();
        enemyAgent = GetComponent<NavMeshAgent>();
        enemyID = gameManager.GetEnemyID();
        gameManager.IncreaseEnemyNum();
        gameManager.AddToActiveEnemies(enemyID,gameObject);
        attackAnimationNamesLen = attackAnimationNames.Length;
        maximumHealth = health;
        distanceToAttackPlayerFrom = enemyType switch
        {
            EnemyType.GUN or EnemyType.ROCKET => 13f,
            _ => 1.4f,
        };
        attackAnimationName = "Attack_1_trig";
        //Initialisation of objectToMoveTo, never actually moves to itself
        objectToMoveTo = gameObject;
        filesToAttack = gameManager.GetFilesToAttack();
        filesLen = filesToAttack.Length;
        players = GameObject.FindGameObjectsWithTag("Player");
        fileMask = LayerMask.GetMask("File","Wall");
        meleeBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",leftFoot},{"Attack_2_trig",rightHand},{"Attack_3_trig",leftHand}};
        swordBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",rightHand},{"Attack_2_trig",rightHand},{"Attack_3_trig",rightHand}};
        heavyBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",rightHand},{"Attack_2_trig",leftHand},{"Attack_3_trig",leftHand}};
        random = new System.Random();
    }

    void Update()
    {
        if(gameManager.GetIsGameActive() && !isDead){
            if(isMovingToObject){
                switch(objectType){
                    case ObjectType.FILE:
                        switch(enemyType){
                            case EnemyType.GUN:
                                RaycastHit hit;
                                //The gun enemy will only hit a file if there are no obstructions
                                if(Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 60, fileMask))
                                { 
                                    enemyAgent.ResetPath();
                                    transform.LookAt(objectToMoveTo.transform.parent.position);
                                    isMovingToObject = false;
                                    anim.ResetTrigger("Run_trig");
                                    isAttackingObject = true;
                                }
                                //The gun enemy can sometimes not look at the file when they are travelling to it. 
                                //This ensures they do at a certain point
                                if(Vector3.Distance(transform.position,enemyAgent.destination) <= 20){
                                    transform.LookAt(objectToMoveTo.transform.parent.position);
                                }
                                break;
                            default:
                                if(Vector3.Distance(transform.position,enemyAgent.destination) <= 0)
                                {
                                    isMovingToObject = false;
                                    anim.ResetTrigger("Run_trig");
                                    isAttackingObject = true;
                                    Vector3 filePosition = objectToMoveTo.transform.parent.position;
                                    Vector3 posToLookAt = transform.position;
                                    switch(pointNum){
                                        //Gets them looking straight ahead at the file once positioned, 
                                        // as enemies looking at the centre can sometimes be stanced diagonally
                                        case 1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 27:
                                            posToLookAt = new(transform.position.x,transform.position.y,filePosition.z);
                                            break;
                                        case 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 28 or 29 or 30 or 31 or 32 or 33 or 34 or 35 or 36:
                                            posToLookAt = new(filePosition.x,transform.position.y,transform.position.z);
                                            break;
                                    }
                                    transform.LookAt(posToLookAt);
                                }
                                break;
                        }
                        break;
                    case ObjectType.PLAYER:
                        if(objectToMoveTo.IsDestroyed() || objectToMoveTo.GetComponent<PlayerController>().GetIsDead()){
                            anim.ResetTrigger("Run_trig");
                            enemyAgent.ResetPath();
                            isMovingToObject = false;
                            //Reset objectToMoveTo so that a new object is picked
                            objectToMoveTo = gameObject;
                        } else{
                            MoveTowardsPlayer(objectToMoveTo.transform.position);
                            Vector3 playerPos = new(objectToMoveTo.transform.position.x,
                                                    transform.position.y,
                                                    objectToMoveTo.transform.position.z);
                            if(Vector3.Distance(transform.position,playerPos) < distanceToAttackPlayerFrom){
                                anim.ResetTrigger("Run_trig");
                                enemyAgent.ResetPath();
                                isMovingToObject = false;
                                isAttackingObject = true;
                            }
                        }
                        break;
                }
            } else if(isAttackingObject && canAttack){
                AttackObject();
                if(objectType == ObjectType.PLAYER){
                    if(objectToMoveTo.IsDestroyed() || playerScript.GetIsDead()){
                        anim.ResetTrigger(attackAnimationName);
                        isAttackingObject = false;
                        objectToMoveTo = gameObject;
                    //If the player gets too far away, chase them
                    }else if(Vector3.Distance(transform.position,objectToMoveTo.transform.position) > distanceToAttackPlayerFrom){
                        isAttackingObject = false;
                        anim.SetTrigger("Run_trig");
                        isMovingToObject = true;
                    }
                }
            } else if(objectToMoveTo == gameObject){
                objectToMoveTo = GetObjectToAttack();
                anim.SetTrigger("Run_trig");
                isMovingToObject = true;
            }
        }
    }

    void OnTriggerEnter(Collider other){
        if(other.CompareTag("AttackEffectArea")){
            ItemSpaceController itemSpaceScript = other.transform.parent.GetComponent<ItemSpaceController>();
            if(itemSpaceScript.GetItemSpaceOwner() == GameManager.ItemSpaceOwner.PLAYER && 
                itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
                GetComponent<NavMeshAgent>().speed -= 3;
            }
        } else{
            GameObject enemyGameObject = other.gameObject;
            if(enemyGameObject.CompareTag("PlayerMelee")){
                health -= 5;
                healthBar.UpdateHealth(health,maximumHealth);
            } else if(enemyGameObject.CompareTag("PlayerLaser")){
                GameObject shooterObject = enemyGameObject.GetComponent<LaserController>().GetShooterGameObject();
                if(!shooterObject.IsDestroyed()){
                    if(shooterObject.GetComponent<PlayerController>().GetIsItemSquarePlayer()){
                        health -= 20;
                    } else{
                        health -= 10;
                    }
                } else{
                    health -= 10;
                }
                health -= 10;
                healthBar.UpdateHealth(health,maximumHealth);
            } else if(enemyGameObject.CompareTag("PlayerSword")){
                health -= 20;
                healthBar.UpdateHealth(health,maximumHealth);
            } else if(enemyGameObject.CompareTag("PlayerHeavy")){
                health -= 30;
                healthBar.UpdateHealth(health,maximumHealth);
            }
            if(health <= 0 && !isDead){
                if(enemyGameObject.CompareTag("PlayerLaser")){
                    Destroy(enemyGameObject);
                }
                StopAllCoroutines();
                DeathProcedure();
            } else{
                if(objectType == ObjectType.FILE && 
                (enemyGameObject.CompareTag("PlayerMelee") || 
                enemyGameObject.CompareTag("PlayerSword") || 
                enemyGameObject.CompareTag("PlayerHeavy") || 
                enemyGameObject.CompareTag("PlayerLaser"))){
                    GameObject player = gameObject;
                    if(enemyGameObject.CompareTag("PlayerMelee") || 
                    enemyGameObject.CompareTag("PlayerSword") || 
                    enemyGameObject.CompareTag("PlayerHeavy")){
                        //For close range players, the player gameobject is attached to the root of their collider
                        player = enemyGameObject.transform.root.gameObject;
                    } else if(enemyGameObject.CompareTag("PlayerLaser")){
                        //For ranged players, the player gameobject is found through the laser object
                        player = enemyGameObject.GetComponent<LaserController>().GetShooterGameObject();
                    }
                    if(player.GetComponent<PlayerController>().CanPlayerBeAttacked()){
                        objectToMoveTo.transform.parent.gameObject.GetComponent<FileController>().MakePointAvailable(pointNum);
                        objectToMoveTo = player;
                        playerScript = player.GetComponent<PlayerController>();
                        isAttackingObject = false;
                        isMovingToObject = true;
                        objectType = ObjectType.PLAYER;
                        transform.LookAt(objectToMoveTo.transform.position);
                        anim.ResetTrigger(attackAnimationName);
                        anim.SetTrigger("Run_trig");
                    }
                }
                if(enemyGameObject.CompareTag("PlayerLaser")){
                    Destroy(enemyGameObject);
                }
            }
        }
    }

    void OnTriggerExit(Collider other){
        if(other.CompareTag("AttackEffectArea")){
            ItemSpaceController itemSpaceScript = other.transform.parent.GetComponent<ItemSpaceController>();
            if(itemSpaceScript.GetItemSpaceOwner() == GameManager.ItemSpaceOwner.PLAYER && 
                itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
                GetComponent<NavMeshAgent>().speed += 3;
            }
        }
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Public class methods

/// <summary>
/// Executes necessary functions for when an enemy dies 
/// </summary>
    public void DeathProcedure(){
        isDead = true;
        objectToMoveTo.transform.parent.gameObject.GetComponent<FileController>().MakePointAvailable(pointNum);
        // if(objectToMoveTo.CompareTag("Point")){
        //     objectToMoveTo.transform.parent.gameObject.GetComponent<FileController>().MakePointAvailable(pointNum);
        // } else if(objectToMoveTo.CompareTag("File")){
        //     objectToMoveTo.GetComponent<FileController>().MakePointAvailable(pointNum);
        // }
        gameManager.RemoveFromActiveEnemies(enemyID);
        enemyAgent.ResetPath();
        anim.ResetTrigger(attackAnimationName);
        anim.ResetTrigger("Run_trig");
        anim.SetTrigger("Death_trig");
        switch(enemyType){
            case EnemyType.MELEE:
                gameManager.IncreasePlayerPoints(5);
                break;
            case EnemyType.GUN:
                gameManager.IncreasePlayerPoints(10);
                break;
            case EnemyType.SWORD:
                gameManager.IncreasePlayerPoints(20);
                break;
            case EnemyType.HEAVY:
                gameManager.IncreasePlayerPoints(30);
                break;
        }
        StartCoroutine(WaitForDeath());
    }

    public void DecreaseHealth(int healthToTakeAway){
        health -= healthToTakeAway;
        healthBar.UpdateHealth(health,maximumHealth);
        if(health <= 0 && !isDead){
            DeathProcedure();
        }
    }

    public void SetAsItemSquareEnemy(){
        isItemSquareEnemy = true;
        health += 70;
        maximumHealth = health;
        GetComponent<NavMeshAgent>().speed += 4;
        transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().materials = new Material[]{green};
    }

    //Activated by the attack area of players to get enemies to stop following them when they run away
    public void StopAttackingPlayer(GameObject player){
        if(objectType == ObjectType.PLAYER){
            if(objectToMoveTo == player){
                isAttackingObject = false;
                objectToMoveTo = GetObjectToAttack();
                isMovingToObject = true;
            }
        }
    }

    private void MoveTowardsPlayer(Vector3 locationToMoveTo){
        //Changes the y value for previously discussed reason
        Vector3 moveHere = new(locationToMoveTo.x,transform.position.y,locationToMoveTo.z);
        enemyAgent.SetDestination(moveHere);
    }

/// <summary>
/// Makes all enemies stop moving in the event of a game over in which they are still present 
/// </summary>
    public void GameOverProcedure(){
        StopAllCoroutines();
        enemyAgent.ResetPath();
        anim.ResetTrigger(attackAnimationName);
        anim.ResetTrigger("Run_trig");
    }

    public int GetEnemyID(){
        return enemyID;
    }

    public bool GetIsDead(){
        return isDead;
    }

    public bool GetIsItemSquareEnemy(){
        return isItemSquareEnemy;
    }

    public GameObject GetObjectToMoveTo(){
        return objectToMoveTo;
    }


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Private class methods

/// <summary>
/// Launches an attack with a random attack animation, both melee and ranged.
/// </summary>

    private void AttackObject(){
        if(canAttack){
            int randomNum = random.Next(0,attackAnimationNamesLen);
            attackAnimationName = attackAnimationNames[randomNum];
            anim.SetTrigger(attackAnimationName);
            switch(enemyType){
                case EnemyType.MELEE:
                    meleeBoxColliders[attackAnimationName].enabled = true;
                    break;
                case EnemyType.SWORD:
                    swordBoxColliders[attackAnimationName].enabled = true;
                    break;
                case EnemyType.HEAVY:
                    heavyBoxColliders[attackAnimationName].enabled = true;
                    break;
                case EnemyType.GUN:
                    //Adds 2 to the y value so the laser doesn't spawn on the ground
                    Vector3 laserHeight = new(transform.position.x,transform.position.y + 2,transform.position.z);
                    Vector3 laserPosition = (transform.forward * 2) + laserHeight;
                    Quaternion laserRotation = transform.rotation * Quaternion.Euler(90,0,0);
                    Vector3 positionForLaser = new(objectToMoveTo.transform.position.x,
                                                   objectToMoveTo.transform.position.y, 
                                                   objectToMoveTo.transform.position.z);
                    GameObject newLaser = Instantiate(laser,laserPosition,laserRotation);
                    LaserController laserScript = newLaser.GetComponent<LaserController>();
                    laserScript.SetAsEnemyLaser();
                    laserScript.SetPositionToAttack(positionForLaser);
                    laserScript.SetShooterGameObject(gameObject);
                    break;
            }
            StartCoroutine(AttackCooldown());
        }
    }

/// <summary>
/// Finds an object for the enemy to move towards and sets it as the navmeshagent destination. 
/// This is either the closest file or the closest player.
/// </summary>
    private GameObject GetObjectToAttack(){
        float closestDistance = 100;
        GameObject filePoint;
        float[] distanceToFiles = new float[filesLen];
        Dictionary<float,GameObject> distanceToFilesDict = new();
        //Extracts all the distances to all the files on the map.
        //Places the distances in an array and in a dictionary with their respective file gameobjects.
        for(int i=0; i<filesLen; i++){
            filePoint = filesToAttack[i];
            float distanceToFile = Vector3.Distance(transform.position,filePoint.transform.position);
            distanceToFiles[i] = distanceToFile;
            distanceToFilesDict.Add(distanceToFile,filesToAttack[i]);
        }
        //Sorts the distances, and then loops through them. Each distance's entry is found in the dictionary.
        //If the file is filled up, the script can then examine the next closest file in the list and find it's dictionary entry.
        Array.Sort(distanceToFiles);
        for(int i=0; i<distanceToFiles.Length; i++){
            filePoint = distanceToFilesDict[distanceToFiles[i]];
            filePoint = filePoint.GetComponent<FileController>().GetAvailablePoint(out pointNum);
            //If an available point was found
            if(pointNum != 0){
                objectType = ObjectType.FILE;
                //The position is set like this because the points are on a different y position to the player.
                //If the real position was used, the enemy would be trying to run to a y position that they cannot get to.
                Vector3 filePointPos = new(filePoint.transform.position.x,
                                           transform.position.y,
                                           filePoint.transform.position.z);
                enemyAgent.SetDestination(filePointPos);
                return filePoint;
            }
        }

        GameObject closestPlayer = gameObject;
        foreach(GameObject player in players){
            float distanceFromPlayer = Vector3.Distance(transform.position,player.transform.position);
            if(distanceFromPlayer < closestDistance){
                closestDistance = distanceFromPlayer;
                closestPlayer = player;
            }
        }
        objectType = ObjectType.PLAYER;
        return closestPlayer;
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//IEnumerators

    IEnumerator AttackCooldown(){
        canAttack = false;
        yield return new WaitForSeconds(attackCooldownTime);
        anim.ResetTrigger(attackAnimationName);
        switch(enemyType){
            case EnemyType.MELEE:
                meleeBoxColliders[attackAnimationName].enabled = false;
                break;
            case EnemyType.SWORD:
                swordBoxColliders[attackAnimationName].enabled = false;
                break;
            case EnemyType.HEAVY:
                heavyBoxColliders[attackAnimationName].enabled = false;
                break;
        }
        canAttack = true;
    }

    IEnumerator WaitForDeath(){
        //Gives time for the death animation to play
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

}
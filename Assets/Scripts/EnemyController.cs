using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

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
    private float distanceAttackingPlayer;
    private string attackAnimationName;
    private readonly string[] attackAnimationNames = {"Attack_1_trig","Attack_2_trig","Attack_3_trig"};
    private Animator anim;
    private enum EnemyType{MELEE,SWORD,HEAVY,GUN,ROCKET}
    private enum ObjectType{FILE,PLAYER}
    private GameManager gameManager;
    private GameObject[] filesToAttack;
    // private GameObject playerToAttack;
    private GameObject objectToMoveTo;
    private ObjectType objectType;
    private NavMeshAgent enemyAgent;
    private LayerMask fileMask;
    private Dictionary<string,BoxCollider> meleeBoxColliders;
    private Dictionary<string,BoxCollider> swordBoxColliders;
    private Dictionary<string,BoxCollider> heavyBoxColliders;

    private System.Random random;
    private PlayerController playerScript;
    [SerializeField] private float attackCooldownTime;
    [SerializeField] private int health;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private GameObject[] playerClasses;
    [SerializeField] private GameObject laser;
    [SerializeField] private BoxCollider rightHand;
    [SerializeField] private BoxCollider leftHand;
    [SerializeField] private BoxCollider leftFoot;
    [SerializeField] private Material green;

    // Start is called before the first frame update
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
        distanceAttackingPlayer = enemyType switch
        {
            EnemyType.GUN or EnemyType.ROCKET => 13f,
            _ => 1.4f,
        };
        attackAnimationName = "Attack_1_trig";
        objectToMoveTo = gameObject;
        filesToAttack = gameManager.GetFilesToAttack();
        playerClasses = GameObject.FindGameObjectsWithTag("Player");
        fileMask = LayerMask.GetMask("File","Wall");
        meleeBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",leftFoot},{"Attack_2_trig",rightHand},{"Attack_3_trig",leftHand}};
        swordBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",rightHand},{"Attack_2_trig",rightHand},{"Attack_3_trig",rightHand}};
        heavyBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",rightHand},{"Attack_2_trig",leftHand},{"Attack_3_trig",leftHand}};
        random = new System.Random();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isDead){
            if (isMovingToObject){
                switch(objectType){
                    case ObjectType.FILE:
                        switch(enemyType){
                            case EnemyType.GUN:
                                RaycastHit hit;
                                // Does the ray intersect any file or wall objects
                                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 60, fileMask))
                                { 
                                    enemyAgent.ResetPath();
                                    transform.LookAt(objectToMoveTo.transform.position);
                                    isMovingToObject = false;
                                    anim.ResetTrigger("Run_trig");
                                    isAttackingObject = true;
                                }
                                //The gun enemy can sometimes not look at the file when they are travelling to it, this ensures they do at a certain point
                                if (Vector3.Distance(transform.position,new Vector3(objectToMoveTo.transform.position.x,transform.position.y,objectToMoveTo.transform.position.z)) <= 20){
                                    transform.LookAt(objectToMoveTo.transform.position);
                                }
                                break;
                            default:
                                if (Vector3.Distance(transform.position,new Vector3(objectToMoveTo.transform.position.x,transform.position.y,objectToMoveTo.transform.position.z)) <= 0)
                                {
                                    isMovingToObject = false;
                                    anim.ResetTrigger("Run_trig");
                                    isAttackingObject = true;
                                    Vector3 filePosition = objectToMoveTo.transform.parent.transform.position;
                                    Vector3 posToLookAt;
                                    switch(pointNum){
                                        //Gets them looking straight ahead at the file once positioned, 
                                        // as enemies looking at the centre can sometimes be stanced diagonally
                                        //TODO: Look at changing this as enemies in the middle already look directly ahead at the file
                                        case 1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 27:
                                            posToLookAt = new(transform.position.x,transform.position.y,filePosition.z);
                                            transform.LookAt(posToLookAt);
                                            break;
                                        case 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 28 or 29 or 30 or 31 or 32 or 33 or 34 or 35 or 36:
                                            posToLookAt = new(filePosition.x,transform.position.y,transform.position.z);
                                            transform.LookAt(posToLookAt);
                                            break;
                                    }
                                }
                                break;
                        }
                        break;
                    case ObjectType.PLAYER:
                        if (objectToMoveTo.IsDestroyed() || playerScript.GetIsDead()){
                            anim.ResetTrigger("Run_trig");
                            enemyAgent.ResetPath();
                            isMovingToObject = false;
                            objectToMoveTo = gameObject;
                        } else{
                            MoveTowardsPlayer(objectToMoveTo.transform.position);
                            if (Vector3.Distance(transform.position,new(objectToMoveTo.transform.position.x,transform.position.y,objectToMoveTo.transform.position.z)) < distanceAttackingPlayer){
                                anim.ResetTrigger("Run_trig");
                                enemyAgent.ResetPath();
                                isMovingToObject = false;
                                isAttackingObject = true;
                            }
                        }
                        break;
                }
            } else if (isAttackingObject && canAttack){
                AttackObject();
                if (objectType == ObjectType.PLAYER){
                    if (objectToMoveTo.IsDestroyed() || playerScript.GetIsDead()){
                        anim.ResetTrigger(attackAnimationName);
                        isAttackingObject = false;
                        objectToMoveTo = gameObject;
                    }else if (Vector3.Distance(transform.position,objectToMoveTo.transform.position) > distanceAttackingPlayer){
                        isAttackingObject = false;
                        anim.SetTrigger("Run_trig");
                        isMovingToObject = true;
                    }
                }
            } else if (objectToMoveTo == gameObject){
                objectToMoveTo = GetObjectToAttack();
                anim.SetTrigger("Run_trig");
                isMovingToObject = true;
            }
        }
    }

    private GameObject GetObjectToAttack(){
        float closestDistance = 100;
        GameObject examinedObject;
        float[] distanceToFiles = new float[filesToAttack.Length];
        Dictionary<float,GameObject> distanceToFilesDict = new();
        for(int i = 0; i<filesToAttack.Length; i++){
            examinedObject = filesToAttack[i];
            float distanceToFile = Vector3.Distance(transform.position,examinedObject.transform.position);
            distanceToFiles[i] = distanceToFile;
            distanceToFilesDict.Add(distanceToFile,filesToAttack[i]);
        }
        Array.Sort(distanceToFiles);
        for(int i=0; i<distanceToFiles.Length; i++){
            examinedObject = distanceToFilesDict[distanceToFiles[i]];
            examinedObject = examinedObject.GetComponent<FileController>().GetAvailablePoint(out pointNum);
            if (pointNum != 0){
                objectType = ObjectType.FILE;
                switch(enemyType){
                    case EnemyType.GUN or EnemyType.ROCKET:
                        Vector3 filePosition = examinedObject.transform.parent.transform.position;
                        Vector3 newFilePosition = new(filePosition.x,transform.position.y,filePosition.z);
                        enemyAgent.SetDestination(newFilePosition);
                        return examinedObject.transform.parent.gameObject;
                    default:
                        enemyAgent.SetDestination(examinedObject.transform.position);
                        return examinedObject;
                }
            }
        }

        GameObject closestPlayer = gameObject;
        foreach(GameObject player in playerClasses){
            float distanceFromPlayer = Vector3.Distance(transform.position,player.transform.position);
            if (distanceFromPlayer < closestDistance){
                closestDistance = distanceFromPlayer;
                closestPlayer = player;
            }
        }
        objectType = ObjectType.PLAYER;
        return closestPlayer;
    }

    private void AttackObject(){
        if (canAttack){
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
                    //Plus 2 to the y so the laser doesn't spawn on the ground
                    Vector3 laserHeight = new(transform.position.x,transform.position.y + 2,transform.position.z);
                    Vector3 laserPosition = (transform.forward * 2) + laserHeight;
                    Quaternion laserRotation = transform.rotation * Quaternion.Euler(90,0,0);
                    Vector3 positionForLaser = new(objectToMoveTo.transform.position.x,
                        objectToMoveTo.transform.position.y, 
                        objectToMoveTo.transform.position.z
                    );
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

    public int GetEnemyID(){
        return enemyID;
    }

    public void DecreaseHealth(int healthToTakeAway){
        health -= healthToTakeAway;
        healthBar.UpdateHealth(health,maximumHealth);
        if (health <= 0 && !isDead){
            DeathProcedure();
        }
    }

    public void SetAsItemSquareEnemy(){
        isItemSquareEnemy = true;
        health += 100;
        maximumHealth = health;
        GetComponent<NavMeshAgent>().speed += 4;
        transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().materials = new Material[]{green};
    }
    public void OnTriggerEnter(Collider other){
        if (other.CompareTag("AttackEffectArea")){
            ItemSpaceController itemSpaceScript = other.transform.parent.GetComponent<ItemSpaceController>();
            if (itemSpaceScript.GetItemSpaceOwner() == GameManager.ItemSpaceOwner.PLAYER && 
                itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
                GetComponent<NavMeshAgent>().speed -= 3;
            }
        } else{
            GameObject enemyGameObject = other.gameObject;
            if (enemyGameObject.CompareTag("PlayerMelee")){
                health -= 5;
                healthBar.UpdateHealth(health,maximumHealth);
            } else if(enemyGameObject.CompareTag("PlayerLaser")){
                GameObject shooterObject = enemyGameObject.GetComponent<LaserController>().GetShooterGameObject();
                if (!shooterObject.IsDestroyed()){
                    if (shooterObject.GetComponent<PlayerController>().GetIsItemSquarePlayer()){
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
            } else if (enemyGameObject.CompareTag("PlayerHeavy")){
                health -= 30;
                healthBar.UpdateHealth(health,maximumHealth);
            }
            if (health <= 0 && !isDead){
                if (enemyGameObject.CompareTag("PlayerLaser")){
                    Destroy(enemyGameObject);
                }
                StopAllCoroutines();
                DeathProcedure();
            } else{
                if (objectType == ObjectType.FILE && 
                (enemyGameObject.CompareTag("PlayerMelee") || 
                enemyGameObject.CompareTag("PlayerSword") || 
                enemyGameObject.CompareTag("PlayerHeavy") || 
                enemyGameObject.CompareTag("PlayerLaser"))){
                    GameObject player = gameObject;
                    if (enemyGameObject.CompareTag("PlayerMelee") || 
                    enemyGameObject.CompareTag("PlayerSword") || 
                    enemyGameObject.CompareTag("PlayerHeavy")){
                        player = enemyGameObject.transform.root.gameObject;
                    } else if (enemyGameObject.CompareTag("PlayerLaser")){
                        player = enemyGameObject.GetComponent<LaserController>().GetShooterGameObject();
                    }
                    if (player.GetComponent<PlayerController>().CanPlayerBeAttacked()){
                        if (objectToMoveTo.CompareTag("Point")){
                            objectToMoveTo.transform.parent.gameObject.GetComponent<FileController>().MakePointAvailable(pointNum);
                        } else{
                            objectToMoveTo.GetComponent<FileController>().MakePointAvailable(pointNum);
                        }
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
                if (enemyGameObject.CompareTag("PlayerLaser")){
                    Destroy(enemyGameObject);
                }
            }
        }
    }

    public void OnTriggerExit(Collider other){
        if (other.CompareTag("AttackEffectArea")){
            ItemSpaceController itemSpaceScript = other.transform.parent.GetComponent<ItemSpaceController>();
            if (itemSpaceScript.GetItemSpaceOwner() == GameManager.ItemSpaceOwner.PLAYER && 
                itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
                GetComponent<NavMeshAgent>().speed += 3;
            }
        }
    }

    public bool GetIsDead(){
        return isDead;
    }

    public bool GetIsItemSquareEnemy(){
        return isItemSquareEnemy;
    }

    public void StopAttackingPlayer(GameObject player){
        //TODO: Could remove player object check
        if (objectType == ObjectType.PLAYER){
            if (objectToMoveTo == player){
                isAttackingObject = false;
                objectToMoveTo = GetObjectToAttack();
                isMovingToObject = true;
            }
        }
    }

    public GameObject GetObjectOfInterest(){
        return objectToMoveTo;
    }

    private void MoveTowardsPlayer(Vector3 locationToMoveTo){
        Vector3 moveHere = new(locationToMoveTo.x,transform.position.y,locationToMoveTo.z);
        enemyAgent.SetDestination(moveHere);
    }

    public void DeathProcedure(){
        isDead = true;
        if (objectToMoveTo.CompareTag("Point")){
            objectToMoveTo.transform.parent.gameObject.GetComponent<FileController>().MakePointAvailable(pointNum);
        } else if(objectToMoveTo.CompareTag("File")){
            objectToMoveTo.GetComponent<FileController>().MakePointAvailable(pointNum);
        }
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
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

}
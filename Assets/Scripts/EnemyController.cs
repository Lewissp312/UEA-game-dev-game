using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class EnemyController : MonoBehaviour
{
    private bool canAttack;
    private bool isDead;
    private bool isMovingToObject;
    private bool isAttackingObject;
    private int enemyID;
    private int pointNum;
    private int attackAnimationNamesLen;
    private float distanceAttackingPlayer;
    private string attackAnimationName;
    private readonly string[] attackAnimationNames = {"Attack_1_trig","Attack_2_trig","Attack_3_trig"};
    private Animator anim;
    private enum EnemyType{MELEE,SWORD,HEAVY,GUN,ROCKET}
    private enum ObjectType{FILE,PLAYER}
    private GameManager gameManager;
    private GameObject[] filesToAttack;
    private GameObject playerToAttack;
    private GameObject objectToMoveTo;
    private ObjectType objectType;
    private NavMeshAgent enemyAgent;
    private LayerMask fileMask;
    private Dictionary<string,BoxCollider> meleeBoxColliders;
    private Dictionary<string,BoxCollider> swordBoxColliders;
    private Dictionary<string,BoxCollider> heavyBoxColliders;

    private System.Random random;
    [SerializeField] private float attackCooldownTime;
    [SerializeField] private float moveSpeed;
    [SerializeField] private int health;
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private GameObject[] playerClasses;
    [SerializeField] private GameObject laser;
    [SerializeField] private BoxCollider rightHand;
    [SerializeField] private BoxCollider leftHand;
    [SerializeField] private BoxCollider leftFoot;

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
        attackAnimationNamesLen = attackAnimationNames.Length;
        objectToMoveTo = gameObject;
        filesToAttack = gameManager.GetFilesToAttack();
        playerClasses = GameObject.FindGameObjectsWithTag("Player");
        fileMask = LayerMask.GetMask("File","Wall");
        meleeBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",leftFoot},{"Attack_2_trig",rightHand},{"Attack_3_trig",leftHand}};
        swordBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",rightHand},{"Attack_2_trig",rightHand},{"Attack_3_trig",rightHand}};
        heavyBoxColliders = new Dictionary<string, BoxCollider>(){{"Attack_1_trig",rightHand},{"Attack_2_trig",leftHand},{"Attack_3_trig",leftHand}};
        attackAnimationName = "Attack_1_trig";
        random = new System.Random();
        distanceAttackingPlayer = enemyType switch
        {
            EnemyType.GUN or EnemyType.ROCKET => 13f,
            _ => 1.4f,
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (isMovingToObject){
            switch(objectType){
                case ObjectType.FILE:
                    switch(enemyType){
                        case EnemyType.GUN:
                            RaycastHit hit;
                            // Does the ray intersect any file or wall objects
                            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 60, fileMask))
                            { 
                                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.black); 
                                enemyAgent.ResetPath();
                                transform.LookAt(objectToMoveTo.transform.position);
                                isMovingToObject = false;
                                anim.ResetTrigger("Run_trig");
                                isAttackingObject = true;
                            } else{
                                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white); 
                            }
                            //The gun enemy can sometimes not look at the file when they are travelling to it, this ensures they do at a certain point
                            if (Vector3.Distance(transform.position,new Vector3(objectToMoveTo.transform.position.x,transform.position.y,objectToMoveTo.transform.position.z)) <= 20){
                                print("I am looking at it as a gun");
                                transform.LookAt(objectToMoveTo.transform.position);
                            }
                            break;
                        default:
                            print($"Remaining distance: {Vector3.Distance(transform.position,objectToMoveTo.transform.position)}");
                            print($"Current position: {transform.position}. Position to move to: {objectToMoveTo.transform.position}");
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
                                    case 1 or 2 or 3 or 4 or 5 or 11 or 12 or 13 or 14 or 15:
                                        posToLookAt = new(transform.position.x,transform.position.y,filePosition.z);
                                        transform.LookAt(posToLookAt);
                                        print("I am looking at it as a melee");
                                        break;
                                    case 6 or 7 or 8 or 9 or 10 or 16 or 17 or 18 or 19 or 20:
                                        posToLookAt = new(filePosition.x,transform.position.y,transform.position.z);
                                        transform.LookAt(posToLookAt);
                                        print("I am looking at it as a melee");
                                        break;
                                }
                            }
                            break;
                    }
                    break;
                case ObjectType.PLAYER:
                    MoveTowardsPlayer(objectToMoveTo.transform.position);
                    if (Vector3.Distance(transform.position,new(objectToMoveTo.transform.position.x,transform.position.y,objectToMoveTo.transform.position.z)) < distanceAttackingPlayer){
                        enemyAgent.ResetPath();
                        isMovingToObject = false;
                        anim.ResetTrigger("Run_trig");
                        isAttackingObject = true;
                    }
                    break;
            }
        } else if (isAttackingObject && canAttack){
            AttackObject();
            if (objectType == ObjectType.PLAYER){
                if (Vector3.Distance(transform.position,objectToMoveTo.transform.position) > distanceAttackingPlayer){
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

        //TODO: Add one here for the items when they are done

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
            print(randomNum);
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
                    PlayerLaser laserScript = newLaser.GetComponent<PlayerLaser>();
                    laserScript.SetAsEnemyLaser();
                    laserScript.SetPositionToAttack(positionForLaser);
                    laserScript.SetParentGameObject(gameObject);
                    break;
            }
            StartCoroutine(AttackCooldown());
        }
    }

    public int GetEnemyID(){
        return enemyID;
    }

    //Health subtractions, death check, move to object position

    public void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("PlayerMelee")){
            health -= 5;
        } else if(other.gameObject.CompareTag("PlayerLaser")){
            health -= 10;
        } else if(other.gameObject.CompareTag("PlayerSword")){
            health -= 20;
        } else if (other.gameObject.CompareTag("PlayerHeavy")){
            health -= 30;
        }
        if (health <= 0 && !isDead){
            // StopAllCoroutines();
            anim.ResetTrigger(attackAnimationName);
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
            isDead = true;
            if (other.gameObject.CompareTag("PlayerLaser")){
                Destroy(other.gameObject);
            }
            StartCoroutine(WaitForDeath());
        } else{
            if (objectType == ObjectType.FILE){
                if (other.gameObject.CompareTag("PlayerMelee") || other.gameObject.CompareTag("PlayerSword") || other.gameObject.CompareTag("PlayerHeavy")){
                    objectToMoveTo = FindPlayerGameobject(other.gameObject);
                    isAttackingObject = false;
                    objectType = ObjectType.PLAYER;
                    transform.LookAt(objectToMoveTo.transform.position);
                    anim.SetTrigger("Run_trig");
                    isMovingToObject = true;
                } else if (other.gameObject.CompareTag("PlayerLaser")){
                    objectToMoveTo = other.gameObject.GetComponent<PlayerLaser>().GetParentGameobject();
                    isAttackingObject = false;
                    objectType = ObjectType.PLAYER;
                    transform.LookAt(objectToMoveTo.transform.position);
                    anim.SetTrigger("Run_trig");
                    isMovingToObject = true;
                }
            }
            if (other.gameObject.CompareTag("PlayerLaser")){
                Destroy(other.gameObject);
            }
        }
    }

    public bool GetIsDead(){
        return isDead;
    }

    public void StopAttackingPlayer(){
        if (objectType == ObjectType.PLAYER){
            isAttackingObject = false;
            objectToMoveTo = GetObjectToAttack();
            isMovingToObject = true;
        }
    }

    public GameObject GetPlayerToAttack(){
        return playerToAttack;
    }

    private void MoveTowardsPlayer(Vector3 locationToMoveTo){
        Vector3 moveHere = new(locationToMoveTo.x,transform.position.y,locationToMoveTo.z);
        enemyAgent.SetDestination(moveHere);
    }

    private GameObject FindPlayerGameobject(GameObject playerObject){
        if (playerObject.transform.parent){
            return FindPlayerGameobject(playerObject.transform.parent.gameObject);
        }
        return playerObject;
    }

    IEnumerator AttackCooldown(){
        canAttack = false;
        yield return new WaitForSeconds(attackCooldownTime);
        anim.ResetTrigger(attackAnimationName);
        print($"I have now reset {attackAnimationName}");
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
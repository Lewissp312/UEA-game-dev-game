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
    private bool canTakeDamageFromMelee;
    private int enemyID;
    private int pointNum;
    private int attackAnimationNamesLen;
    private string attackAnimationName;
    private string[] attackAnimationNames = {"Attack_1_trig","Attack_2_trig","Attack_3_trig"};
    private Animator anim;
    private enum EnemyType{MELEE,SWORD,HEAVY,GUN,ROCKET}
    private enum ObjectType{FILE,PLAYER}
    // private BoxCollider leftHandCollider;
    // private BoxCollider rightHand;
    private GameManager gameManager;
    private GameObject[] filesToAttack;
    private GameObject playerToAttack;
    private GameObject objectToMoveTo;
    private ObjectType objectType;
    // private Vector3 objectPosition;
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
        canTakeDamageFromMelee = true;
        canAttack = true;
        // attackAnimationNames;
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
    }

    // Update is called once per frame
    void Update()
    {
        // print("Anyone there?");
        if (isMovingToObject){
            switch(objectType){
                case ObjectType.FILE:
                    switch(enemyType){
                        case EnemyType.GUN:
                            RaycastHit hit;
                            // Does the ray intersect any file or wall objects
                            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 30, fileMask))
                            { 
                                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.black); 
                                Debug.Log("Did Hit");
                                enemyAgent.ResetPath();
                                transform.LookAt(objectToMoveTo.transform.position);
                                isMovingToObject = false;
                                anim.ResetTrigger("Run_trig");
                                isAttackingObject = true;
                            } else{
                                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white); 
                                Debug.Log("Did not Hit"); 
                            }
                            //The gun enemy can sometimes not look at the file when they are travelling to it, this ensures they do at a certain point
                            if (enemyAgent.remainingDistance < 10){
                                print("I am looking at it as a gun");
                                transform.LookAt(objectToMoveTo.transform.position);
                            }
                            break;
                        default:
                            print($"Remaining distance: {enemyAgent.remainingDistance}");
                            if (enemyAgent.remainingDistance <= 0 && (!enemyAgent.hasPath || enemyAgent.velocity.sqrMagnitude == 0f))
                            {
                                // print("Reached the file now");
                                isMovingToObject = false;
                                anim.ResetTrigger("Run_trig");
                                isAttackingObject = true;
                                //1,2,3,7,8,9 = z
                                //4,5,6,10,11,12 = x
                                Vector3 filePosition = objectToMoveTo.transform.parent.transform.position;
                                Vector3 posToLookAt;
                                switch(pointNum){
                                    //Gets them looking straight ahead at the file once positioned, 
                                    // as enemies looking at the centre can sometimes be stanced diagonally
                                    //TODO: Look at changing this as enemies in the middle already look directly ahead at the file
                                    case 1 or 2 or 3 or 7 or 8 or 9:
                                        posToLookAt = new(transform.position.x,transform.position.y,filePosition.z);
                                        transform.LookAt(posToLookAt);
                                        print("I am looking at it as a melee");
                                        break;
                                    case 4 or 5 or 6 or 10 or 11 or 12:
                                        posToLookAt = new(filePosition.x,transform.position.y,transform.position.z);
                                        transform.LookAt(posToLookAt);
                                        print("I am looking at it as a melee");
                                        break;
                                }
                                // transform.LookAt(objectToMoveTo.transform.parent.transform.position);
                            // enemyAgent.isStopped = true;
                            }
                            break;
                    }
                    break;
                case ObjectType.PLAYER:
                    // print("Enemy is running to the player");
                    MoveTowardsPlayer(objectToMoveTo.transform.position);
                    //Vector3.Distance(transform.position,objectToMoveTo.transform.position)
                    if (Vector3.Distance(transform.position,new(objectToMoveTo.transform.position.x,transform.position.y,objectToMoveTo.transform.position.z)) < 1.4f){
                        enemyAgent.ResetPath();
                        // enemyAgent.isStopped = true;
                        // enemyAgent.SetDestination(enemyAgent.transform.position);
                        // print("Enemy has reached the player");
                        isMovingToObject = false;
                        anim.ResetTrigger("Run_trig");
                        isAttackingObject = true;
                    }
                    break;
            }
        } else if (isAttackingObject && canAttack){
            AttackObject();
            // print("Enemy has attacked the player");
            // CheckIfObjectDestroyed();
            if (objectType == ObjectType.PLAYER){
                switch(enemyType){
                    case EnemyType.GUN or EnemyType.ROCKET:
                        if (Vector3.Distance(transform.position,objectToMoveTo.transform.position) > 5f){
                            // print("Enemy is running after the player");
                            isAttackingObject = false;
                            anim.SetTrigger("Run_trig");
                            isMovingToObject = true;
                            // enemyAgent.isStopped = false;
                        }
                        break;
                    default:
                        if (Vector3.Distance(transform.position,objectToMoveTo.transform.position) > 1.4f){
                            // print("Enemy is running after the player");
                            isAttackingObject = false;
                            anim.SetTrigger("Run_trig");
                            isMovingToObject = true;
                            // enemyAgent.isStopped = false;
                        }
                        break;
                }
            }
        } else if (objectToMoveTo == gameObject){
            objectToMoveTo = GetObjectToAttack();
            anim.SetTrigger("Run_trig");
            isMovingToObject = true;
            // enemyAgent.isStopped = false;
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
            // print(pointNum);
            // print("Hello?");
            if (pointNum != 0){
                // print($"I have chosen a point and I am now running to {examinedObject.transform.position} from my position of {transform.position}");
                objectType = ObjectType.FILE;
                switch(enemyType){
                    case EnemyType.GUN:
                        Vector3 filePosition = examinedObject.transform.parent.transform.position;
                        Vector3 newFilePosition = new(filePosition.x,transform.position.y,filePosition.z);
                        // print($"I'm running to {newFilePosition}");
                        enemyAgent.SetDestination(newFilePosition);
                        // enemyAgent.SetDestination(examinedObject.transform.parent.transform.position);
                        return examinedObject.transform.parent.gameObject;
                    default:
                        // print($"I'm running to {examinedObject.transform.position}");
                        enemyAgent.SetDestination(examinedObject.transform.position);
                        return examinedObject;
                }
            }
        }

        //TODO: Add one here for the items when they are done

        // float closestDistance = 100;
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



        // GameObject closestFile = gameObject;
        // foreach(GameObject file in filesToAttack){
        //     float distanceFromFile = Vector3.Distance(transform.position,file.transform.position);
        //     if (distanceFromFile < closestDistance){
        //         closestDistance = distanceFromFile;
        //         closestFile = file;
        //     }
        // }
        // // needs to be adjusted to the enemies height as if the file height is used the 
        // // enemy will try to run to the middle of the file, which is above them
        // objectPosition = new Vector3(
        //     closestFile.transform.position.x,
        //     transform.position.y,
        //     closestFile.transform.position.z
        // );
        // return closestFile;
    }

    private void AttackObject(){
        if (canAttack){
            print("Hello");
            int randomNum = random.Next(0,attackAnimationNamesLen);
            print(randomNum);
            attackAnimationName = attackAnimationNames[randomNum];
            // print(attackAnimationName);
            anim.SetTrigger(attackAnimationName);
            // print($"Now setting {attackAnimationName}");
            switch(enemyType){
                case EnemyType.MELEE:
                    meleeBoxColliders[attackAnimationName].enabled = true;
                    // anim.SetTrigger("Punch_trig");
                    // leftHandCollider.enabled = true;
                    // rightHand.enabled = true;
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
                    // anim.SetTrigger("Attack_1_trig");
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
            StopAllCoroutines();
            // anim.ResetTrigger("Punch_trig");
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


    // public void OnTriggerEnter(Collider other){
    //     if (other.gameObject.CompareTag("Melee") && canTakeDamageFromMelee){
    //         health -= 10;
    //         //TODO: put attack effects here
    //         // canTakeDamageFromMelee = false;
    //         // StartCoroutine(WaitToTakeMeleeDamage());
    //         if (objectType == ObjectType.FILE){
    //             isAttackingObject = false;
    //             objectToMoveTo = FindPlayerGameobject(other.gameObject);
    //             objectType = ObjectType.PLAYER;
    //             transform.LookAt(objectToMoveTo.transform.position);
    //             anim.SetTrigger("Run_trig");
    //             isMovingToObject = true;
    //             // enemyAgent.isStopped = false;
    //         }
    //     } else if(other.gameObject.CompareTag("Sword") && canTakeDamageFromMelee){

    //     }
        
    //     else if (other.gameObject.CompareTag("PlayerLaser")){
    //         health -= 15;
    //         Destroy(other.gameObject);
    //     } 
    //     if (health <= 0 && !isDead){
    //         StopAllCoroutines();
    //         // anim.ResetTrigger("Punch_trig");
    //         anim.ResetTrigger(attackAnimationName);
    //         anim.SetTrigger("Death_trig");
    //         gameManager.IncreasePlayerPoints(10);
    //         isDead = true;
    //         StartCoroutine(WaitForDeath());
    //     }
    // }

    public bool GetIsDead(){
        return isDead;
    }

    public void StopAttackingPlayer(){
        if (objectType == ObjectType.PLAYER){
            isAttackingObject = false;
            objectToMoveTo = GetObjectToAttack();
            // objectType = ObjectType.FILE;
            isMovingToObject = true;
            // enemyAgent.isStopped = false;
        }
    }

    public GameObject GetPlayerToAttack(){
        return playerToAttack;
    }

    private void MoveTowardsPlayer(Vector3 locationToMoveTo){
        Vector3 moveHere = new(locationToMoveTo.x,transform.position.y,locationToMoveTo.z);
        print($"Distance between me ({transform.position})and target position ({moveHere}): {Vector3.Distance(moveHere,transform.position)}");
        // print($"Distance between me and target position: {Vector3.Distance(moveHere,transform.position)}");
        enemyAgent.SetDestination(moveHere);
        // enemyAgent.SetDestination(locationToMoveTo);
        // transform.SetPositionAndRotation(
        //     Vector3.MoveTowards(
        //         transform.position,locationToMoveTo,moveSpeed
        //     ), 
        //     Quaternion.RotateTowards(
        //         transform.rotation, 
        //         Quaternion.LookRotation(locationToMoveTo - transform.position), 850 * Time.deltaTime
        //     )
        // );
    }

    private GameObject FindPlayerGameobject(GameObject playerObject){
        if (playerObject.transform.parent){
            return FindPlayerGameobject(playerObject.transform.parent.gameObject);
        }
        return playerObject;
    }

    IEnumerator AttackCooldown(){ //could probably make this into a generic "AttackCooldown"
    //time to wait would be a variable that is established on start
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
            // case EnemyType.GUN:
            //     // anim.ResetTrigger("Shoot_small_trig");
            //     break;
        }
        canAttack = true;
    }

    IEnumerator WaitToTakeMeleeDamage(){
        yield return new WaitForSeconds(0.5f);
        canTakeDamageFromMelee = true;
    }

    IEnumerator WaitForDeath(){
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

}
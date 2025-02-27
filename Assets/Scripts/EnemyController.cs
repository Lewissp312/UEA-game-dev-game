using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyController : MonoBehaviour
{
    private bool canAttack;
    private bool isDead;
    private bool isMovingToObject;
    private bool isAttackingObject;
    private bool canTakeDamageFromMelee;
    private enum EnemyType{MELEE,SMALL_RANGED}
    private enum ObjectType{FILE,PLAYER}
    private int enemyID;
    private Animator anim;
    private BoxCollider leftHandCollider;
    private BoxCollider rightHandCollider;
    private GameManager gameManager;
    private GameObject[] filesToAttack;
    private GameObject playerToAttack;
    private GameObject objectToMoveTo;
    private ObjectType objectType;
    private Vector3 objectPosition;
    [SerializeField] private float attackCooldownTime;
    [SerializeField] private float moveSpeed;
    [SerializeField] private int health;
    [SerializeField] private EnemyType enemyType;
    // Start is called before the first frame update
    void Start()
    {
        canTakeDamageFromMelee = true;
        canAttack = true;
        objectType = ObjectType.FILE;
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        enemyID = gameManager.GetEnemyID();
        anim = GetComponent<Animator>();
        print($"Anim is here {anim}");
        leftHandCollider = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).GetComponent<BoxCollider>();
        rightHandCollider = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).transform.GetChild(2).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).GetComponent<BoxCollider>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        objectToMoveTo = gameObject;
        filesToAttack = gameManager.GetFilesToAttack();
        gameManager.IncreaseEnemyNum();
    }

    // Update is called once per frame
    void Update()
    {
        if (isMovingToObject){
            switch(objectType){
                case ObjectType.FILE:
                    MoveTowardsLocation(objectPosition);
                    break;
                case ObjectType.PLAYER:
                    MoveTowardsLocation(objectToMoveTo.transform.position);
                    if (Vector3.Distance(transform.position,objectToMoveTo.transform.position) < 1.4f){
                        isMovingToObject = false;
                        anim.ResetTrigger("Run_trig");
                        isAttackingObject = true;
                    }
                    break;
            }
            // if (objectToMoveTo.CompareTag("File")){
            //     MoveTowardsLocation(objectPosition);
            // } else{
            //     MoveTowardsLocation(objectToMoveTo.transform.position);
            //     if (Vector3.Distance(transform.position,objectToMoveTo.transform.position) < 1.4f){
            //         isMovingToObject = false;
            //         anim.ResetTrigger("Run_trig");
            //         isAttackingObject = true;
            //     }
            // }
        } else if (isAttackingObject && canAttack){
            AttackObject();
            // CheckIfObjectDestroyed();
            if (objectType == ObjectType.PLAYER){
                if (Vector3.Distance(transform.position,objectToMoveTo.transform.position) > 1.4f){
                    isAttackingObject = false;
                    anim.SetTrigger("Run_trig");
                    isMovingToObject = true;
                }
            }
        } else if (objectToMoveTo == gameObject){
            objectToMoveTo = GetClosestFile();
            anim.SetTrigger("Run_trig");
            isMovingToObject = true;
        }
    }

    private GameObject GetClosestFile(){
        float closestDistance = 100;
        GameObject closestFile = gameObject;
        foreach(GameObject file in filesToAttack){
            float distanceFromFile = Vector3.Distance(transform.position,file.transform.position);
            if (distanceFromFile < closestDistance){
                closestDistance = distanceFromFile;
                closestFile = file;
            }
        }
        // needs to be adjusted to the enemies height as if the file height is used the 
        // enemy will try to run to the middle of the file, which is above them
        objectPosition = new Vector3(
            closestFile.transform.position.x,
            transform.position.y,
            closestFile.transform.position.z
        );
        return closestFile;
    }

    private void AttackObject(){
        if (canAttack){
            switch(enemyType){
                case EnemyType.MELEE:
                    anim.SetTrigger("Punch_trig");
                    // leftHandCollider.enabled = true;
                    rightHandCollider.enabled = true;
                    StartCoroutine(AttackCooldown());
                    break;
            }
        }
    }

    public int GetEnemyID(){
        return enemyID;
    }

    public void OnTriggerEnter(Collider other){

        if (other.gameObject.CompareTag("Melee") && canTakeDamageFromMelee){
            health -= 10;
            //TODO: adjust when different status effects or buffs are used (e.g taking less damage or increased damage)
            canTakeDamageFromMelee = false;
            StartCoroutine(WaitToTakeMeleeDamage());
            if (objectType == ObjectType.FILE){
                isAttackingObject = false;
                objectToMoveTo = FindPlayerGameobject(other.gameObject);
                objectType = ObjectType.PLAYER;
                transform.LookAt(objectToMoveTo.transform.position);
                anim.SetTrigger("Run_trig");
                isMovingToObject = true;
            }
        } else if (other.gameObject.CompareTag("Laser")){
            health -= 15;
            Destroy(other.gameObject);
        } else if (other.gameObject == objectToMoveTo && objectType == ObjectType.FILE){
            isMovingToObject = false;
            anim.ResetTrigger("Run_trig");
            isAttackingObject = true;
        }
        if (health <= 0 && !isDead){
            StopAllCoroutines();
            anim.ResetTrigger("Punch_trig");
            anim.SetTrigger("Death_trig");
            gameManager.IncreasePlayerPoints(10);
            isDead = true;
            StartCoroutine(WaitForDeath());
        }
    }

    public bool GetIsDead(){
        return isDead;
    }

    public void StopAttackingPlayer(){
        if (objectType == ObjectType.PLAYER){
            isAttackingObject = false;
            objectToMoveTo = GetClosestFile();
            objectType = ObjectType.FILE;
            isMovingToObject = true;
        }
    }

    public GameObject GetPlayerToAttack(){
        return playerToAttack;
    }

    private void MoveTowardsLocation(Vector3 locationToMoveTo){
        transform.SetPositionAndRotation(
            Vector3.MoveTowards(
                transform.position,locationToMoveTo,moveSpeed
            ), 
            Quaternion.RotateTowards(
                transform.rotation, 
                Quaternion.LookRotation(locationToMoveTo - transform.position), 850 * Time.deltaTime
            )
        );
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
        switch(enemyType){
            case EnemyType.MELEE:
                //TODO: enable certain colliders depending on what melee attack is being used 
                // (e.g right hand for normal punch, right leg for kick)
                // leftHandCollider.enabled = false;
                rightHandCollider.enabled = false;
                anim.ResetTrigger("Punch_trig");
                break;
            case EnemyType.SMALL_RANGED:
                anim.ResetTrigger("Shoot_small_trig");
                break;
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
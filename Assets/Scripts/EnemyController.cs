using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyController : MonoBehaviour
{
    private int enemyID;
    //TODO: Vary speed of enemies so that players can actually escape them. 
    // Make melee enemy speed slightly slower than player melee speed
    private float moveSpeed = 10;
    private bool canAttack;
    private bool isDead;
    private bool isMovingToObject;
    private bool isAttackingObject;
    private bool canTakeDamageFromMelee;
    private Vector3 objectPosition;
    private BoxCollider leftHandCollider;
    private BoxCollider rightHandCollider;

    private enum EnemyType{MELEE,SMALL_RANGED}
    private GameManager gameManager;
    private GameObject[] filesToAttack;
    private GameObject playerToAttack;
    private GameObject objectToMoveTo;
    private Animator anim;
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private float attackCooldownTime;
    [SerializeField] private int health;
    // Start is called before the first frame update
    void Start()
    {
        leftHandCollider = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).GetComponent<BoxCollider>();
        rightHandCollider = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).transform.GetChild(2).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).GetComponent<BoxCollider>();
        anim = GetComponent<Animator>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerToAttack = gameObject;
        objectToMoveTo = gameObject;
        filesToAttack = gameManager.GetFilesToAttack();
        canTakeDamageFromMelee = true;
        canAttack = true;
        moveSpeed = 0.07f;
        enemyID = gameManager.GetEnemyID();
        gameManager.IncreaseEnemyNum();
    }

    // Update is called once per frame
    void Update()
    {
        if (isMovingToObject){
            if (objectToMoveTo.CompareTag("File")){
                MoveTowardsLocation(objectPosition);
            } else{
                MoveTowardsLocation(objectToMoveTo.transform.position);
            }
        } else if (isAttackingObject && canAttack){
            AttackObject();
            // CheckIfObjectDestroyed();
            if (objectToMoveTo.CompareTag("Player")){
                if (Vector3.Distance(transform.position,objectToMoveTo.transform.position) > 1.5f){
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
            if (objectToMoveTo.CompareTag("File")){
                isAttackingObject = false;
                objectToMoveTo = FindPlayerGameobject(other.gameObject);
                transform.LookAt(objectToMoveTo.transform.position);
                anim.SetTrigger("Run_trig");
                isMovingToObject = true;
            }
        } else if (other.gameObject.CompareTag("Laser")){
            health -= 5;
            Destroy(other.gameObject);
        } else if (other.gameObject == objectToMoveTo){
            isMovingToObject = false;
            anim.ResetTrigger("Run_trig");
            isAttackingObject = true;
        }
        if (health <= 0){
            anim.ResetTrigger("Punch_trig");
            anim.SetTrigger("Death_trig");
            isDead = true;
            StartCoroutine(WaitForDeath());
        }
    }

    public bool GetIsDead(){
        return isDead;
    }

    public void StopAttackingPlayer(){
        isAttackingObject = false;
        objectToMoveTo = GetClosestFile();
        isMovingToObject = true;
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
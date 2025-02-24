using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

// using Unity.VisualScripting;
// using UnityEditor;
// using UnityEditorInternal;
using UnityEngine;

//original scale: Vector3(10.9097452,0.0169142466,11.5897226)

public class CubeMovement : MonoBehaviour
{
    private enum PlayerType{MELEE,SMALL_RANGED}
    private float playerSpeed;
    private int attackCooldownTime;
    private int enemyToAttackID;
    private bool isOverPlayer;
    private bool isMovingToPosition;
    private bool isPlayerClicked;
    private bool isLockedOntoEnemy;
    private bool isMovingToEnemy;
    private bool canAttack;
    private BoxCollider leftHandCollider;
    private BoxCollider rightHandCollider;
    private Animator playerAnim;
    private Dictionary<int,GameObject> enemyInfo;
    private GameObject enemyToAttack;
    private Enemy enemyToAttackScript;
    private Vector3 mousePosition;
    private Vector3 positionToMoveTo;
    private Vector3 lastGoodMousePos;
    private Color ringColor;
    private Color ringColorTrans;
    private Renderer ringRenderer;
    private ParticleSystem selectedEffect; 
    [SerializeField] private PlayerType playerType;
    [SerializeField] private GameObject laser;
    // Start is called before the first frame update
    void Start()
    {
        playerSpeed = 0.07f;
        enemyInfo = new();
        leftHandCollider = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).GetComponent<BoxCollider>();
        rightHandCollider = transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).transform.GetChild(2).transform.GetChild(0).transform.GetChild(0).
        transform.GetChild(0).GetComponent<BoxCollider>();
        //1,0,0,0,1,0,0,0
        playerAnim = GetComponent<Animator>();
        selectedEffect = GetComponent<ParticleSystem>();
        ringRenderer = transform.Find("PlayerAttackArea").gameObject.GetComponent<Renderer>();
        ringColor = new Color(0.96f,0.96f,0.51f,0.5f);
        ringColorTrans = new Color(0.96f,0.96f,0.51f,0f);
        canAttack = true;
        //This would not be needed in a full version, as this would be set when instantiated by a gameManager (possibly)
        if (playerType == PlayerType.MELEE){
            attackCooldownTime = 1;
        } else{
            attackCooldownTime = 2;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(3)){
            if (!isMovingToPosition){
                mousePosition = GetMouseOnBoardPosition(out isOverPlayer);
                if (isOverPlayer){ //May need to include a check here later to see if 
                // gameManager is preparing an ultimate and this player has just been selected, because in that case
                // the isClicked/highlighting effect would not be needed
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
                    transform.SetPositionAndRotation(Vector3.MoveTowards(transform.position,enemyToAttack.transform.position,playerSpeed), 
                    Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(enemyToAttack.transform.position - transform.position), 850 * Time.deltaTime));
                    // print(Vector3.Distance(transform.position,enemyToAttack.transform.position));
                    if (Vector3.Distance(transform.position,enemyToAttack.transform.position) <= 1.4f){
                        isMovingToEnemy = false;
                        playerAnim.ResetTrigger("Run_trig");
                        canAttack = true;
                    }
                }
                else if(canAttack){
                    //could use a switch here
                    if (playerType == PlayerType.SMALL_RANGED){
                        //ideally would control different aspects of a player character with variables, such as "canFire", 
                        // "canMoveToEnemy", "timeToReload" etc. to help checks like this
                        transform.LookAt(enemyToAttack.transform.position);
                        //start firing animation
                        GameObject newLaser = Instantiate(laser,transform.position,transform.rotation);
                        newLaser.GetComponent<PlayerLaser>().SetFireDirection(enemyToAttack.transform.position);
                        StartCoroutine(AttackCooldown());
                        // fireDirection = speed * Time.deltaTime * -transform.forward;
                        //change to object pooling to be more effecient
                    } else if(playerType == PlayerType.MELEE){
                        //Could perhaps activate colliders here for hands, could make them bigger to hit more enemies
                        //Would need to make sure that all colliders are deactivated if the player chatacter is moved away too
                        if (enemyToAttackScript.GetIsDead() || enemyToAttack.IsDestroyed()){
                            canAttack = false;
                            isLockedOntoEnemy = false;
                            enemyInfo.Remove(enemyToAttackID);
                        } else{
                            playerAnim.SetTrigger("Punch_trig");
                            leftHandCollider.enabled = true;
                            rightHandCollider.enabled = true;
                            StartCoroutine(AttackCooldown());
                        }
                    }
                }
                //if you are within range of the enemy now, attack them
                //When the enemy dies, isAttackingEnemy must be set to false
                //have a variable here that is set by collision with the player. If they collide, then don't move any further forward
                //When this happens, an attack is executed, and moving forward is prohibited until the attack is done (could be a combo maybe)
                //This would have to be different for ranged weapons. In this case, the check is not needed, as they can fire wherever and don't
                //need to get close
            } else if (enemyInfo.Count > 0){
                enemyToAttack = GetClosestEnemy();
                enemyToAttackScript = enemyToAttack.GetComponent<Enemy>();
                enemyToAttackID = enemyToAttackScript.GetEnemyID();
                isLockedOntoEnemy = true;
                // transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(enemyToAttack.transform.position - transform.position), 850 * Time.deltaTime);
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

        if (isMovingToPosition){
            transform.SetPositionAndRotation(Vector3.MoveTowards(transform.position,positionToMoveTo,playerSpeed), 
            Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(positionToMoveTo - transform.position), 850 * Time.deltaTime));
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
        // if (other.gameObject.CompareTag("Enemy") && playerType == PlayerType.MELEE)
        // {
        //     if (other.gameObject == enemyToAttack){
        //         isMovingToEnemy = false;


        //         //Do the attack here
        //         //only do the attack when you encounter the enemy but the attack should also affect other enemies
        //         //enemy will probably have a bit of knockback so will need to move to them again
        //         //Check if enemy has died after attack, enemy can perhaps set the isLockedOntoEnemy
        //         //do the attack
        //         //set isMovingToEnemy to be true again (will probably need to keep track of the duration of the animation)
        //         //need to wait until the animation is done before moving the player
        //     }
        //     // if(Physics.Raycast(origin, forward, hitRange, out hit))
        //     // {
        //     //     if(hit.transform.gameObject.tag == "Enemy")
        //     //     {
        //     //         hit.transform.gameObject.SendMessage("TakeDamage", 30);
        //     //     }
        //     // }
        // }
    }

    private GameObject GetClosestEnemy(){
        float lowestDistance = 100;
        float tempDistance;
        GameObject lowestDistanceEnemy = gameObject;
        foreach(KeyValuePair<int, GameObject> enemies in enemyInfo){
            tempDistance = Vector3.Distance(transform.position,enemies.Value.transform.position);
            if (tempDistance < lowestDistance){
                lowestDistance = tempDistance;
                lowestDistanceEnemy = enemies.Value;
            }
        }
        return lowestDistanceEnemy;
    }

    // private BoxCollider FindHandCollider(string hand){
        
    // }

    public void AddToEnemyList(int enemyID, GameObject enemy){
        enemyInfo.Add(enemyID,enemy);
        // Debug.Log(enemyInfo[enemyID].transform.position);
    }

    public void RemoveFromEnemyList(int enemyID){
        //Would call this when an enemy leaves a player's field, using OnTriggerExit. 
        //However, this would need to be placed in gameManager, as the enemy could die while still inside the circle but
        //not by the player's hand. They would still be in the player's list. When the enemy dies 
        // the function is called from gameManager to go through all player's lists and remove that enemy.
        //For now it's in here
        enemyInfo.Remove(enemyID);
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

        //could possibly have parameters for hit range of attack and attack to be executed? 
        //also might need to use this for group attacks like rockets 
        // (but could maybe have enemies take damage if they get caught 
        // in the explosion effect, with possible varying damage depending 
        // on how far they are from the explosion)?
        //Gunner enemies only attack one person so no need for this, 
        // but would need to make sure that bullets collide with other 
        // things (like walls and other enemies)

    }

    IEnumerator AttackCooldown(){ //could probably make this into a generic "AttackCooldown"
    //time to wait would be a variable that is established on start
        // playerAnim.SetTrigger("Punch_trig");
        //deactivate hand colliders here
        canAttack = false;
        switch(playerType){
            case PlayerType.MELEE:
                yield return new WaitForSeconds(0.3f);
                leftHandCollider.enabled = false;
                rightHandCollider.enabled = false;
                playerAnim.ResetTrigger("Punch_trig");
                break;
            case PlayerType.SMALL_RANGED:
                yield return new WaitForSeconds(0.3f);
                break;
        }
        canAttack = true;
    }
}


//An attack's conditions have been met for launch. 
// If the attack is a ranged attack, such as a basic gun or rocket, the player does not need
//to move closer to the enemy, as they can fire from where they are. If it is a melee attack however, the player
//must move to the enemy to hit them. Melee players will have some sort of object or just their fists that they use to attack the enemy.
//Objects (such as swords and fists) will have colliders. If an enemy comes into contact with this collider, they will take damage.
//The player should move as close as they can, with the distance between the player and the enemy being measured to see how close they are.
//Once they get into a certain range, do the attack.


using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls all file behaviour
/// </summary>
public class FileController : MonoBehaviour
{
    private int maximumHealth;
    private GameManager gameManager;
    private Dictionary<string,bool> pointIsOccupiedDict;
    private Dictionary<string,GameObject> pointPositionsDict;
    [SerializeField] private int health;
    [SerializeField] private HealthBar healthBar; 

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Unity methods
    void Start()
    {
        maximumHealth = health;
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        pointIsOccupiedDict = new Dictionary<string, bool>();
        pointPositionsDict = new Dictionary<string, GameObject>();
        int numOfPoints = 0;
        //Two dictionaries are set up.
        //One keeps track of whether a point is occupied, the other stores the point gameobjects
        foreach(Transform child in transform)
        {
            if(child.CompareTag("Point"))
            {
                pointPositionsDict.Add($"Point {numOfPoints+1}", child.gameObject);
                numOfPoints++;
            }
        }
        for(int i=0; i<numOfPoints; i++){
            pointIsOccupiedDict.Add($"Point {i+1}", false);
        }
    }

    void OnTriggerEnter(Collider other){
        if(other.gameObject.CompareTag("EnemyMelee")){
            gameManager.IncreaseEnemyPoints(1);
            health -= 5; 
            healthBar.UpdateHealth(health,maximumHealth);
        } else if(other.gameObject.CompareTag("EnemyLaser")){
            gameManager.IncreaseEnemyPoints(2);
            health -= 10; 
            healthBar.UpdateHealth(health,maximumHealth);
            Destroy(other.gameObject);
        } else if(other.gameObject.CompareTag("EnemySword")){
            gameManager.IncreaseEnemyPoints(3);
            health -= 20; 
            healthBar.UpdateHealth(health,maximumHealth);
        } else if(other.gameObject.CompareTag("EnemyHeavy")){
            gameManager.IncreaseEnemyPoints(4);
            health -= 30; 
            healthBar.UpdateHealth(health,maximumHealth);
        }
        if(health <= 0){
            gameManager.GameOver(GameManager.GameOverCause.FILEDESTROYED);
        }
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Public class methods

    public void MakePointAvailable(int pointNum){
        pointIsOccupiedDict[$"Point {pointNum}"] = false;
    }

    public GameObject GetAvailablePoint(out int pointNum){
        int i = 0;
        foreach(KeyValuePair<string, bool> point in pointIsOccupiedDict)
        {
            //If the point is not occupied
            if(!point.Value){
                //Set the point to occupied
                pointIsOccupiedDict[point.Key] = true;
                //Give the enemy the number of this point so they can make it free later
                pointNum = i+1;
                //Return the position of the point
                return pointPositionsDict[point.Key];
            }
            i++;
        }
        //If a point is not found
        pointNum = 0;
        return gameObject;
    }
}

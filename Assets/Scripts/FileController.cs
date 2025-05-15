using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class FileController : MonoBehaviour
{
    private int maximumHealth;
    private GameManager gameManager;
    private Dictionary<string,bool> pointIsOccupiedDict;
    private Dictionary<string,GameObject> pointPositionsDict;
    [SerializeField] private int health;
    [SerializeField] private HealthBar healthBar; 
    void Start()
    {
        maximumHealth = health;
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        pointIsOccupiedDict = new Dictionary<string, bool>();
        pointPositionsDict = new Dictionary<string, GameObject>();
        int numOfPoints = 0;
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Point"))
            {
                pointPositionsDict.Add($"Point {numOfPoints+1}", child.gameObject);
                numOfPoints++;
            }
        }
        for (int i=0; i<numOfPoints; i++){
            pointIsOccupiedDict.Add($"Point {i+1}", false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject GetAvailablePoint(out int pointNum){
        int i = 0;
        foreach(KeyValuePair<string, bool> point in pointIsOccupiedDict)
        {
            if (!point.Value){
                pointIsOccupiedDict[point.Key] = true;
                pointNum = i+1;
                return pointPositionsDict[point.Key];
            }
            i++;
        }
        pointNum = 0;
        return gameObject;
    }

    public void MakePointAvailable(int pointNum){
        pointIsOccupiedDict[$"Point {pointNum}"] = false;
    }

    // public void DecreaseHealth(int healthToTakeAway){
    //     health -= healthToTakeAway;
    //     healthBar.UpdateHealth(health,maximumHealth);
    // }

    //5,10,20,30

    private void OnTriggerEnter(Collider other){
        if (other.gameObject.CompareTag("EnemyMelee")){
            gameManager.IncreaseEnemyPoints(1);
            health -= 5; 
            healthBar.UpdateHealth(health,maximumHealth);
        } else if (other.gameObject.CompareTag("EnemyLaser")){
            gameManager.IncreaseEnemyPoints(2);
            health -= 10; 
            healthBar.UpdateHealth(health,maximumHealth);
            Destroy(other.gameObject);
        } else if (other.gameObject.CompareTag("EnemySword")){
            gameManager.IncreaseEnemyPoints(3);
            health -= 20; 
            healthBar.UpdateHealth(health,maximumHealth);
        } else if (other.gameObject.CompareTag("EnemyHeavy")){
            gameManager.IncreaseEnemyPoints(4);
            health -= 30; 
            healthBar.UpdateHealth(health,maximumHealth);
        }
        if (health <= 0){
            gameManager.GameOver(GameManager.GameOverCause.FILEDESTROYED);
        }
    }
}

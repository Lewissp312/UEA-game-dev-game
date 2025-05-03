using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class FileController : MonoBehaviour
{
    private GameManager gameManager;
    private Dictionary<string,bool> pointIsOccupiedDict;
    private Dictionary<string,GameObject> pointPositionsDict; 
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        pointIsOccupiedDict = new Dictionary<string, bool>();
        pointPositionsDict = new Dictionary<string, GameObject>();
        int numOfPoints = 0;
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Point"))
            {
                pointPositionsDict.Add($"Point {numOfPoints+1}", child.gameObject);
                // print(pointPositionsDict[$"Point {numOfPoints+1}"].transform.localPosition);
                // print(pointPositionsDict[$"Point {numOfPoints+1}"]);
                numOfPoints++;
            }
        }
        // numOfPoints++;
        // print($"There are {numOfPoints}, I just counted");
        for (int i=0; i<numOfPoints; i++){
            pointIsOccupiedDict.Add($"Point {i+1}", false);
            // print($"Just added {i+1}");
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
                // print($"Yeah you can go here, going to {pointPositionsDict[point.Key].transform.position}");
                pointIsOccupiedDict[point.Key] = true;
                pointNum = i+1;
                // print($"Available position! it's {i+1}, go to {pointPositionsDict[point.Key].transform.position}");
                return pointPositionsDict[point.Key];
            }
            i++;
            print(i);
        }
        pointNum = 0;
        return gameObject;
    }

    public void MakePointAvailable(string pointName){
        pointIsOccupiedDict[pointName] = false;
    }

    private void OnTriggerEnter(Collider other){
        // print(other.gameObject);
        if (other.gameObject.CompareTag("EnemyMelee")){
            gameManager.IncreaseEnemyPoints(1);
            //TODO: implement different point levels for different enemy attacks
        } else if (other.gameObject.CompareTag("EnemyLaser")){
            gameManager.IncreaseEnemyPoints(2);
            Destroy(other.gameObject);
        } else if (other.gameObject.CompareTag("EnemySword")){
            gameManager.IncreaseEnemyPoints(3);
            // Destroy(other.gameObject);
        } else if (other.gameObject.CompareTag("EnemyHeavy")){
            gameManager.IncreaseEnemyPoints(4);
            // Destroy(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        print(other.gameObject);
        // if (other.gameObject.CompareTag("EnemyMelee")){
        //     gameManager.IncreaseEnemyPoints(1);
        //     //TODO: implement different point levels for different enemy attacks
        // } else if (other.gameObject.CompareTag("EnemyLaser")){
        //     gameManager.IncreaseEnemyPoints(2);
        //     Destroy(other.gameObject);
        // }
    }
}

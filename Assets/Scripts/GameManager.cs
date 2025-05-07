using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private int enemyID;
    private int playerPoints;
    private int enemyPoints;
    private GameObject[] filesToAttack;
    private GameObject[] spawnPoints;
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private TextMeshProUGUI playerPointsText;
    [SerializeField] private TextMeshProUGUI enemyPointsText;

    // private GameObject[] filesToAttack;
    // Start is called before the first frame update
    void Start()
    {
        enemyID = 0;
        filesToAttack = GameObject.FindGameObjectsWithTag("File");
        spawnPoints = new GameObject[296];
        int numOfSpawnPoints = 0;
        foreach (Transform child in transform)
        {
            if (child.CompareTag("SpawnPoint"))
            {
                spawnPoints[numOfSpawnPoints] = child.gameObject;
                Instantiate(enemies[0],child.gameObject.transform.position,enemies[0].transform.rotation);
                numOfSpawnPoints++;
            }
        }
        // foreach(GameObject spawnPoint in spawnPoints){
        //     Instantiate(enemies[0],spawnPoint.transform.position,enemies[0].transform.rotation);
        // }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public int GetEnemyID(){
        return enemyID;
    }

    public GameObject[] GetFilesToAttack(){
        return GameObject.FindGameObjectsWithTag("File");
    }

    public void IncreaseEnemyNum(){
        enemyID++;
    }

    public void IncreasePlayerPoints(int points){
        playerPoints+=points;
        playerPointsText.text = $"<color=yellow>Player Points: {playerPoints}</color>";
    }

    public void IncreaseEnemyPoints(int points){
        enemyPoints+=points;
        enemyPointsText.text = $"<color=purple>Enemy Points: {enemyPoints}</color>";
    }


}
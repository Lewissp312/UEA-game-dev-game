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
    [SerializeField] private TextMeshProUGUI playerPointsText;
    [SerializeField] private TextMeshProUGUI enemyPointsText;

    // private GameObject[] filesToAttack;
    // Start is called before the first frame update
    void Start()
    {
        enemyID = 0;
        filesToAttack = GameObject.FindGameObjectsWithTag("File");
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
        playerPointsText.text = $"Player Points: {playerPoints}";
    }

    public void IncreaseEnemyPoints(int points){
        enemyPoints+=points;
        enemyPointsText.text = $"Enemy Points: {enemyPoints}";
    }


}
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int enemyID;
    private GameObject[] filesToAttack;
    // Start is called before the first frame update
    void Start()
    {
        filesToAttack = GameObject.FindGameObjectsWithTag("File");
        enemyID = 0;
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


}
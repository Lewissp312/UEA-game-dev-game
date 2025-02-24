using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int enemyID;
    // Start is called before the first frame update
    void Start()
    {
        enemyID = 0;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public int GetEnemyID(){
        return enemyID;
    }

    public void IncreaseEnemyNum(){
        enemyID++;
    }


}
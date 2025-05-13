using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private bool isMenuActive;
    private bool canSelectItemSpace;
    private int enemyID;
    private int playerPoints;
    private int enemyPoints;
    private int itemSpacesLen;
    private System.Random random;
    private Dictionary<int,GameObject> activeEnemies;
    private GameObject[] filesToAttack;
    private GameObject[] itemSpaces;
    private GameObject[] spawnPoints;
    public enum ItemSpaceItems{BURN,SLOWNESS,GUN,NONE}
    public enum ItemSpaceOwner{PLAYER,ENEMY,NONE};
    [SerializeField] private GameObject fileContainer;
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private TextMeshProUGUI playerPointsText;
    [SerializeField] private TextMeshProUGUI enemyPointsText;

    // private GameObject[] filesToAttack;
    // Start is called before the first frame update
    void Start()
    {
        canSelectItemSpace = true;
        enemyID = 0;
        playerPoints = 100;
        random = new System.Random();
        activeEnemies = new();
        filesToAttack = new GameObject[fileContainer.transform.childCount];
        for (int i = 0; i < fileContainer.transform.childCount;i++){
            filesToAttack[i] = fileContainer.transform.GetChild(i).gameObject;
        }
        itemSpaces = GameObject.FindGameObjectsWithTag("ItemSpace");
        itemSpacesLen = itemSpaces.Length;
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
        if (canSelectItemSpace && enemyPoints >= 20){
            for(int i = 0; i<itemSpacesLen; i++){
                GameObject chosenItemSpace = itemSpaces[random.Next(itemSpacesLen)];
                ItemSpaceController itemSpaceScript = chosenItemSpace.GetComponent<ItemSpaceController>();
                if (itemSpaceScript.GetItemSpaceOwner() == ItemSpaceOwner.NONE){
                    itemSpaceScript.SetItemSpaceOwner(ItemSpaceOwner.ENEMY);
                    if (enemyPoints < 30){
                        itemSpaceScript.InstantiateItem(ItemSpaceItems.GUN);
                    } else{
                        //Initialisation needed here, using this default value is impossible
                        ItemSpaceItems itemChoice = ItemSpaceItems.SLOWNESS;
                        if(enemyPoints < 50){ //Gun (20 points) and slowness area (30 points)
                            ItemSpaceItems[] itemChoices = {ItemSpaceItems.SLOWNESS,ItemSpaceItems.GUN};
                            itemChoice = itemChoices[random.Next(itemChoices.Length)];
                        } else if (enemyPoints > 50){
                            ItemSpaceItems[] itemChoices = {ItemSpaceItems.BURN,ItemSpaceItems.SLOWNESS,ItemSpaceItems.GUN};
                            itemChoice = itemChoices[random.Next(itemChoices.Length)];
                        }
                        itemSpaceScript.InstantiateItem(itemChoice);
                        // switch(itemChoice){
                        //     case ItemSpaceItems.BURN or ItemSpaceItems.SLOWNESS:
                        //         itemSpaceScript.InstantiateItem(itemChoice);
                        //         break;
                        //     case ItemSpaceItems.GUN:
                        //         itemSpaceScript.InstantiateItem(ItemSpaceItems.GUN);
                        //         break;
                        // }
                    }
                    break;
                }
            }
            canSelectItemSpace = false;
            StartCoroutine(WaitForItemSpaceSelection());
        }


    }

    public int GetEnemyID(){
        return enemyID;
    }

    public int GetPlayerPoints(){
        return playerPoints;
    }

    public bool GetIsMenuActive(){
        return isMenuActive; 
    }

    public GameObject[] GetFilesToAttack(){
        return GameObject.FindGameObjectsWithTag("File");
    }

    public void AddToActiveEnemies(int enemyID, GameObject enemy){
        activeEnemies.Add(enemyID,enemy);
    }

    public void RemoveFromActiveEnemies(int enemyID){
        activeEnemies.Remove(enemyID);
    }

    public void SetIsMenuActive(bool isMenuActive){
        this.isMenuActive = isMenuActive;
    }

    public void IncreaseEnemyNum(){
        enemyID++;
    }

    public void IncreasePlayerPoints(int points){
        playerPoints+=points;
        playerPointsText.text = $"<color=yellow>Player Points: {playerPoints}</color>";
    }

    public void DecreasePlayerPoints(int pointsToSubtract){
        playerPoints -= pointsToSubtract;
        playerPointsText.text = $"<color=yellow>Player Points: {playerPoints}</color>";
    }

    public void IncreaseEnemyPoints(int points){
        enemyPoints+=points;
        enemyPointsText.text = $"<color=purple>Enemy Points: {enemyPoints}</color>";
    }

    IEnumerator WaitForItemSpaceSelection(){
        yield return new WaitForSeconds(10);
        canSelectItemSpace = true;
    }


}
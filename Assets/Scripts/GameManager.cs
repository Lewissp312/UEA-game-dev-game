using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the game loop
/// </summary>
public class GameManager : MonoBehaviour
{
    private bool isMenuActive;
    private bool isGameActive;
    private bool isBetweenWaves;
    private bool canSelectItemSpace;
    private int enemyID;
    private int playerPoints;
    private int enemyPoints;
    private int itemSpacesLen;
    private int enemiesLen;
    private int playersLen;
    private int wave;
    private int miniWave;
    private int secondsLeft;
    public enum GameOverCause{FINISHED,FILEDESTROYED,PLAYERSDEAD};
    private System.Random random;
    private Dictionary<int,GameObject> activeEnemies;
    private GameObject[] filesToAttack;
    private GameObject[] itemSpaces;
    private GameObject[] spawnPoints;
    public enum ItemSpaceItems{BURN,SLOWNESS,GUN,NONE}
    public enum ItemSpaceOwner{PLAYER,ENEMY,NONE};
    [SerializeField] private GameObject[] players;
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private GameObject fileContainer;
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private TextMeshProUGUI playerPointsText;
    [SerializeField] private TextMeshProUGUI enemyPointsText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI countdownTimer;

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Unity methods
    void Start()
    {
        canSelectItemSpace = true;
        isBetweenWaves = true;
        enemyID = 0;
        playerPoints = 100;
        wave = 1;
        enemiesLen = enemies.Length;
        playersLen = players.Length;
        random = new System.Random();
        activeEnemies = new();
        filesToAttack = new GameObject[fileContainer.transform.childCount];
        for(int i = 0; i < fileContainer.transform.childCount; i++){
            filesToAttack[i] = fileContainer.transform.GetChild(i).gameObject;
        }
        itemSpaces = GameObject.FindGameObjectsWithTag("ItemSpace");
        itemSpacesLen = itemSpaces.Length;
        spawnPoints = new GameObject[60];
        int numOfSpawnPoints = 0;
        foreach(Transform child in transform)
        {
            if(child.CompareTag("SpawnPoint"))
            {
                spawnPoints[numOfSpawnPoints] = child.gameObject;
                numOfSpawnPoints++;
            }
        }
        playerPointsText.text = $"<color=yellow>Player Points: {playerPoints}</color>";
        enemyPointsText.text = $"<color=purple>Enemy Points: {enemyPoints}</color>";
        waveText.text = $"Wave: {wave}";
    }

    void Update()
    {
        if(isGameActive && !isBetweenWaves && canSelectItemSpace && enemyPoints >= 20){
            for(int i = 0; i<itemSpacesLen; i++){
                GameObject chosenItemSpace = itemSpaces[random.Next(itemSpacesLen)];
                ItemSpaceController itemSpaceScript = chosenItemSpace.GetComponent<ItemSpaceController>();
                if(itemSpaceScript.GetItemSpaceOwner() == ItemSpaceOwner.NONE){
                    itemSpaceScript.SetItemSpaceOwner(ItemSpaceOwner.ENEMY);
                    if(enemyPoints < 30){ //Gun (20 points)
                        //This is the only item that can be afforded with less than 30 points
                        itemSpaceScript.InstantiateItem(ItemSpaceItems.GUN);
                    } else{
                        //Using this default initialsation value is impossible
                        ItemSpaceItems itemChoice = ItemSpaceItems.SLOWNESS;
                        if(enemyPoints < 50){ //Slowness area (30 points)
                            ItemSpaceItems[] itemChoices = {ItemSpaceItems.SLOWNESS,ItemSpaceItems.GUN};
                            itemChoice = itemChoices[random.Next(itemChoices.Length)];
                        } else if(enemyPoints > 50){ //Burn area (50 points)
                            ItemSpaceItems[] itemChoices = {ItemSpaceItems.BURN,ItemSpaceItems.SLOWNESS,ItemSpaceItems.GUN};
                            itemChoice = itemChoices[random.Next(itemChoices.Length)];
                        }
                        itemSpaceScript.InstantiateItem(itemChoice);
                        switch(itemChoice){
                            case ItemSpaceItems.GUN:
                                DecreaseEnemyPoints(20);
                                break;
                            case ItemSpaceItems.SLOWNESS:
                                DecreaseEnemyPoints(30);
                                break;
                            case ItemSpaceItems.BURN:
                                DecreaseEnemyPoints(50);
                                break;
                        }
                    }
                    break;
                }
            }
            canSelectItemSpace = false;
            StartCoroutine(WaitForItemSpaceSelection());
        }
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Public class methods

/// <summary>
/// Starts the procedures for a game over, depending on what caused it
/// </summary>
    public void GameOver(GameOverCause gameOverCause){
        isGameActive = false;
        StopAllCoroutines();
        gameOverMenu.SetActive(true);
        switch(gameOverCause){
            case GameOverCause.FINISHED:
                gameOverMenu.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "Files defended! Congratulations!";
                foreach(GameObject player in players){
                    if(!player.IsDestroyed()){
                        if(!player.GetComponent<PlayerController>().GetIsDead()){
                            player.GetComponent<PlayerController>().GameOverProcedure();
                        }
                    }
                }
                break;
            case GameOverCause.FILEDESTROYED:
                gameOverMenu.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "Files breached! The virus wins...";
                foreach(GameObject player in players){
                    if(!player.IsDestroyed()){
                        if(!player.GetComponent<PlayerController>().GetIsDead()){
                            player.GetComponent<PlayerController>().GameOverProcedure();
                        }
                    }
                }
                foreach(KeyValuePair<int,GameObject> enemy in activeEnemies){
                    GameObject enemyObject = enemy.Value;
                    if(!enemyObject.IsDestroyed()){
                        if(!enemyObject.GetComponent<EnemyController>().GetIsDead()){
                            enemyObject.GetComponent<EnemyController>().GameOverProcedure();
                        }
                    }
                }
                break;
            case GameOverCause.PLAYERSDEAD:
                gameOverMenu.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "Anti-virus down! The virus wins...";
                foreach(KeyValuePair<int,GameObject> enemy in activeEnemies){
                    GameObject enemyObject = enemy.Value;
                    if(!enemyObject.IsDestroyed()){
                        if(!enemyObject.GetComponent<EnemyController>().GetIsDead()){
                            enemyObject.GetComponent<EnemyController>().GameOverProcedure();
                        }
                    }
                }
                break;
        }
    }

    public void AddToActiveEnemies(int enemyID, GameObject enemy){
        activeEnemies.Add(enemyID,enemy);
    }

    public void RemoveFromActiveEnemies(int enemyID){
        activeEnemies.Remove(enemyID);
        if(activeEnemies.Count <= 0){
            //Miniwaves make up a larger wave, there are two for each wave  
            switch(miniWave){
                case 1:
                    //Sets the seconds left to 0 to make the next miniwave start now
                    secondsLeft = 0;
                    break;
                case 2:
                    switch(wave){
                        case 3:
                            GameOver(GameOverCause.FINISHED);
                            break;
                        default:
                            wave++;
                            waveText.text = $"Wave: {wave}";
                            StartCoroutine(WaveLoop());
                            break;
                    }
                    break;
            }
        }
    }

    public void CheckIfAllPlayersAreDead(){
        int numOfDeadPlayers = 0;
        for(int i = 0; i < playersLen; i++){
            if(players[i].IsDestroyed() || players[i].GetComponent<PlayerController>().GetIsDead()){
                numOfDeadPlayers++;
            }
        }
        if(numOfDeadPlayers == playersLen){
            GameOver(GameOverCause.PLAYERSDEAD);
        }
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

    public void DecreaseEnemyPoints(int pointsToSubtract){
        enemyPoints -= pointsToSubtract;
        enemyPointsText.text = $"<color=purple>Enemy Points: {enemyPoints}</color>";
    }

    public void ResetGame(){
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StartGame(){
        isGameActive = true;
        startMenu.SetActive(false);
        StartCoroutine(WaveLoop());
    }

    public int GetEnemyID(){
        return enemyID;
    }

    public int GetPlayerPoints(){
        return playerPoints;
    }

    public bool GetIsGameActive(){
        return isGameActive;
    }

    public bool GetIsMenuActive(){
        return isMenuActive; 
    }

    public GameObject[] GetFilesToAttack(){
        return filesToAttack;
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Private class methods

    private void SpawnWave(){
        int numOfSpawns = 20;
        int rangeOfEnemies = enemiesLen;
        switch(wave){
            case 1:
                numOfSpawns = 20;
                //For the first wave, only melee and gun enemies are spawned
                rangeOfEnemies = 2;
                break;
            case 2:
                numOfSpawns = 40;
                rangeOfEnemies = enemiesLen;
                break;
            case 3:
                numOfSpawns = 60;
                rangeOfEnemies = enemiesLen;
                break;
        }
        int numOfHeavies = 0;
        int numOfSwords = 0;
        for(int i = 0; i < numOfSpawns; i++){
            GameObject selectedSpawnPoint = spawnPoints[i];
            GameObject selectedEnemy = enemies[random.Next(rangeOfEnemies)];
            if(selectedEnemy.transform.GetChild(0).CompareTag("Heavy")){
                //Limits the number of heavies and sword enemies, as too many can make the game chaotic
                if(numOfHeavies != 5){
                    numOfHeavies++;
                } else{
                    selectedEnemy = enemies[random.Next(2)];
                }
            } else if(selectedEnemy.transform.GetChild(0).CompareTag("Sword")){
                if(numOfSwords != 15){
                    numOfSwords++;
                } else{
                    selectedEnemy = enemies[random.Next(2)];
                }
            }
            //Lets the player know which spawn points are active
            selectedSpawnPoint.GetComponent<ParticleSystem>().Play();
            Instantiate(selectedEnemy,selectedSpawnPoint.transform.position,selectedEnemy.transform.rotation);
        }
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//IEnumerators

    IEnumerator WaitForItemSpaceSelection(){
        yield return new WaitForSeconds(10);
        canSelectItemSpace = true;
    }

    IEnumerator WaveLoop(){
        isBetweenWaves = true;
        miniWave = 1;
        secondsLeft = 15;
        countdownTimer.transform.parent.gameObject.SetActive(true);
        for(int i = 0; i<15; i++){
            countdownTimer.text = $"Time Left: {secondsLeft}";
            secondsLeft--;
            yield return new WaitForSeconds(1);
        }
        SpawnWave();
        isBetweenWaves = false;
        secondsLeft = 80;
        for(int i = 0; i<80; i++){
            countdownTimer.text = $"Time Left: {secondsLeft}";
            secondsLeft--;
            yield return new WaitForSeconds(1);
            //This check is here as secondsLeft can be set to 0 by other functions
            if(secondsLeft <= 0){
                countdownTimer.transform.parent.gameObject.SetActive(false);
                break;
            }
        }
        miniWave++;
        SpawnWave();
    }


}
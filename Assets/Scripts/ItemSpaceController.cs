using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;

public class ItemSpaceController : MonoBehaviour
{
    // public enum AttackEffects{SLOWNESS,BURN};
    // public enum SpaceOwner{PLAYER,ENEMY};
    // private bool isSpaceActive;
    private bool isClicked;
    private GameObject playerOrEnemy;
    private GameManager gameManager;
    private GameManager.ItemSpaceItems activeItem;
    private GameManager.ItemSpaceOwner spaceOwner;
    private Material originalMaterial;
    private MeshRenderer renderer;
    [SerializeField] private GameObject menuOptions;
    [SerializeField] private GameObject attackEffectArea;
    [SerializeField] private GameObject[] players;
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private Material neonGreen;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        renderer = GetComponent<MeshRenderer>();
        originalMaterial = renderer.material;
        print(originalMaterial);
        spaceOwner = GameManager.ItemSpaceOwner.NONE;
        activeItem = GameManager.ItemSpaceItems.NONE;
    }

    // Update is called once per frame
    void Update()
    {
        if (spaceOwner == GameManager.ItemSpaceOwner.NONE && 
            Input.GetMouseButtonDown(1) && 
            !gameManager.GetIsMenuActive() && 
            HasPlayerClickedOnSpace()){
            isClicked = true;
            spaceOwner = GameManager.ItemSpaceOwner.PLAYER;
            gameManager.SetIsMenuActive(true);
            menuOptions.SetActive(true);
        }
    }

    private bool HasPlayerClickedOnSpace(){
        Ray ray;
        RaycastHit[] hits;
        ray =  Camera.main.ScreenPointToRay(Input.mousePosition);
        //Raycast all as it needs to go through the player attack areas
        hits = Physics.RaycastAll(ray:ray,maxDistance:100);
        foreach (RaycastHit hit in hits){
            if(hit.transform.gameObject == gameObject){
                return true;
            }
        }
        return false;
    }

    public GameManager.ItemSpaceOwner GetItemSpaceOwner(){
        return spaceOwner;
    }

    public void SetItemSpaceOwner(GameManager.ItemSpaceOwner spaceOwner){
        this.spaceOwner = spaceOwner;
        if (this.spaceOwner == GameManager.ItemSpaceOwner.ENEMY){
            renderer.material = neonGreen;  
        }
    }

    public GameManager.ItemSpaceItems GetActiveItem(){
        return activeItem;
    }

    public void HandleMenuInput(int menuChoice){
        //"isClicked" is needed here as all cubes have this function triggered whenever one of them is clicked
        if (isClicked){
            switch(menuChoice){
                case 0: //Burn Area, costs 50 points
                    if (gameManager.GetPlayerPoints() - 50 < 0){
                        //Display the "Not enough points!" message
                        menuOptions.transform.GetChild(5).gameObject.SetActive(true);
                        StartCoroutine(WaitForNoPointsNotification());
                    } else{
                        gameManager.DecreasePlayerPoints(50);
                        InstantiateItem(GameManager.ItemSpaceItems.BURN);
                        isClicked = false;
                        menuOptions.SetActive(false);
                        gameManager.SetIsMenuActive(false);
                        menuOptions.transform.GetChild(5).gameObject.SetActive(false);
                    }
                    break;
                case 1: //Slowness Area, 30 points
                    if (gameManager.GetPlayerPoints() - 30 < 0){
                        //Display the "Not enough points!" message
                        menuOptions.transform.GetChild(5).gameObject.SetActive(true);
                        StartCoroutine(WaitForNoPointsNotification());
                    } else{
                        gameManager.DecreasePlayerPoints(30);
                        InstantiateItem(GameManager.ItemSpaceItems.SLOWNESS);
                        isClicked = false;
                        menuOptions.SetActive(false);
                        gameManager.SetIsMenuActive(false);
                        menuOptions.transform.GetChild(5).gameObject.SetActive(false);
                    }
                    break;
                case 2: //Player Gun, 20 points
                    if (gameManager.GetPlayerPoints() - 20 < 0){
                            menuOptions.transform.GetChild(5).gameObject.SetActive(true);
                            StartCoroutine(WaitForNoPointsNotification());
                    } else{
                        gameManager.DecreasePlayerPoints(20);
                        InstantiateItem(GameManager.ItemSpaceItems.GUN);
                        isClicked = false;
                        menuOptions.SetActive(false);
                        gameManager.SetIsMenuActive(false);
                        menuOptions.transform.GetChild(5).gameObject.SetActive(false);
                    }
                    break;
                case 3: //Cancel
                    isClicked = false;
                    spaceOwner = GameManager.ItemSpaceOwner.NONE; 
                    menuOptions.SetActive(false);
                    gameManager.SetIsMenuActive(false);
                    menuOptions.transform.GetChild(5).gameObject.SetActive(false);
                    break;
            }
        }
    }

    public void InstantiateItem(GameManager.ItemSpaceItems item){
        activeItem = item;
        //By this point, the enemy would have already set their spaceowner type
        switch(item){
            case GameManager.ItemSpaceItems.SLOWNESS or GameManager.ItemSpaceItems.BURN:
                Instantiate(attackEffectArea,new Vector3(transform.position.x,transform.position.y + 1,transform.position.z),attackEffectArea.transform.rotation,parent:transform);
                StartCoroutine(WaitForItemDestruction());
                break;
            case GameManager.ItemSpaceItems.GUN:
                switch(spaceOwner){
                    case GameManager.ItemSpaceOwner.PLAYER:
                        playerOrEnemy = Instantiate(players[1],new Vector3(transform.position.x,transform.position.y + 1,transform.position.z),players[1].transform.rotation);
                        playerOrEnemy.GetComponent<PlayerController>().SetAsItemSquarePlayer();
                        StartCoroutine(WaitForItemDestruction()); 
                        break;
                    case GameManager.ItemSpaceOwner.ENEMY:
                        playerOrEnemy = Instantiate(enemies[1],new Vector3(transform.position.x,transform.position.y + 1,transform.position.z),enemies[1].transform.rotation); 
                        playerOrEnemy.GetComponent<EnemyController>().SetAsItemSquareEnemy();
                        StartCoroutine(WaitForItemDestruction());
                        break;
                }
                break;
        }
    }

    IEnumerator WaitForNoPointsNotification(){
        yield return new WaitForSeconds(2);
        menuOptions.transform.GetChild(5).gameObject.SetActive(false);
    }

    IEnumerator WaitForItemDestruction(){
        yield return new WaitForSeconds(20);
        switch(activeItem){
            case GameManager.ItemSpaceItems.BURN or GameManager.ItemSpaceItems.SLOWNESS:
                GameObject childObject = transform.GetChild(0).gameObject;
                childObject.GetComponent<AttackEffectAreaController>().DestroyProcedure();
                Destroy(childObject);
                break;
            case GameManager.ItemSpaceItems.GUN:
                if (!playerOrEnemy.IsDestroyed()){
                    switch(spaceOwner){
                        case GameManager.ItemSpaceOwner.PLAYER:
                            playerOrEnemy.GetComponent<PlayerController>().DeathProcedure();
                            break;
                        case GameManager.ItemSpaceOwner.ENEMY:
                            playerOrEnemy.GetComponent<EnemyController>().DeathProcedure();
                            break;
                    }
                }
                break;
        }
        renderer.material = originalMaterial;
        spaceOwner = GameManager.ItemSpaceOwner.NONE;
        activeItem = GameManager.ItemSpaceItems.NONE;
    }
}

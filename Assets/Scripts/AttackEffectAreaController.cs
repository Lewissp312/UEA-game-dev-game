using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls the behaviour for the areas spawned by item squares, such as slowness and burn areas.
/// </summary>

public class AttackEffectAreaController : MonoBehaviour
{
    private bool canInflictBurnDamage;
    private GameManager gameManager;
    private GameManager.ItemSpaceItems attackEffect;
    private ItemSpaceController itemSpaceScript;
    private BoxCollider boxCollider;
    private LayerMask colliderMask;
    private Vector3 worldCenter;
    private Vector3 worldHalfExtents;
    [SerializeField] private Material transparentGreen;
    [SerializeField] private ParticleSystem burnEffect;

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Unity methods
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        itemSpaceScript = transform.parent.gameObject.GetComponent<ItemSpaceController>();
        attackEffect = itemSpaceScript.GetActiveItem();
        switch(itemSpaceScript.GetItemSpaceOwner()){
            case GameManager.ItemSpaceOwner.PLAYER:
                colliderMask = LayerMask.GetMask("Enemy");
                break;
            case GameManager.ItemSpaceOwner.ENEMY:
                colliderMask = LayerMask.GetMask("Player");
                GetComponent<MeshRenderer>().material = transparentGreen;
                break;
        }
        //Collider code adapted from https://discussions.unity.com/t/how-can-i-get-an-overlapbox-with-the-exact-same-size-and-position-as-a-boxcollider/235044/2
        boxCollider = GetComponent<BoxCollider>();
        worldCenter = boxCollider.transform.TransformPoint(boxCollider.center);
        worldHalfExtents = Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale) * 0.5f;
        if (attackEffect == GameManager.ItemSpaceItems.BURN){
            canInflictBurnDamage = true;
            StartCoroutine(WaitForBurnDamage());
        }
    }

    void Update()
    {
        if (canInflictBurnDamage && gameManager.GetIsGameActive()){
            Collider[] overlaps = Physics.OverlapBox(worldCenter, worldHalfExtents, boxCollider.transform.rotation,layerMask:colliderMask);
            switch(itemSpaceScript.GetItemSpaceOwner()){
                case GameManager.ItemSpaceOwner.PLAYER:
                    foreach(Collider collider in overlaps){
                        if (!collider.gameObject.IsDestroyed()){
                            EnemyController enemyScript = collider.gameObject.GetComponent<EnemyController>();
                            if (!enemyScript.GetIsDead()){
                                ParticleSystem damageEffectCopy = Instantiate(burnEffect,collider.gameObject.transform.position,transform.rotation);
                                damageEffectCopy.Play();
                                Destroy(damageEffectCopy.gameObject,damageEffectCopy.main.duration);
                                enemyScript.DecreaseHealth(8);
                            }
                        }
                    }
                    break;
                case GameManager.ItemSpaceOwner.ENEMY:
                    foreach(Collider collider in overlaps){
                        if (!collider.gameObject.IsDestroyed()){
                            PlayerController playerScript = collider.gameObject.GetComponent<PlayerController>();
                            if (playerScript.GetIsDead()){
                                ParticleSystem damageEffectCopy = Instantiate(burnEffect,collider.gameObject.transform.position,transform.rotation);
                                damageEffectCopy.Play();
                                Destroy(damageEffectCopy.gameObject,damageEffectCopy.main.duration);
                                playerScript.DecreaseHealth(8);
                            }
                        }
                    }
                    break;
                
            }
            StartCoroutine(WaitForBurnDamage());
        }

    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Public class methods


/// <summary>
/// Method that is to be executed whenever an attack effect area is destroyed.
/// OnDestroy() is not used here as it would not execute in time
/// </summary>
    public void DestroyProcedure(){
        if (itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
            Collider[] overlaps = Physics.OverlapBox(worldCenter, worldHalfExtents, boxCollider.transform.rotation,layerMask:colliderMask);
            foreach(Collider collider in overlaps){
                collider.gameObject.GetComponent<NavMeshAgent>().speed += 3;
            }
        }
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//IEnumerators

    IEnumerator WaitForBurnDamage(){
        canInflictBurnDamage = false;
        yield return new WaitForSeconds(1);
        canInflictBurnDamage = true;
    }
}

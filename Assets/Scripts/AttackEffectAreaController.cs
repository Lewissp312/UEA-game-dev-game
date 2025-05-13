using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class AttackEffectAreaController : MonoBehaviour
{
    private bool canInflictBurnDamage;
    private GameManager.ItemSpaceItems attackEffect;
    private ItemSpaceController itemSpaceScript;
    private BoxCollider boxCollider;
    private LayerMask colliderMask;
    private Vector3 worldCenter;
    private Vector3 worldHalfExtents;
    [SerializeField] private Material transparentGreen;
    // Start is called before the first frame update
    void Start()
    {
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
        switch(attackEffect){
            case GameManager.ItemSpaceItems.SLOWNESS:
                // Collider[] overlaps = Physics.OverlapBox(worldCenter, worldHalfExtents, boxCollider.transform.rotation,layerMask:colliderMask);
                // foreach(Collider collider in overlaps){
                //     collider.gameObject.GetComponent<NavMeshAgent>().speed -= 3;
                // }
                break;
            case GameManager.ItemSpaceItems.BURN:
                canInflictBurnDamage = true;
                StartCoroutine(WaitForBurnDamage());
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (canInflictBurnDamage){
            Collider[] overlaps = Physics.OverlapBox(worldCenter, worldHalfExtents, boxCollider.transform.rotation,layerMask:colliderMask);
            switch(itemSpaceScript.GetItemSpaceOwner()){
                case GameManager.ItemSpaceOwner.PLAYER:
                    foreach(Collider collider in overlaps){
                        EnemyController enemyScript = collider.gameObject.GetComponent<EnemyController>();
                        if (!enemyScript.IsDestroyed() && !enemyScript.GetIsDead()){
                            enemyScript.DecreaseHealth(5);
                        }
                    }
                    break;
                case GameManager.ItemSpaceOwner.ENEMY:
                    foreach(Collider collider in overlaps){
                        PlayerController playerScript = collider.gameObject.GetComponent<PlayerController>();
                        if (!collider.gameObject.IsDestroyed() && !playerScript.GetIsDead()){
                            playerScript.DecreaseHealth(5);
                        }
                    }
                    break;
                
            }
            StartCoroutine(WaitForBurnDamage());
        }

    }

    public void DestroyProcedure(){
        if (itemSpaceScript.GetActiveItem() == GameManager.ItemSpaceItems.SLOWNESS){
            Collider[] overlaps = Physics.OverlapBox(worldCenter, worldHalfExtents, boxCollider.transform.rotation,layerMask:colliderMask);
            foreach(Collider collider in overlaps){
                collider.gameObject.GetComponent<NavMeshAgent>().speed += 3;
            }
        }
    }

    IEnumerator WaitForBurnDamage(){
        canInflictBurnDamage = false;
        yield return new WaitForSeconds(2);
        canInflictBurnDamage = true;
    }
}

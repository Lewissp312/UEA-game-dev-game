// using System.Collections;
// using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;

public class PlayerLaser : MonoBehaviour
{
    private int speed;
    private Vector3 positionToAttack;
    private Vector3 fireDirection;
    private GameObject parentGameObject;
    [SerializeField] private Material green;
    // Start is called before the first frame update
    void Start()
    {
        speed = 40;
        fireDirection = (positionToAttack - transform.position).normalized * speed;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += fireDirection * Time.deltaTime;
    }

    public GameObject GetParentGameobject(){
        return parentGameObject;
    }

    public void SetPositionToAttack(Vector3 positionToAttack){
        this.positionToAttack = positionToAttack;
    }

    public void SetAsEnemyLaser(){
        tag = "EnemyLaser";
        GetComponent<MeshRenderer>().material = green;
    }

    public void SetParentGameObject(GameObject parentGameObject){
        this.parentGameObject = parentGameObject;
    }
}

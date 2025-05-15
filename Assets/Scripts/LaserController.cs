using UnityEngine;

/// <summary>
/// Controls behaviour for lasers
/// </summary>
public class LaserController : MonoBehaviour
{
    private int speed;
    private Vector3 positionToAttack;
    private Vector3 fireDirection;
    private GameObject shooterGameObject;
    [SerializeField] private Material green;

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Unity methods
    void Start()
    {
        speed = 40;
        fireDirection = (positionToAttack - transform.position).normalized * speed;
    }

    void Update()
    {
        transform.position += fireDirection * Time.deltaTime;
    }

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Public class methods

    public void SetPositionToAttack(Vector3 positionToAttack){
        this.positionToAttack = positionToAttack;
    }

    public void SetAsEnemyLaser(){
        tag = "EnemyLaser";
        GetComponent<MeshRenderer>().material = green;
    }

    public void SetShooterGameObject(GameObject shooterGameObject){
        this.shooterGameObject = shooterGameObject;
    }

    public GameObject GetShooterGameObject(){
        return shooterGameObject;
    }
}

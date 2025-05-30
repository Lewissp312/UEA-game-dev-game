using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the right click scrolling camera
/// </summary>
public class CameraController : MonoBehaviour
{
    private Vector3 lastDragPosition;
    private GameManager gameManager;

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Unity methods

    void Start(){
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }
    void LateUpdate()
    {
        if (gameManager.GetIsGameActive()){
            //Code adapted from https://stackoverflow.com/questions/45921780/adding-camera-drag-in-unity-3d
            //Right click
            if (Input.GetMouseButtonDown(1))
                lastDragPosition = Input.mousePosition;
            if (Input.GetMouseButton(1))
            {
                Vector3 originalPosition = transform.position;
                Vector3 delta = lastDragPosition - Input.mousePosition;
                transform.Translate(6f * Time.deltaTime * delta);
                lastDragPosition = Input.mousePosition;
                if (transform.position.x < 44 || transform.position.x > 185 || transform.position.z < 15 || transform.position.z > 86){
                    transform.position = originalPosition;
                }
            }
        }
    }
}

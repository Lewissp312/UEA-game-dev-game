using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float dragSpeed = 1f;
    Vector3 lastDragPosition;
    void LateUpdate()
    {
        //Code adapted from https://stackoverflow.com/questions/45921780/adding-camera-drag-in-unity-3d
        if (Input.GetMouseButtonDown(1))
            lastDragPosition = Input.mousePosition;
        if (Input.GetMouseButton(1))
        {
            Vector3 delta = lastDragPosition - Input.mousePosition;
            transform.Translate(6f * Time.deltaTime * delta);
            lastDragPosition = Input.mousePosition;
        }
    }
}

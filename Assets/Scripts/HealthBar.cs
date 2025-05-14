using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private Camera camera;
    private Transform target;
    private Vector3 offset;
    [SerializeField] private Slider slider;
    // Start is called before the first frame update
    void Start()
    {
        target = transform.parent.parent;
        camera = Camera.main;
        if (target.CompareTag("File")){
            offset = new Vector3(0,10,0);
        } else if(target.GetChild(0).CompareTag("HeavyHealth")){
            offset = new Vector3(0,9,0);
        } else{
            offset = new Vector3(0,4,0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = camera.transform.rotation; 
        transform.position = target.position + offset;
        
    }

    public void UpdateHealth(float currentHealth, float maximumHealth){
        if (currentHealth <= 0){
            slider.value = 0;
        } else{
            float newValue = currentHealth / maximumHealth;
            slider.value = newValue;
        }
    }
}

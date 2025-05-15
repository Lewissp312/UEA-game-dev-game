using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls all health bars
/// </summary>
public class HealthBar : MonoBehaviour
{
    private Camera camera;
    private Transform target;
    private Vector3 offset;
    [SerializeField] private Slider slider;

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Unity methods
    void Start()
    {
        target = transform.parent.parent;
        camera = Camera.main;
        //The offset is different for files and heavies.
        //This is because the bars need to be higher up for them, given their size difference
        if (target.CompareTag("File")){
            offset = new Vector3(0,10,0);
        } else if(target.GetChild(0).CompareTag("Heavy")){
            offset = new Vector3(0,9,0);
        } else{
            offset = new Vector3(0,4,0);
        }
    }
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

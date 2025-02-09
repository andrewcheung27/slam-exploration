using UnityEngine;

public class SensorController : MonoBehaviour
{
    RaycastHit hit;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void Activate()
    {
        float maxDistance = 100f;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, maxDistance)) {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red);
            Debug.Log("HIT");
        }
    }
}

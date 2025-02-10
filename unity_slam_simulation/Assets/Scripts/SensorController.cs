using UnityEngine;

public class SensorController : MonoBehaviour
{
    private RaycastHit hit;

    public bool debug = false;
    public GameObject pointPrefab;
    public float sensorRange = 100f;

    public void Activate()
    {
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, sensorRange)) {
            if (!hit.collider.gameObject.CompareTag("Point")) {  // don't observe previous observations
                GameObject newPoint = Instantiate(pointPrefab, hit.point, Quaternion.identity);
                newPoint.GetComponent<MeshRenderer>().material = hit.collider.gameObject.GetComponent<MeshRenderer>().material;
                if (debug) Debug.Log("HIT: " + hit.collider.gameObject.name);
            }
        }
    }
}

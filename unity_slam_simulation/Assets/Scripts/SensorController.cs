using System.Collections.Generic;
using UnityEngine;

public class SensorController : MonoBehaviour
{
    public bool debug = false;
    public float sensorRange = 100f;
    public float maxSensorAngle = 60f;  // max angle of cone
    public int numSensorRays = 1000;  // number of raycasts to do when sensor is activated
    public GameObject sensorLineRendererPrefab;  // empty GameObject with a LineRenderer component
    public float debugSensorRayDuration = 1f;  // how long the debug rays are shown for

    Vector3 SampleConeDirection(Vector3 direction, float angle)
    // ChatGPT function to get a random direction on the base of a cone
    {
        float theta = Random.Range(0f, Mathf.Deg2Rad * angle); // Random cone angle
        float phi = Random.Range(0f, 2f * Mathf.PI); // Random azimuth

        // Convert spherical to Cartesian
        float x = Mathf.Sin(theta) * Mathf.Cos(phi);
        float y = Mathf.Sin(theta) * Mathf.Sin(phi);
        float z = Mathf.Cos(theta);

        Vector3 localDirection = new Vector3(x, y, z);
        return Quaternion.FromToRotation(Vector3.forward, direction) * localDirection;
    }

    public List<Point> Activate()
    {
        MeshRenderer meshRenderer;
        List<Point> pointCloud = new List<Point>();

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        for (int i = 0; i < numSensorRays; i++) {
            Vector3 randomDir = SampleConeDirection(forward, maxSensorAngle);
            if (Physics.Raycast(origin, randomDir, out RaycastHit hit, sensorRange)) {
                // get mesh renderer to get the color of the object hit
                if (hit.collider.gameObject.TryGetComponent<MeshRenderer>(out meshRenderer)) {
                    // create Point object
                    Point p = new Point(hit.point, meshRenderer.material.color);
                    pointCloud.Add(p);
                }
            }
        }

        // visualize raycasts with Debug.DrawRay()
        // if (debug) {
        //     foreach (Point point in pointCloud) {
        //         Debug.DrawRay(origin, point.position - origin, Color.red, debugSensorRayDuration);
        //     }
        // }

        // visualize raycasts with LineRenderer
        if (debug) {
            StartCoroutine(DrawLines(pointCloud));
        }

        return pointCloud;
    }

    IEnumerator<WaitForSeconds> DrawLines(List<Point> pointCloud)
    {
        List<GameObject> lines = new List<GameObject>();

        foreach(Point point in pointCloud) {
            GameObject child = Instantiate(sensorLineRendererPrefab, transform);
            lines.Add(child);
            if (child.TryGetComponent(out LineRenderer renderer)) {
                renderer.SetPosition(0, transform.position);
                renderer.SetPosition(1, point.position);
                renderer.startColor = Color.red;
                renderer.endColor = Color.red;
            }
        }

        yield return new WaitForSeconds(debugSensorRayDuration);

        foreach(GameObject line in lines) {
            if (line != null) {
                Destroy(line);
            }
        }
    }
}

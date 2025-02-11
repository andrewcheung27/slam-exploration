using System.Collections.Generic;
using UnityEngine;

public class SensorController : MonoBehaviour
{
    private List<Vector3> pointsOnConeFace;
    private List<Point> pointCloud;
    private RaycastHit hit;

    public bool debug = false;
    public GameObject pointPrefab;
    public float sensorRange = 100f;

    void DrawCircle()
    {
        // TODO
        // https://www.youtube.com/watch?v=DdAfwHYNFOE
    }

    void GetPointsOnConeFace()
    {
        // TODO: call DrawCircle() multiple times with decreasing radius, make sure to store points in pointsOnConeFace
    }

    public void Activate()
    {
        MeshRenderer meshRenderer;

        pointCloud.Clear();  // clear point cloud from previous node

        GetPointsOnConeFace();  // xyz coordinates as a Vector3

        // TODO: point cloud map of points in a big cone
        foreach (Vector3 position in pointsOnConeFace) {
            float distance = 69;  // TODO: calculate distance between sensor and point

            // raycast to the point on the cone face
            if (Physics.Raycast(transform.position, position, out hit, Mathf.Min(distance, sensorRange))) {
                // get mesh renderer to get the color of the object hit
                if (hit.collider.gameObject.TryGetComponent<MeshRenderer>(out meshRenderer)) {
                    // create Point object
                    Point p = new Point(position, meshRenderer.material.color);
                    // TODO: add point to point cloud
                    if (debug) Debug.Log("HIT: " + hit.collider.gameObject.name);
                }
            }
        }

        // TODO: return point cloud

        // if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, sensorRange)) {
        //     if (!hit.collider.gameObject.CompareTag("Point")) {  // don't observe previous observations
        //         GameObject newPoint = Instantiate(pointPrefab, hit.point, Quaternion.identity);
        //         newPoint.GetComponent<MeshRenderer>().material = hit.collider.gameObject.GetComponent<MeshRenderer>().material;
        //         if (debug) Debug.Log("HIT: " + hit.collider.gameObject.name);
        //     }
        // }
    }
}

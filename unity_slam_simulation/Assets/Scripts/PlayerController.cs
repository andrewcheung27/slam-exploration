using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    RaycastHit hit;
    Rigidbody rb;
    LineRenderer lineRenderer;
    InputAction moveAction;
    InputAction interactAction;
    SensorController sensorController;
    GameObject voxelRenderer;
    float timeSinceSensorActivated = 0f;
    public ParticleSystem system;
    public GameObject sensor;
    public GameObject pointPrefab;
    public float moveSpeed = 10f;
    public float sensorCooldown = 1.0f;  // in seconds

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // voxelRenderer = system.GetComponent<VoxelRenderer>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startColor = Color.blue;
        lineRenderer.endColor = Color.blue;
        sensorController = sensor.GetComponent<SensorController>();

        moveAction = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");
    }

    void Start()
    {
        
    }

    void Update()
    {
        timeSinceSensorActivated += Time.deltaTime;
        if (timeSinceSensorActivated > sensorCooldown) {
            ActivateSensor();
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        rb.linearVelocity = new Vector3(
            moveInput.x * moveSpeed * Time.fixedDeltaTime,  // x axis
            rb.linearVelocity.y,  // keep velocity on y axis
            moveInput.y * moveSpeed * Time.fixedDeltaTime  // moveInput.y corresponds to z axis in 3D
        );
    }

    // void ActivateSensor(InputAction.CallbackContext ctx)
    // {
    //     if (timeSinceSensorActivated > sensorCooldown) {
    //         Debug.Log("INTERACT ACTION TRIGGERED");
    //         sensorController.Activate();
    //         timeSinceSensorActivated = 0f;
    //     }
    // }

    // void ActivateSensor()
    // {
    //     if (interactAction.WasPressedThisFrame()) {
    //         Debug.Log("INTERACT ACTION TRIGGERED");


    //         float maxDistance = 100f;
    //         if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, maxDistance)) {
    //             // Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red);
    //             lineRenderer.SetPosition(0, transform.position);
    //             lineRenderer.SetPosition(1, hit.collider.gameObject.transform.position);
    //             Debug.Log("HIT");
    //         }

    //         // sensorController.Activate();
    //         timeSinceSensorActivated = 0f;
    //     }
    // }

    // void ActivateSensor()
    // {
    //     if (interactAction.WasPressedThisFrame()) {
    //         float maxDistance = 100f;
    //         if (Physics.Raycast(sensor.transform.position, transform.TransformDirection(Vector3.forward), out hit, maxDistance)) {
    //             if (!hit.collider.gameObject.CompareTag("Point")) {  // don't observe previous observations
    //                 GameObject newPoint = Instantiate(pointPrefab, hit.point, Quaternion.identity);
    //                 newPoint.GetComponent<MeshRenderer>().material = hit.collider.gameObject.GetComponent<MeshRenderer>().material;
    //                 Debug.Log("HIT: " + hit.collider.gameObject);
    //             }
    //         }
    //         timeSinceSensorActivated = 0f;
    //     }
    // }


    void ActivateSensor()
    {
        if (interactAction.WasPressedThisFrame()) {
            float maxDistance = 100f;
            if (Physics.Raycast(sensor.transform.position, transform.TransformDirection(Vector3.forward), out hit, maxDistance)) {
                if (!hit.collider.gameObject.CompareTag("Point")) {  // don't observe previous observations
                    GameObject newPoint = Instantiate(pointPrefab, hit.point, Quaternion.identity);
                    newPoint.GetComponent<MeshRenderer>().material = hit.collider.gameObject.GetComponent<MeshRenderer>().material;
                    Debug.Log("HIT: " + hit.collider.gameObject);
                }
            }
            timeSinceSensorActivated = 0f;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction interactAction;
    private SensorController sensorController;
    private float timeSinceSensorActivated = 0f;
    private int nodeIndex = 0;  // incrementing id for PoseNodes
    private PoseGraph poseGraph;
    private PoseGraph poseGraphGroundTruth;

    public GameObject sensor;
    public float moveSpeed = 10f;
    public float sensorCooldown = 1f;  // in seconds
    public float sensorError = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");

        sensorController = sensor.GetComponent<SensorController>();
    }

    void Start()
    {
        // this must be in Start() instead of Awake() to make sure GameManager.instance is initialized
        poseGraph = GameManager.instance.GetPoseGraph();
        // "pose graph" for ground truth, which has no edges and only stores ground truth nodes
        poseGraphGroundTruth = GameManager.instance.GetPoseGraphGroundTruth();
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

    // simulates error in pose estimation, which could come from sensor inaccuracy/drift or feature matching in Visual SLAM
    Vector3 GetRandomError()
    {
        return new Vector3(
            Random.Range(-1 * sensorError, sensorError), 
            Random.Range(0, sensorError),  // don't allow negative Y because ground level is at 0
            Random.Range(-1 * sensorError, sensorError));
    }

    PoseNode CreatePoseNode(List<Point> pointCloud, bool simulateError=true)
    {
        Vector3 position = transform.position;
        if (simulateError) position += GetRandomError();
        // TODO: simulate error for rotation?
        Vector3 rotation = transform.eulerAngles;

        Pose pose = new Pose(position, rotation);
        int timePlaceholder = 69;  // TODO: time might not be necessary for PoseNodes
        return new PoseNode(nodeIndex, pose, timePlaceholder, pointCloud);
    }

    void ActivateSensor()
    {
        if (interactAction.WasPressedThisFrame()) {
            sensorController.Activate();
            List<Point> pointCloud = null;  // TODO: get point cloud from sensor

            // add node to pose graph, with some error
            poseGraph.AddNode(CreatePoseNode(pointCloud, simulateError: true));

            // add node to ground truth, with no point cloud and no error
            poseGraphGroundTruth.AddNode(CreatePoseNode(null, simulateError: false));

            // bookkeeping
            nodeIndex++;
            timeSinceSensorActivated = 0f;
        }
    }
}

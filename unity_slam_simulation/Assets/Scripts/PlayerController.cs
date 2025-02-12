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

    void ActivateSensor()
    // in a real visual SLAM system, we would extract features from video frames and use their change between frames to estimate trajectory.
    // here, we just get a point cloud and add nodes to the pose graph with random error.
    {
        if (interactAction.WasPressedThisFrame()) {
            // activate sensor to get point cloud
            List<Point> pointCloud = sensorController.Activate();

            Vector3 position = transform.position;
            Vector3 positionWithError = position + GetRandomError();
            Vector3 rotation = transform.eulerAngles;
            Vector3 rotationWithError = rotation;  // TODO: simulate error for rotation?

            Pose pose = new Pose(positionWithError, rotationWithError);
            Pose poseGroundTruth = new Pose(position, rotation);

            // add node to pose graph
            PoseNode node = new PoseNode(nodeIndex, pose, poseGroundTruth, pointCloud);
            poseGraph.AddNode(node);

            // bookkeeping
            nodeIndex++;
            timeSinceSensorActivated = 0f;
        }
    }
}

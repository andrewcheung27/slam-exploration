using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction interactAction;
    private SensorController sensorController;
    private float timeSinceSensorActivated = 0f;
    private int nodeIndex = 0;  // incrementing id for PoseNodes
    private PoseGraph poseGraph;

    public GameObject sensor;
    public float moveSpeed = 10f;
    public float rotateSpeed = 10f;
    public float sensorCooldown = 1f;  // in seconds
    public float sensorError = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = InputSystem.actions.FindAction("Move");
        rotateAction = InputSystem.actions.FindAction("Rotate");
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
        RotatePlayer();

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
        Vector3 velocity = new Vector3(
            moveInput.x * moveSpeed * Time.fixedDeltaTime,  // x axis
            rb.linearVelocity.y,  // keep velocity on y axis
            moveInput.y * moveSpeed * Time.fixedDeltaTime  // moveInput.y corresponds to z axis in 3D
        );
        // set velocity with TransformDirection() to move relative to the direction player is facing
        rb.linearVelocity = transform.TransformDirection(velocity);
    }

    void RotatePlayer()
    {
        Vector2 rotateInput = rotateAction.ReadValue<Vector2>();
        transform.Rotate(new Vector3(
            0f, 
            rotateInput.x * rotateSpeed * Time.deltaTime,  // look left and right
            0f
        ));
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

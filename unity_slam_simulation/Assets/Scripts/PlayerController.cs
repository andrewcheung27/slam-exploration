using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;

    [Header ("Inputs")]
    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction interactAction;
    private InputAction riseAction;
    private InputAction straightenAction;
    private bool shouldStraighten = false;

    [Header ("Sensor")]
    private SensorController sensorController;
    private float timeSinceSensorActivated = 0f;
    public GameObject sensor;
    public float moveSpeed = 10f;
    public float rotateSpeed = 10f;
    public float sensorCooldown = 1f;  // how long before sensor can be activated again (in seconds)
    public float sensorError = 1f;
    public bool sensorEnabled = true;

    [Header ("Pose Graph")]
    private PoseGraph poseGraph;
    private int nodeIndex = 0;  // incrementing id for PoseNodes
    private Pose prevPoseEstimated;  // the previous pose estimated. used to simulate drift.
    private Pose prevPoseGT;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = InputSystem.actions.FindAction("Move");
        rotateAction = InputSystem.actions.FindAction("Rotate");
        interactAction = InputSystem.actions.FindAction("Interact");
        riseAction = InputSystem.actions.FindAction("Rise");
        straightenAction = InputSystem.actions.FindAction("Straighten");

        sensorController = sensor.GetComponent<SensorController>();

        prevPoseEstimated = new Pose(transform.position, transform.eulerAngles);
        prevPoseGT = new Pose(transform.position, transform.eulerAngles);
    }

    void Start()
    {
        // this must be in Start() instead of Awake() to make sure GameManager.instance is initialized
        poseGraph = GameManager.instance.GetPoseGraph();
    }

    void Update()
    {
        RotatePlayer();
        RisePlayer();
        if (straightenAction.WasPressedThisFrame())
        {
            shouldStraighten = !shouldStraighten; 
        }

        if (shouldStraighten)
        {
            StraightenPlayer();
        }

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
            rotateInput.y * rotateSpeed * Time.deltaTime, 
            rotateInput.x * rotateSpeed * Time.deltaTime,  // look left and right
            0f
        ));
    }

    void RisePlayer()
    {
        Vector2 riseInput = riseAction.ReadValue<Vector2>(); 
        Vector3 velocity = new Vector3(
                   rb.linearVelocity.x,  
                   riseInput.y * moveSpeed * Time.fixedDeltaTime,  
                   rb.linearVelocity.y 
               );
        // set velocity with TransformDirection() to move relative to the direction player is facing
        rb.linearVelocity = transform.TransformDirection(velocity);
    }

    void StraightenPlayer()
    {
        Quaternion targetRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f); // Adjust speed
    }

    // simulates error in pose estimation, which could come from sensor inaccuracy/drift or feature matching in Visual SLAM
    Vector3 GetRandomError()
    {
        return new Vector3(
            Random.Range(-1 * sensorError, sensorError), 
            Random.Range(-1 * sensorError, sensorError), 
            Random.Range(-1 * sensorError, sensorError)
        );
    }

    Vector3 GetPositionWithError(Vector3 currentPositionGT)
    {
        // get vector from previous GT position to current GT position
        Vector3 prevPosition = prevPoseGT.position;
        Vector3 v = currentPositionGT - prevPosition;
        // add random error
        v += GetRandomError();
        // add it to previous estimated position to get new estimated position, with accumulating error
        return prevPoseEstimated.position + v;
    }

    Vector3 GetRotationWithError(Vector3 currentRotationGT)
    {
        // TODO: simulate random error for rotation?
        return currentRotationGT;
    }

    void ActivateSensor()
    // in a real visual SLAM system, we would extract features from video frames and use their change between frames to estimate trajectory.
    // here, we just get a point cloud and add nodes to the pose graph with random error.
    {
        if (!sensorEnabled) {
            return;
        }

        if (interactAction.WasPressedThisFrame()) {
            // activate sensor to get point cloud
            List<Point> pointCloud = sensorController.Activate();

            // ground truth
            Vector3 position = transform.position;
            Vector3 rotation = transform.eulerAngles;
            Pose poseGroundTruth = new Pose(position, rotation);

            // position/rotation with error from the last node and additional simulated error
            Vector3 positionWithError = GetPositionWithError(position);
            Vector3 rotationWithError = rotation;

            // update the previous pose estimated
            prevPoseEstimated = new Pose(positionWithError, rotationWithError);
            // update the previous ground truth pose
            prevPoseGT = poseGroundTruth;

            // add node to pose graph
            PoseNode node = new PoseNode(nodeIndex, prevPoseEstimated, poseGroundTruth, pointCloud);
            poseGraph.AddNode(node);
            // Debug.Log("difference between estimate and GT positions: " + (prevPoseEstimated.position - poseGroundTruth.position).magnitude.ToString());

            // bookkeeping
            nodeIndex++;
            timeSinceSensorActivated = 0f;
        }
    }

    public void setSensorEnabled(bool b)
    {
        sensorEnabled = b;
    }
}

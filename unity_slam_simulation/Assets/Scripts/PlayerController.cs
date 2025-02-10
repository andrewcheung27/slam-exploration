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
    public float sensorCooldown = 1.0f;  // in seconds

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");

        sensorController = sensor.GetComponent<SensorController>();

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

    public PoseGraph GetPoseGraph()
    {
        return poseGraph;
    }

    void ActivateSensor()
    {
        if (interactAction.WasPressedThisFrame()) {
            sensorController.Activate();

            // add node to pose graph
            Pose pose = new Pose(transform.position, transform.eulerAngles);
            int timePlaceholder = 69;  // TODO: time might not be necessary for PoseNodes
            List<Point> pointCloud = null;  // TODO: get point cloud from sensor
            PoseNode poseNode = new PoseNode(nodeIndex, pose, timePlaceholder, pointCloud);
            poseGraph.AddNode(poseNode);

            // bookkeeping
            nodeIndex++;
            timeSinceSensorActivated = 0f;
        }
    }
}

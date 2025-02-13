using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EKFSLAM : MonoBehaviour
{
    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction interactAction;
    private InputAction riseAction;
    private InputAction straightenAction;
    private bool shouldStraighten = false;
    private SensorController sensorController;
    private float timeSinceSensorActivated = 0f;

    private List<RobotState> robotStates = new List<RobotState>();
    private Dictionary<int, List<GameObject>> objects = new Dictionary<int, List<GameObject>>();

    private List<Vector3> landmarks; // Landmark positions
    private Matrix4x4 covariance; // Covariance matrix (4x4 for 3D pose)

    private RobotState previousRobotState;
    private Matrix4x4 previousCovariance; // Store the previous covariance

    public GameObject sensor;
    public float moveSpeed = 500f;
    public float rotateSpeed = 200f;
    public float sensorCooldown = 1f; // in seconds
    public float sensorError = 1f;
    public bool sensorEnabled = true;
    public float sensorRange = 100f;

    // Define RobotState structure (now includes 3D orientation)
    private struct RobotState
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private RobotState robotState;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = InputSystem.actions.FindAction("Move");
        rotateAction = InputSystem.actions.FindAction("Rotate");
        interactAction = InputSystem.actions.FindAction("Interact");
        riseAction = InputSystem.actions.FindAction("Rise");
        straightenAction = InputSystem.actions.FindAction("Straighten");

        sensorController = sensor.GetComponent<SensorController>();
    }

    void Start()
    {
        robotState = new RobotState { position = Vector3.zero, rotation = Quaternion.identity };
        landmarks = new List<Vector3>();
        covariance = Matrix4x4.identity;
        covariance[0, 0] = 0.1f;
        covariance[1, 1] = 0.1f;
        covariance[2, 2] = 0.1f;
        covariance[3, 3] = 0.1f;

        previousRobotState = robotState;
        previousCovariance = covariance; 

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
        if (timeSinceSensorActivated > sensorCooldown)
        {
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
            moveInput.x * moveSpeed * Time.fixedDeltaTime,
            rb.linearVelocity.y,
            moveInput.y * moveSpeed * Time.fixedDeltaTime
        );
        rb.linearVelocity = transform.TransformDirection(velocity);

        // Update robot state position based on movement
        //robotState.position += transform.TransformDirection(velocity * Time.fixedDeltaTime);
    }

    void RotatePlayer()
    {
        Vector2 rotateInput = rotateAction.ReadValue<Vector2>();
        transform.Rotate(new Vector3(
            rotateInput.y * rotateSpeed * Time.deltaTime,
            rotateInput.x * rotateSpeed * Time.deltaTime,
            0f
        ));

        // Update robot state rotation
        //robotState.rotation = transform.rotation;
    }

    void RisePlayer()
    {
        Vector2 riseInput = riseAction.ReadValue<Vector2>();
        Vector3 velocity = new Vector3(
            rb.linearVelocity.x,
            riseInput.y * moveSpeed * Time.fixedDeltaTime,
            rb.linearVelocity.z
        );
        rb.linearVelocity = transform.TransformDirection(velocity);

        // Update robot state position for vertical movement
        //robotState.position += transform.TransformDirection(velocity * Time.fixedDeltaTime);
    }

    void StraightenPlayer()
    {
        Quaternion targetRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        //robotState.rotation = transform.rotation; // Update rotation state
    }

    void ActivateSensor()
    {
        if (!sensorEnabled)
        {
            return;
        }

        if (interactAction.WasPressedThisFrame())
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, sensorRange))
            {
                Vector3 landmarkPosition = hit.point;
                PredictRobotState();
                ProcessMeasurement(landmarkPosition);
                previousRobotState = robotState;
                previousCovariance = covariance;
            }
        }
    }

    private void ProcessMeasurement(Vector3 landmarkPosition)
    {
        Vector3 measurement = transform.InverseTransformPoint(landmarkPosition);

        int existingLandmarkIndex = -1;
        for (int i = 0; i < landmarks.Count; i++)
        {
            if (Vector3.Distance(landmarks[i], landmarkPosition) < 2f)
            {
                existingLandmarkIndex = i;
                break;
            }
        }

        if (existingLandmarkIndex == -1)
        {
            AddNewLandmark(landmarkPosition);
            existingLandmarkIndex = landmarks.Count-1;

        }
        
        // Always do a correct step
        Correct(measurement, existingLandmarkIndex);
        ManageObjects(existingLandmarkIndex);

        

    }


    private void AddNewLandmark(Vector3 landmarkPosition)
    {
        landmarks.Add(landmarkPosition);
        robotStates.Add(robotState);
        
    }


    void PredictRobotState()
    {
        // 1. Get odometry (movement) information:
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector2 rotateInput = rotateAction.ReadValue<Vector2>();
        Vector2 riseInput = riseAction.ReadValue<Vector2>();

        // 2. Calculate the change in pose (delta_x, delta_y, delta_z, delta_theta):
        Vector3 velocity = new Vector3(
            moveInput.x * moveSpeed * Time.fixedDeltaTime,
            riseInput.y * moveSpeed * Time.fixedDeltaTime,
            moveInput.y * moveSpeed * Time.fixedDeltaTime
        );
        Vector3 delta_position = transform.TransformDirection(velocity); // In world coordinates
        Quaternion delta_rotation = Quaternion.Euler(0, rotateInput.x * rotateSpeed * Time.fixedDeltaTime, 0);

        // 3. Update the robot state (predict the next pose):
        robotState.position += delta_position;
        robotState.rotation *= delta_rotation;

        // 4. Update the covariance matrix (predict uncertainty):
        // This is a simplified example.  A real EKF would use a more complex
        // motion model Jacobian (G) and process noise covariance (Q).
        // For simplicity, we'll just add a small amount of uncertainty:
        Matrix4x4 G = Matrix4x4.identity; // Motion model Jacobian (simplified)
        Matrix4x4 Q = Matrix4x4.identity; // Measurement noise
        Q[0, 0] = 0.1f;
        Q[1, 1] = 0.1f;
        Q[2, 2] = 0.1f;
        Q[3, 3] = 0.1f;
        covariance = MatrixAdd(MatrixMultiply(MatrixMultiply(G, previousCovariance), G.transpose), Q);
    }
        


    private void Correct(Vector3 measurement, int landmarkIndex)
    {
        Vector3 landmark = landmarks[landmarkIndex];
        Vector3 predictedMeasurement = transform.InverseTransformPoint(landmark);

        Matrix4x4 H = new Matrix4x4();
        H.SetColumn(0, -transform.InverseTransformDirection(landmark - robotState.position));
        H.SetColumn(1, -transform.InverseTransformDirection(landmark - robotState.position));
        H.SetColumn(2, -transform.InverseTransformDirection(landmark - robotState.position));
        H.SetColumn(3, Vector4.zero); // Simplified rotation Jacobian

        Matrix4x4 R = Matrix4x4.identity; // Measurement noise
        R[0, 0] = 0.1f;
        R[1, 1] = 0.1f;
        R[2, 2] = 0.1f;
        R[3, 3] = 0.1f;

        // Kalman Gain (using helper functions for matrix operations)
        Matrix4x4 S = MatrixAdd(MatrixMultiply(H, MatrixMultiply(covariance, H.transpose)), R);
        Matrix4x4 K = MatrixMultiply(covariance, MatrixMultiply(H.transpose, S.inverse));

        // Update state (using Vector4 for calculations)
        Vector4 z = new Vector4(measurement.x, measurement.y, measurement.z, 0);
        Vector4 z_pred = new Vector4(predictedMeasurement.x, predictedMeasurement.y, predictedMeasurement.z, 0);
        Vector4 stateUpdate = MatrixMultiply(K, Vector4Subtract(z, z_pred));

        robotState.position += new Vector3(stateUpdate.x, stateUpdate.y, stateUpdate.z);
        robotState.rotation *= Quaternion.Euler(0, 0, stateUpdate.w); // Simplified rotation update
        //if (landmarkIndex >= robotStates.Count)
        //{
        //    for (int i = robotStates.Count; i <= landmarkIndex; i++)
        //    {
        //        robotStates.Add(new RobotState()); // Add default RobotStates until the index is valid.
        //    }
        //}
        robotStates[landmarkIndex] = robotState;

        // Update covariance (using helper functions)
        Matrix4x4 I = Matrix4x4.identity;
        covariance = MatrixMultiply(MatrixSubtract(I, MatrixMultiply(K, H)), covariance);

    }


    // Helper functions for Matrix4x4 operations (since + and - are not directly supported)

    private Matrix4x4 MatrixAdd(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = a[i, j] + b[i, j];
            }
        }
        return result;
    }

    private Matrix4x4 MatrixSubtract(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = a[i, j] - b[i, j];
            }
        }
        return result;
    }

    private Matrix4x4 MatrixMultiply(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = 0;
                for (int k = 0; k < 4; k++)
                {
                    result[i, j] += a[i, k] * b[k, j];
                }
            }
        }
        return result;
    }

    private Vector4 MatrixMultiply(Matrix4x4 a, Vector4 b)
    {
        Vector4 result = new Vector4();
        for (int i = 0; i < 4; i++)
        {
            result[i] = 0;
            for (int j = 0; j < 4; j++)
            {
                result[i] += a[i, j] * b[j];
            }
        }
        return result;
    }

    private Vector4 Vector4Subtract(Vector4 a, Vector4 b)
    {
        return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    }



    private void ManageObjects(int landMarkIndex)
    {
        if (landMarkIndex == -1)
        {
            return;
        }


        if (objects.ContainsKey(landMarkIndex)) 
        {
            Debug.Log("Destroy");
            foreach (var obj in objects[landMarkIndex])
            {
                Destroy(obj);
            }
            objects.Remove(landMarkIndex);

        }
        objects.Add(landMarkIndex, new List<GameObject>());
        objects[landMarkIndex].Add(DrawCube(Color.blue, landmarks[landMarkIndex], 1f));
        objects[landMarkIndex].Add(DrawCube(Color.red, robotStates[landMarkIndex].position, 1f));
        List<GameObject> shapes = DrawArrow(Color.green, robotStates[landMarkIndex].position, robotStates[landMarkIndex].rotation * Vector3.forward);
        foreach (var shape in shapes)
        {
            objects[landMarkIndex].Add(shape);
        }


    }

 


    private GameObject DrawCube(Color color, Vector3 position, float size)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = position;
        cube.transform.localScale = Vector3.one * size;

        // Set the color
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        return cube;
    }

    private GameObject DrawLine(Color color, Vector3 start, Vector3 end)
    {
        GameObject lineObject = new GameObject("Line");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        // Assign a default material for visibility
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        return lineObject;
    }

    private List<GameObject> DrawArrow(Color color, Vector3 position, Vector3 direction)
    {
        Vector3 end = position + direction.normalized * 2f;
        GameObject l1 = DrawLine(color, position, end);

        // Draw arrowhead
        Vector3 right = Quaternion.Euler(0, 150, 0) * direction.normalized * 0.2f;
        Vector3 left = Quaternion.Euler(0, -150, 0) * direction.normalized * 0.2f;
        GameObject l2 = DrawLine(color, end, end - right);
        GameObject l3 = DrawLine(color, end, end - left);

        //Or you can do it this way
        return new List<GameObject> { l1, l2, l3 };
    }
    public void setSensorEnabled(bool b)
    {
        sensorEnabled = b;
    }
}
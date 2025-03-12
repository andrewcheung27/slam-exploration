using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private PoseGraph poseGraph = null;
    private List<GameObject> poseNodesDisplayed = null;
    private List<Point> globalPointCloud = null;
    private List<GameObject> poseNodesGroundTruthDisplayed = null;
    private bool gameRunning = false;  // whether the game is running now
    private TextMeshProUGUI restartStopButtonText = null;
    private string gameSceneName = null;
    private PlayerController playerController = null;

    public static GameManager instance;
    public bool debug = false;
    public TextMeshProUGUI startText;
    public TextMeshProUGUI optimizingPoseGraphText;
    public TextMeshProUGUI metricsText;
    public TextMeshProUGUI nodeExplanationText;
    public Button startButton;
    public Button restartStopButton;
    public GameObject poseNodePrefab;  // to display nodes on the pose graph
    public GameObject poseNodeGroundTruthPrefab;  // to display ground truth for the nodes
    public string viewMapSceneName;  // name of scene for viewing point cloud map and trajectory
    public GameObject player;  // GameObject with PlayerController component
    public GameObject UI;  // parent of all UI elements, including EventManager

    void Awake()
    {
        // make sure there is only one instance of GameManager
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
            return;
        }

        poseGraph = new PoseGraph(_debug: debug);
        poseNodesDisplayed = new List<GameObject>();
        poseNodesGroundTruthDisplayed = new List<GameObject>();

        Time.timeScale = 0;  // don't start game until user clicks start button

        // show start stuff (should be inactive by default in the scene)
        startButton.gameObject.SetActive(true);
        startText.gameObject.SetActive(true);

        // button setup
        startButton.onClick.AddListener(HandleStartStop);
        restartStopButton.onClick.AddListener(HandleStartStop);
        restartStopButtonText = restartStopButton.GetComponentInChildren<TextMeshProUGUI>();

        // save name of the game scene so we can return to it later
        gameSceneName = SceneManager.GetActiveScene().name;

        // preserve these when we switch between environment and map scenes
        DontDestroyOnLoad(gameObject);  // this GameManager
        DontDestroyOnLoad(UI);

        // store PlayerController component
        player.TryGetComponent(out playerController);
    }

    public PoseGraph GetPoseGraph()
    {
        return poseGraph;
    }

    IEnumerator<string> HandleStart()
    {
        // load game scene if it's not already loaded
        AsyncOperation asyncLoad = null;
        if (SceneManager.GetActiveScene().name != gameSceneName) {
            asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        }

        // wait until scene loading is finished, then move on to the code below
        while (asyncLoad != null && !asyncLoad.isDone) {
            yield return null;
        }

        // start game
        Time.timeScale = 1;

        // remove start UI
        startButton.gameObject.SetActive(false);
        startText.gameObject.SetActive(false);

        // restart UI
        restartStopButton.gameObject.SetActive(true);
        restartStopButtonText.text = "Stop Simulation";

        // metrics UI
        if (metricsText != null) {
            metricsText.gameObject.SetActive(false);
        }

        // node explanation UI
        if (nodeExplanationText != null) {
            nodeExplanationText.gameObject.SetActive(false);
        }

        // enable sensor
        if (playerController != null) {
            playerController.setSensorCanBeEnabled(true);
        }

        // clear pose graph from previous run
        poseGraph.Clear();

        // delete the pose nodes from the previous run
        foreach (GameObject obj in poseNodesDisplayed) {
            Destroy(obj);
        }
        poseNodesDisplayed.Clear();

        // also delete pose nodes for ground truth from the previous run
        foreach (GameObject obj in poseNodesGroundTruthDisplayed) {
            Destroy(obj);
        }
        poseNodesGroundTruthDisplayed.Clear();

        gameRunning = true;
    }

    IEnumerator<string> HandleStop()
    {
        // optimize pose graph
        optimizingPoseGraphText.gameObject.SetActive(true);
        poseGraph.Optimize();

        // calculate metrics
        float absoluteTrajectoryErrorRMSE = poseGraph.CalculateAbsoluteTrajectoryErrorRMSE();
        if (metricsText != null) {
            metricsText.gameObject.SetActive(true);
            metricsText.text = "Absolute Trajectory Error RMSE: " + absoluteTrajectoryErrorRMSE.ToString("0.###");
        }

        // load scene to view the map
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(viewMapSceneName);

        // wait until scene loading is finished, then move on to the code below
        while (!asyncLoad.isDone) {
            yield return null;
        }

        // hide message since pose graph optimization is done
        optimizingPoseGraphText.gameObject.SetActive(false);

        // node explanation UI
        if (nodeExplanationText != null) {
            nodeExplanationText.gameObject.SetActive(true);
        }

        // disable sensor
        if (playerController != null) {
            playerController.setSensorCanBeEnabled(false);
            playerController.setSensorEnabled(false);
        }

        // update button text
        restartStopButtonText.text = "Restart Simulation";

        // show how SLAM performed
        EvaluateSLAM();

        gameRunning = false;
    }

    void HandleStartStop()
    {
        if (gameRunning) {
            StartCoroutine(HandleStop());
        }
        else {
            StartCoroutine(HandleStart());
        }
    }


    List<Point> FilterClosePointsGrid(List<Point> points, float cellSize)
    {
        Dictionary<Vector3Int, Point> grid = new Dictionary<Vector3Int, Point>();

        foreach (Point point in points)
        {
            Vector3Int cell = new Vector3Int(
                Mathf.FloorToInt(point.position.x / cellSize),
                Mathf.FloorToInt(point.position.y / cellSize),
                Mathf.FloorToInt(point.position.z / cellSize)
            );

            // Check if the cell already has a representative point
            if (!grid.ContainsKey(cell))
            {
                grid[cell] = point; // Store the first point found in this cell
            }
        }

        return new List<Point>(grid.Values);
    }


    void EvaluateSLAM()
    {
        VoxelRenderer voxelRenderer;
        List<Point> globalPointCloud = new List<Point>();

        // Collect point clouds
        foreach (PoseNode node in poseGraph.GetNodes())
        {
            globalPointCloud.AddRange(node.GetPointCloud());
        }

        // Filter with spatial grid (faster!)
        float gridSize = 0.5f; // Adjust grid size based on density
        List<Point> filteredCloud = FilterClosePointsGrid(globalPointCloud, gridSize);

        // Create visualization
        GameObject temp = Instantiate(poseNodePrefab, poseGraph.GetNodes()[0].GetPose().position, Quaternion.identity);
        if (temp.TryGetComponent<VoxelRenderer>(out voxelRenderer))
        {
            voxelRenderer.SetVoxels(filteredCloud);
        }

        // Spawn nodes
        foreach (PoseNode node in poseGraph.GetNodes())
        {
            GameObject nodeObj = Instantiate(poseNodePrefab, node.GetPose().position, Quaternion.identity);

            // label nodes with node number
            TextMeshProUGUI nodeLabel = nodeObj.GetComponentInChildren<TextMeshProUGUI>();
            nodeLabel.text = node.GetIndex().ToString();

            //if (nodeObj.TryGetComponent<VoxelRenderer>(out voxelRenderer))
            //{
            //    voxelRenderer.SetVoxels(node.GetPointCloud());
            //}
            poseNodesDisplayed.Add(nodeObj);

            GameObject nodeObjGT = Instantiate(poseNodeGroundTruthPrefab, node.GetPoseGroundTruth().position, Quaternion.identity);
            poseNodesGroundTruthDisplayed.Add(nodeObjGT);

            // label GT nodes with node number
            TextMeshProUGUI nodeLabelGT = nodeObjGT.GetComponentInChildren<TextMeshProUGUI>();
            nodeLabelGT.text = node.GetIndex().ToString();
        }
    }
}

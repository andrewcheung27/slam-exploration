using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private PoseGraph poseGraph;
    private PoseGraph poseGraphGroundTruth;
    private List<GameObject> poseNodesDisplayed;
    private List<GameObject> poseNodesGroundTruthDisplayed;
    private bool gameRunning = false;  // whether the game is running now
    private TextMeshProUGUI restartStopButtonText;

    public static GameManager instance;
    public bool debug = false;
    public TextMeshProUGUI startText;
    public Button startButton;
    public Button restartStopButton;
    public GameObject poseNodePrefab;  // to display nodes on the pose graph
    public GameObject poseNodeGroundTruthPrefab;  // to display ground truth for the nodes

    void Awake()
    {
        // make sure there is only one instance of GameManager
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }

        poseGraph = new PoseGraph(_debug: debug);
        poseGraphGroundTruth = new PoseGraph(_trackConstraints: false, _debug: false);
        poseNodesDisplayed = new List<GameObject>();
        poseNodesGroundTruthDisplayed = new List<GameObject>();

        Time.timeScale = 0;  // don't start game until user clicks start button

        startButton.onClick.AddListener(HandleStart);
        restartStopButton.onClick.AddListener(HandleRestartStop);
        restartStopButtonText = restartStopButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    public PoseGraph GetPoseGraph()
    {
        return poseGraph;
    }

    public PoseGraph GetPoseGraphGroundTruth()
    {
        return poseGraphGroundTruth;
    }

    void HandleStart()
    {
        Time.timeScale = 1;
        startButton.gameObject.SetActive(false);
        startText.gameObject.SetActive(false);
        restartStopButton.gameObject.SetActive(true);
        gameRunning = true;
    }

    void HandleRestartStop()
    {
        if (gameRunning) {
            // stop game
            Time.timeScale = 0;
            // update button text
            restartStopButtonText.text = "Restart Simulation";
            // show how SLAM performed
            EvaluateSLAM();
        }
        else {
            // restart game
            Time.timeScale = 1;
            // update button text
            restartStopButtonText.text = "Stop Simulation";

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
        }

        gameRunning = !gameRunning;
    }

    void DisplayPoseNodes(List<PoseNode> nodes, GameObject nodePrefab, List<GameObject> displayedNodes)
    {
        VoxelRenderer voxelRenderer;

        foreach (PoseNode node in nodes) {
            Debug.Log("Node point cloud: " + node.GetPointCloud());
            // spawn node GameObject based on its prefab
            GameObject n = Instantiate(nodePrefab, node.GetPose().position, Quaternion.identity);
            // attach VoxelRenderer script
            n.AddComponent<VoxelRenderer>();
            // display point cloud for that node
            if (n.TryGetComponent<VoxelRenderer>(out voxelRenderer)) {
                voxelRenderer.SetVoxels(node.GetPointCloud());
            }
            displayedNodes.Add(n);
        }
    }

    void EvaluateSLAM()
    {
        // display nodes in the pose graph (trajectory estimated by SLAM)
        DisplayPoseNodes(poseGraph.GetNodes(), poseNodePrefab, poseNodesDisplayed);

        // display ground truth for the nodes
        DisplayPoseNodes(poseGraphGroundTruth.GetNodes(), poseNodeGroundTruthPrefab, poseNodesGroundTruthDisplayed);
    }
}

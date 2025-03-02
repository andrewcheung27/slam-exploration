using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private PoseGraph poseGraph = null;
    private List<GameObject> poseNodesDisplayed = null;
    private List<GameObject> poseNodesGroundTruthDisplayed = null;
    private bool gameRunning = false;  // whether the game is running now
    private TextMeshProUGUI restartStopButtonText = null;
    private string gameSceneName = null;
    private PlayerController playerController = null;

    public static GameManager instance;
    public bool debug = false;
    public TextMeshProUGUI startText;
    public TextMeshProUGUI optimizingPoseGraphText;
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

        // enable sensor
        if (playerController != null) {
            playerController.setSensorEnabled(true);
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

        // load scene to view the map
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(viewMapSceneName);

        // wait until scene loading is finished, then move on to the code below
        while (!asyncLoad.isDone) {
            yield return null;
        }

        // hide message since pose graph optimization is done
        optimizingPoseGraphText.gameObject.SetActive(false);

        // disable sensor
        if (playerController != null) {
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


    void EvaluateSLAM()
    {
        VoxelRenderer voxelRenderer;

        foreach (PoseNode node in poseGraph.GetNodes()) {
            // spawn node GameObject based on its prefab
            GameObject nodeObj = Instantiate(poseNodePrefab, node.GetPose().position, Quaternion.identity);

            // display point cloud for that node
            if (nodeObj.TryGetComponent<VoxelRenderer>(out voxelRenderer)) {
                voxelRenderer.SetVoxels(node.GetPointCloud());
            }

            // keep track of nodes so we can clear them later
            poseNodesDisplayed.Add(nodeObj);

            // spawn ground truth node GameObject based on the other prefab
            GameObject nodeObjGT = Instantiate(poseNodeGroundTruthPrefab, node.GetPoseGroundTruth().position, Quaternion.identity);
            poseNodesGroundTruthDisplayed.Add(nodeObjGT);
        }
    }
}

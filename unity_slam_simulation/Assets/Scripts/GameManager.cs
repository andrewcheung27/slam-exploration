using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private PoseGraph poseGraph;
    private List<GameObject> poseNodesDisplayed;
    private bool gameRunning = false;  // whether the game is running now
    private TextMeshProUGUI restartStopButtonText;

    public static GameManager instance;
    public bool debug = false;
    public TextMeshProUGUI startText;
    public Button startButton;
    public Button restartStopButton;
    public GameObject poseNodePrefab;  // for displaying nodes on the pose graph

    void Awake()
    {
        // make sure there is only one instance of GameManager
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }

        poseGraph = new PoseGraph(debug);
        poseNodesDisplayed = new List<GameObject>();

        Time.timeScale = 0;  // don't start game until user clicks start button

        startButton.onClick.AddListener(HandleStart);
        restartStopButton.onClick.AddListener(HandleRestartStop);
        restartStopButtonText = restartStopButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public PoseGraph GetPoseGraph()
    {
        return poseGraph;
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
        }

        gameRunning = !gameRunning;
    }

    void EvaluateSLAM()
    {
        // display nodes in the pose graph (trajectory estimated by SLAM)
        List<PoseNode> nodes = poseGraph.GetNodes();
        foreach (PoseNode node in nodes) {
            GameObject n = Instantiate(poseNodePrefab, node.GetPose().position, Quaternion.identity);
            poseNodesDisplayed.Add(n);
        }
    }
}

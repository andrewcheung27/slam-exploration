using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class PoseGraph
{
    private bool trackConstraints;  // whether to track constraints (edges). if false, this class basically just stores a list of nodes.
    private bool debug;
    private List<PoseNode> nodes;
    // maps pair of PoseNodes to a Pose, representing a spatial constraint between the nodes
    private Dictionary<Tuple<PoseNode, PoseNode>, Pose> constraints;

    public PoseGraph(bool _trackConstraints=true, bool _debug=false)
    {
        trackConstraints = _trackConstraints;
        debug = _debug;

        nodes = new List<PoseNode>();
        constraints = new Dictionary<Tuple<PoseNode, PoseNode>, Pose>();
    }

    public List<PoseNode> GetNodes()
    {
        return nodes;
    }

    public void AddNode(PoseNode node)
    {
        PoseNode lastNode = null;
        if (nodes.Count > 0) {
            lastNode = nodes.Last();
        } 

        nodes.Add(node);
        if (debug) Debug.Log("Added node: " + node);

        if (lastNode != null) {
            // add constraint between previous and current nodes
            AddConstraint(lastNode, node, Pose.PoseDifference(lastNode.GetPose(), node.GetPose()));
            if (debug) Debug.Log($"Added constraint between nodes {lastNode.GetIndex()} and {node.GetIndex()}, with value {Pose.PoseDifference(lastNode.GetPose(), node.GetPose())}");
        }
    }

    private void AddConstraint(PoseNode node1, PoseNode node2, Pose pose)
    {
        constraints.Add(new Tuple<PoseNode, PoseNode>(node1, node2), pose);
    }

    public void Optimize()
    {
        // TODO: implement pose graph optimization
    }
}

using System;
using System.Collections.Generic;

public class PoseGraph
{
    private List<PoseNode> nodes;
    // maps pair of PoseNodes to a Pose, representing a spatial constraint between the nodes
    private Dictionary<Tuple<PoseNode, PoseNode>, Pose> constraints;

    public PoseGraph()
    {
        nodes = new List<PoseNode>();
        constraints = new Dictionary<Tuple<PoseNode, PoseNode>, Pose>();
    }

    public void AddNode(PoseNode node)
    {
        nodes.Add(node);
    }

    public void AddConstraint(PoseNode node1, PoseNode node2, Pose pose)
    {
        constraints.Add(new Tuple<PoseNode, PoseNode>(node1, node2), pose);
    }

    public void Optimize()
    {
        // TODO: implement pose graph optimization
    }
}

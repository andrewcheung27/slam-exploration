// pose graph stuff was based on this Cyrill Stachniss lecture: https://www.youtube.com/watch?v=uHbRKvD8TWg

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

public class PoseGraph
{
    private bool debug;
    private List<PoseNode> nodes;
    // maps pair of PoseNodes to a Pose, representing a spatial constraint between the nodes
    private Dictionary<Tuple<PoseNode, PoseNode>, Pose> constraints;
    private int poseDimensions = 3;  // number of dimensions we're using for a Pose (for simplicity, dimensions x, z, theta=0)
    private Matrix<float> normalEquationMatrix;  // H
    private Matrix<float> coefficientVector;  // b

    public PoseGraph(bool _debug=false)
    {
        debug = _debug;

        nodes = new List<PoseNode>();
        constraints = new Dictionary<Tuple<PoseNode, PoseNode>, Pose>();
    }

    public List<PoseNode> GetNodes()
    {
        return nodes;
    }

    public void Clear()
    {
        nodes.Clear();
        constraints.Clear();
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

    // calculate RMSE of Absolute Trajectory Error as sum of the differences between each node's estimated position and its ground truth position.
    // currently, this doesn't include rotation.
    public float CalculateAbsoluteTrajectoryErrorRMSE()
    {
        float sum_error_squared = 0f;

        foreach (PoseNode node in nodes) {
            sum_error_squared += Mathf.Pow(Mathf.Abs((node.GetPose().position - node.GetPoseGroundTruth().position).magnitude), 2);
        }

        return Mathf.Sqrt(sum_error_squared / nodes.Count);
    }

    public Matrix<float> ComputeError(PoseNode node1, PoseNode node2, Pose constraint)
    {
        Vector3 nodeDiff = node2.GetPose().position - node1.GetPose().position;
        Vector3 error = constraint.position - nodeDiff;
        float[, ] arr = {{error.x}, {error.y}, {error.z}};  // 3x1
        return Matrix<float>.Build.DenseOfArray(arr);
    }

    // simplified Jacobian blocks with pose = (x, z, theta) and theta = 0
    public Tuple<Matrix<float>, Matrix<float>> ComputeJacobianBlocks(PoseNode node1, PoseNode node2)
    {
        float[, ] arrA = {
            {-1, 0, node2.GetPose().position.y - node1.GetPose().position.y}, 
            {0, -1, node1.GetPose().position.x - node2.GetPose().position.x}, 
            {0, 0, -1}
        };
        var A = Matrix<float>.Build.DenseOfArray(arrA);
        float[, ] arrB = {
            {1, 0, 0}, 
            {0, 1, 0}, 
            {0, 0, 1}
        };
        var B = Matrix<float>.Build.DenseOfArray(arrB);

        return new Tuple<Matrix<float>, Matrix<float>>(A, B);
    }

    // mutates normalEquationMatrix and coefficientVector to build linear system
    public void BuildLinearSystem()
    {
        // information matrix
        float[, ] omega = {{1, 0, 0}, {0, 1, 0}, {0, 0, 1}};
        var infoMatrix = Matrix<float>.Build.DenseOfArray(omega);
        // normal equation matrix
        normalEquationMatrix = Matrix<float>.Build.Sparse(nodes.Count * poseDimensions, nodes.Count * poseDimensions);
        // coefficient vector
        coefficientVector = Matrix<float>.Build.Sparse(nodes.Count * poseDimensions, 1);

        foreach (Tuple<PoseNode, PoseNode> nodePair in constraints.Keys) {
            // indices of the nodes
            int i = nodePair.Item1.GetIndex() * poseDimensions;
            int j = nodePair.Item2.GetIndex() * poseDimensions;

            // spatial constraint between the current pair of nodes
            Pose constraint = constraints[nodePair];

            // compute error
            Matrix<float> error = ComputeError(nodePair.Item1, nodePair.Item2, constraint);

            // compute blocks of Jacobian
            var (A, B) = ComputeJacobianBlocks(nodePair.Item1, nodePair.Item2);

            // update coefficient vector
            coefficientVector.SetSubMatrix(i, 0, A.Transpose() * infoMatrix * error);
            coefficientVector.SetSubMatrix(j, 0, B.Transpose() * infoMatrix * error);

            // update normal equation matrix
            normalEquationMatrix.SetSubMatrix(i, i, A.Transpose() * infoMatrix * A);
            normalEquationMatrix.SetSubMatrix(i, j, A.Transpose() * infoMatrix * B);
            normalEquationMatrix.SetSubMatrix(j, i, B.Transpose() * infoMatrix * A);
            normalEquationMatrix.SetSubMatrix(j, j, B.Transpose() * infoMatrix * B);
        }
    }

    public void Optimize()
    {
        // start all nodes at fixed x and z coords, so we can simulate pose graph optimization.
        // this is a workaround because we aren't simulating loop closing or landmarks.
        for (int i = 0; i < nodes.Count; i++) {
            nodes[i].SetPose(new Pose(new Vector3(i, nodes[i].GetPose().position.y, i), Vector3.zero));
        }

        int maxIter = 1;  // doing more than 1 iteration seemed to make numbers get super big
        for (int iter = 0; iter < maxIter; iter++) {
            // build linear system
            BuildLinearSystem();
            var H = normalEquationMatrix;
            var b = coefficientVector;

            // solve linear system
            Matrix<float> change = H.Solve(b);

            // update node positions
            for (int i = 0; i < nodes.Count; i++) {
                // Debug.Log("old pose: " + nodes[i].GetPose());
                Pose p = nodes[i].GetPose();

                var changeX = change[i * poseDimensions, 0];
                if (!float.IsFinite(changeX)) {
                    break;
                }

                var changeZ = change[i * poseDimensions + 1, 0];
                if (!float.IsFinite(changeZ)) {
                    break;
                }

                p.SetPosition(p.position + new Vector3(changeX, 0, changeZ));
                // Debug.Log("new pose: " + nodes[i].GetPose());
            }
        }
    }
}

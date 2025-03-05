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
    private int poseDimensions = 3;  // number of dimensions we're using for a Pose (3 dimensions of position, we aren't using rotation for simplicty)
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

    // calculate Absolute Trajectory Error as sum of the differences between each node's estimated position and its ground truth position.
    // currently, this doesn't include rotation.
    public float CalculateAbsoluteTrajectoryError()
    {
        float error = 0f;

        foreach (PoseNode node in nodes) {
            error += Mathf.Abs((node.GetPose().position - node.GetPoseGroundTruth().position).magnitude);
        }

        return error;
    }

    public Matrix<float> ComputeError(PoseNode node1, PoseNode node2, Pose constraint)
    {
        // TODO: this should be observed distance - difference between nodes in current graph (see 54:30)
        Vector3 nodeDiff = node2.GetPose().position - node1.GetPose().position;
        Vector3 error = constraint.position - nodeDiff;
        // float[, ] arr = {{constraint.position.x}, {constraint.position.y}, {constraint.position.z}};  // 3x1
        float[, ] arr = {{error.x, error.y, error.z}};
        return Matrix<float>.Build.DenseOfArray(arr);
    }

    public Tuple<Matrix<float>, Matrix<float>> ComputeJacobianBlocks(Pose p)
    {
        float[, ] arrA = {
            {-1, 0, p.position.y}, 
            {0, -1, -1 * p.position.x}, 
            {0, 0, p.position.z}
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
        // TODO: put information matrix somewhere else
        float[, ] omega = {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};
        var infoMatrix = Matrix<float>.Build.DenseOfArray(omega);
        // normal equation matrix
        normalEquationMatrix = Matrix<float>.Build.Sparse(nodes.Count * poseDimensions, nodes.Count * poseDimensions);
        // coefficient vector
        coefficientVector = Matrix<float>.Build.Sparse(nodes.Count * poseDimensions, 1);

        // for each constraint:
        foreach (Tuple<PoseNode, PoseNode> nodePair in constraints.Keys) {
            // indices of the nodes
            // TODO: explain multiply by 3
            int i = nodePair.Item1.GetIndex() * poseDimensions;
            int j = nodePair.Item2.GetIndex() * poseDimensions;
            // spatial constraint between the current pair of nodes
            Pose constraint = constraints[nodePair];

            // compute error
            Matrix<float> error = ComputeError(nodePair.Item1, nodePair.Item2, constraint);

            // compute blocks of Jacobian
            var (A, B) = ComputeJacobianBlocks(constraint);

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

    // TODO: implement pose graph optimization
    public void Optimize()
    {
        Debug.Log("PoseGraph.Optimize() called");
        return;

        // x is the pose graph
        bool converged = false;

        while (!converged) {
            // build linear system
            BuildLinearSystem();
            var H = normalEquationMatrix;
            var b = coefficientVector;
            Debug.Log("H = " + H);
            Debug.Log("b = " + b);

            // solve linear system
            // Vector<float> deltaX = H.TransposeThisAndMultiply(H).Cholesky().Solve(H.TransposeThisAndMultiply(b));
            // Matrix<float> deltaX = H.Cholesky().Solve(b);
            Matrix<float> deltaX = H.Solve(b);
            Debug.Log("DELTA X: " + deltaX);

            // TODO: update node positions
            // // x = x + deltaX
            // for (int i = 0; i < nodes.Count; i++) {
            //     Pose p = nodes[i].GetPose();
            //     var fuck = deltaX[i, 0];
            //     // var x = (float) deltaX[i, 0];
            //     // var y = (float) deltaX[i, 1];
            //     // var z = (float) deltaX[i, 2];
            //     // p.SetPosition(p.position + new Vector3(x, y, z));
            //     p.SetPosition(p.position + new Vector3(fuck, fuck, fuck));
            //     // nodes[i].SetPose(p);
            // }

            // TODO: converge if error is less than threshold
            converged = true;
        }

        // return x
    }
}

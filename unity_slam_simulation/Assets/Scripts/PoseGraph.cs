// pose graph stuff was based on this Cyrill Stachniss lecture: https://www.youtube.com/watch?v=uHbRKvD8TWg

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
// using MathNet.Numerics.LinearAlgebra.Double;

public class PoseGraph
{
    private bool debug;
    private List<PoseNode> nodes;
    // maps pair of PoseNodes to a Pose, representing a spatial constraint between the nodes
    private Dictionary<Tuple<PoseNode, PoseNode>, Pose> constraints;
    private int poseDimensions = 3;  // number of dimensions we're using for a Pose (3 dimensions of position, we aren't using rotation for simplicty)
    private Matrix<double> normalEquationMatrix;  // H
    private Vector<double> coefficientVector;  // b

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

    public Matrix<double> ComputeError(Pose p)
    {
        // TODO: this should be observed distance - difference between nodes in current graph (see 54:30)
        double[, ] arr = {{p.position.x}, {p.position.y}, {p.position.z}};  // 3x1
        return Matrix<double>.Build.DenseOfArray(arr);
    }

    public Tuple<Matrix<double>, Matrix<double>> ComputeJacobianBlocks(Pose p)
    {
        double[, ] arrA = {
            {-1, 0, p.position.y}, 
            {0, -1, -1 * p.position.x}, 
            {0, 0, p.position.z}
        };
        var A = Matrix<double>.Build.DenseOfArray(arrA);
        double[, ] arrB = {
            {1, 0, 0}, 
            {0, 1, 0}, 
            {0, 0, 1}
        };
        var B = Matrix<double>.Build.DenseOfArray(arrB);

        return new Tuple<Matrix<double>, Matrix<double>>(A, B);
    }

    // mutates normalEquationMatrix and coefficientVector to build linear system
    public void BuildLinearSystem()
    {
        // TODO: put information matrix somewhere else
        double[, ] omega = {{1.0, 2.0, 3.0}, {4.0, 5.0, 6.0}, {7.0, 8.0, 9.0}};
        var infoMatrix = Matrix<double>.Build.DenseOfArray(omega);
        // normal equation matrix
        normalEquationMatrix = Matrix<double>.Build.Sparse(100, 100);
        // coefficient vector
        coefficientVector = Vector<double>.Build.Sparse(100, 1);

        // for each constraint:
        foreach (Tuple<PoseNode, PoseNode> nodePair in constraints.Keys) {
            // indices of the nodes
            // TODO: explain multiply by 3
            int i = nodePair.Item1.GetIndex() * poseDimensions;
            int j = nodePair.Item2.GetIndex() * poseDimensions;
            // spatial constraint between the current pair of nodes
            Pose constraint = constraints[nodePair];

            // compute error
            Matrix<double> error = ComputeError(constraint);

            // compute blocks of Jacobian
            var (A, B) = ComputeJacobianBlocks(constraint);

            // update coefficient vector
            coefficientVector.SetSubVector(i, poseDimensions, (A.Transpose() * infoMatrix * error).Column(0));
            coefficientVector.SetSubVector(j, poseDimensions, (B.Transpose() * infoMatrix * error).Column(0));

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
        // x is the pose graph
        bool converged = false;

        while (!converged) {
            // build linear system
            BuildLinearSystem();
            var H = normalEquationMatrix;
            var b = coefficientVector;

            // solve linear system
            Vector<double> deltaX = H.TransposeThisAndMultiply(H).Cholesky().Solve(H.TransposeThisAndMultiply(b));

            // x = x + deltaX
        }

        // return x
    }
}

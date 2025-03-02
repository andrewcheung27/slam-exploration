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

    public Matrix<double> ComputeJacobianBlock(PoseNode p1, PoseNode p2)
    {
        // shape 3x1
        return null;
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

    // // takes a matrix and a smaller square block. puts the block in the matrix at start and end coordinates.
    // // public void SetMatrixBlock(Matrix<double> matrix, Matrix<double> block, int startRow, int startCol, int endRow, int endCol)
    // public void SetMatrixBlock(Matrix<double> matrix, Matrix<double> block, int rowMiddle, int colMiddle)
    // {
    //     int blockRow = 0;
    //     int blockCol = 0;
    //     int startRow = rowMiddle - block.SetSubMatrix

    //     for (int row = startRow; row <= endRow; row++) {
    //         for (int col = startCol; col <= endCol; col++) {
    //             matrix[row, col] = block[blockRow, blockCol];
    //             blockCol++;
    //         }
    //         blockRow++;
    //         blockCol = 0;
    //     }
    // }

    public Tuple<Matrix<double>, Vector<double>> BuildLinearSystem()
    {
        // TODO: put information matrix somewhere else
        double[, ] omega = {{1.0, 2.0, 3.0}, {4.0, 5.0, 6.0}, {7.0, 8.0, 9.0}};
        var infoMatrix = Matrix<double>.Build.DenseOfArray(omega);
        // normal equation matrix
        var H = Matrix<double>.Build.Sparse(100, 100);
        // coefficient vector
        var b = Vector<double>.Build.Sparse(100, 1);

        // for each constraint:
        foreach (Tuple<PoseNode, PoseNode> nodePair in constraints.Keys) {
            // indices of the nodes
            int i = nodePair.Item1.GetIndex();
            int j = nodePair.Item2.GetIndex();
            // spatial constraint between the current pair of nodes
            Pose constraint = constraints[nodePair];

            // compute error
            Matrix<double> error = ComputeError(constraint);

            // compute blocks of Jacobian
            var (A, B) = ComputeJacobianBlocks(constraint);
            // Matrix<double> jacobian_ii = ComputeJacobianBlock(constraint.Item1, constraint.Item1);
            // Matrix<double> jacobian_ij = ComputeJacobianBlock(constraint.Item1, constraint.Item2);
            // Matrix<double> jacobian_ji = ComputeJacobianBlock(constraint.Item2, constraint.Item1);
            // Matrix<double> jacobian_jj = ComputeJacobianBlock(constraint.Item2, constraint.Item2);

            // update coefficient vector
            // b[i] = (jacobian_ii.Transpose() * info_matrix * error)[0, 0];
            // b[j] = (jacobian_jj.Transpose() * info_matrix * error)[0, 0];
            // b[i * 3 + 0] = 

            // update normal equation matrix
            // H[i, i] = (jacobian_ii.Transpose() * info_matrix * jacobian_ii)[0, 0];
            // H[i, j] = (jacobian_ij.Transpose() * info_matrix * jacobian_ij)[0, 0];
            // H[j, i] = (jacobian_ji.Transpose() * info_matrix * jacobian_ji)[0, 0];
            // H[j, j] = (jacobian_jj.Transpose() * info_matrix * jacobian_jj)[0, 0];
            H.SetSubMatrix(i - 1, i - 1, A.Transpose() * infoMatrix * A);
            H.SetSubMatrix(i - 1, j - 1, A.Transpose() * infoMatrix * B);
            H.SetSubMatrix(j - 1, i - 1, B.Transpose() * infoMatrix * A);
            H.SetSubMatrix(j - 1, j - 1, B.Transpose() * infoMatrix * B);
        }

        return new Tuple<Matrix<double>, Vector<double>>(H, b);
    }

    // TODO: implement pose graph optimization
    public void Optimize()
    {
        // x is the pose graph
        bool converged = false;

        // while (!converged)
        while (!converged) {
            var (H, b) = BuildLinearSystem();

            // deltaX = solveSparse(H * deltaX = -b)
            // var deltaX = H.Solve(b);
            Vector<double> deltaX = H.TransposeThisAndMultiply(H).Cholesky().Solve(H.TransposeThisAndMultiply(b));

            // x = x + deltaX
        }

        // return x
    }
}

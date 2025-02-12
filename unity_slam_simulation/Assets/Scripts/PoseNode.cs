using System.Collections.Generic;

public class PoseNode
{
    private int index;
    private Pose pose;
    private int time;
    private List<Point> pointCloud;

    public PoseNode(int _index, Pose _pose, int _time, List<Point> _pointCloud)
    {
        index = _index;
        pose = _pose;
        time = _time;
        pointCloud = _pointCloud;
    }

    // use index for hash code. whoever makes the pose graph is responsible for keeping the PoseNode indices unique.
    public override int GetHashCode()
    {
        return index.GetHashCode();
    }

    public override string ToString()
    {
        return $"PoseNode(index={index}, pose={pose})";
    }

    public int GetIndex()
    {
        return index;
    }

    public Pose GetPose()
    {
        return pose;
    }

    public List<Point> GetPointCloud()
    {
        return pointCloud;
    }
}

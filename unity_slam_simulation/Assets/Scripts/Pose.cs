using UnityEngine;

public class Pose
{
    public Vector3 position;
    public Vector3 rotation;

    public static Pose PoseDifference(Pose pose1, Pose pose2)
    {
        // TODO: is this math correct?
        return new Pose(pose2.position - pose1.position, pose2.rotation - pose1.rotation);
    }

    public Pose(Vector3 _position, Vector3 _rotation)
    {
        position = _position;
        rotation = _rotation;
    }

    public override string ToString()
    {
        return $"Pose(position={position}, rotation={rotation})";
    }
}

using UnityEngine;


// represents a point in a pointcloud map
public class Point
{
    public Vector3 position;
    public Color color;

    public Point(Vector3 _position, Color _color)
    {
        position = _position;
        color = _color;
    }
}

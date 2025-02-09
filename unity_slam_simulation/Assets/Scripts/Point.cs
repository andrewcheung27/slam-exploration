using UnityEngine;


// represents a point in a pointcloud map
public class Point
{
    private Vector3 position;
    private Color color;

    public Point(Vector3 _position, Color _color)
    {
        position = _position;
        color = _color;
    }
}

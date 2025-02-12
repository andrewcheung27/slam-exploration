// based on Sam Schiffer's point cloud tutorial: https://www.youtube.com/watch?v=y6KwsRkQ86U

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class VoxelRenderer : MonoBehaviour
{
    ParticleSystem system;
    ParticleSystem.Particle[] voxels;
    bool voxelIsUpdated = false;
    public float voxelScale = 0.1f;
    public float scale = 1f;

    void Start()
    {
        system = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (voxelIsUpdated) {
            system.SetParticles(voxels, voxels.Length);
            voxelIsUpdated = false;
        }
    }

    public void SetVoxels(List<Point> points)
    {
        voxels = new ParticleSystem.Particle[points.Count];

        for (int i = 0; i < points.Count; i++) {
            // voxels[i].position = points[i].position * scale;
            voxels[i].position = transform.InverseTransformPoint(points[i].position);
            voxels[i].startColor = points[i].color;
            voxels[i].startSize = voxelScale;
        }

        voxelIsUpdated = true;
    }
}

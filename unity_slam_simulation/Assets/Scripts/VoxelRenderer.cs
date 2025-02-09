// based on Sam Schiffer's point cloud tutorial: https://www.youtube.com/watch?v=y6KwsRkQ86U

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

    public void SetVoxels(Vector3[] positions, Color[] colors)
    {
        voxels = new ParticleSystem.Particle[positions.Length];

        for (int i = 0; i < positions.Length; i++) {
            voxels[i].position = positions[i] * scale;
            voxels[i].startColor = colors[i];
            voxels[i].startSize = voxelScale;
        }

        voxelIsUpdated = true;
    }
}

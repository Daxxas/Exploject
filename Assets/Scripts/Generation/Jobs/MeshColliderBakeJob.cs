
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct MeshColliderBakeJob : IJob
{
    public int meshId;
    
    public void Execute()
    {
        Physics.BakeMesh(meshId, false);    
    }
}

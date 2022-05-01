
using Unity.Jobs;
using UnityEngine;

public struct MeshColliderBakeJob : IJob
{
    public int meshId;
    
    public void Execute()
    {
        Physics.BakeMesh(meshId, false);    
    }
}

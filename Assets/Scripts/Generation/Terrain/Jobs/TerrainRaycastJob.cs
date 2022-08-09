using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


// TODO The day I need to raycast on terrain without colliders
public struct TerrainRaycastJob : IJob
{
    public float max;
    public float min;
    public bool fromUp;
    public int resolution;

    public NativeArray<float> terrainData;

    public float2 terrainPosition;
    public Vector3 result;

    public NativeArray<int> triangulation;
    public NativeArray<int> cornerIndexAFromEdge;
    public NativeArray<int> cornerIndexBFromEdge;

    public void Execute()
    {
        float precedentValue = -1;
        for (float y = max; y > min; y--)
        {
            int3 xyz = new int3((int)math.floor(terrainPosition.x), (int)math.floor(y), (int)math.floor(terrainPosition.y));
            int idx = MathUtil.to1D(xyz.x, xyz.y, xyz.z, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight);

            if (terrainData[idx] >= 0)
            {
                // raycast met terrain
                
                // reproduce marching cube at position
                NativeArray<CubePoint> marchCube = new NativeArray<CubePoint>(8, Allocator.Temp);
                
                #region Marche Cube
                marchCube[0] = new CubePoint()
                {
                    p = new int3(xyz.x, xyz.y, xyz.z),
                    val = terrainData[MathUtil.to1D(xyz.x, xyz.y, xyz.z, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight)]
                };
                marchCube[1] = new CubePoint()
                {
                    p = new int3(xyz.x + resolution, xyz.y, xyz.z),
                    val = terrainData[MathUtil.to1D(xyz.x + resolution, xyz.y, xyz.z, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight)]
                };
                marchCube[2] = new CubePoint()
                {
                    p = new int3(xyz.x + resolution, xyz.y, xyz.z + resolution),
                    val = terrainData[MathUtil.to1D(xyz.x + resolution, xyz.y, xyz.z + resolution, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight)]
                };
                marchCube[3] = new CubePoint()
                {
                    p = new int3(xyz.x, xyz.y, xyz.z + resolution),
                    val = terrainData[MathUtil.to1D(xyz.x, xyz.y, xyz.z + resolution, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight)]
                };
                marchCube[4] = new CubePoint()
                {
                    p = new int3(xyz.x, xyz.y + resolution, xyz.z),
                    val = terrainData[MathUtil.to1D(xyz.x, xyz.y + resolution, xyz.z, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight)]
                };
                marchCube[5] = new CubePoint()
                {
                    p = new int3(xyz.x + resolution, xyz.y + resolution, xyz.z),
                    val = terrainData[MathUtil.to1D(xyz.x + resolution, xyz.y + resolution, xyz.z, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight)]
                };
                marchCube[6] = new CubePoint()
                {
                    p = new int3(xyz.x + resolution, xyz.y + resolution, xyz.z + resolution),
                    val = terrainData[MathUtil.to1D(xyz.x + resolution, xyz.y + resolution, xyz.z + resolution, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight)]
                };
                marchCube[7] = new CubePoint()
                {
                    p = new int3(xyz.x, xyz.y + resolution, xyz.z + resolution),
                    val = terrainData[MathUtil.to1D(xyz.x, xyz.y + resolution, xyz.z + resolution, MapDataGenerator.SupportedChunkSize, MapDataGenerator.chunkHeight)]
                };
                #endregion
                
                int cubeindex = 0;
                if (marchCube[0].val < MapDataGenerator.threshold) cubeindex |= 1;
                if (marchCube[1].val < MapDataGenerator.threshold) cubeindex |= 2;
                if (marchCube[2].val < MapDataGenerator.threshold) cubeindex |= 4;
                if (marchCube[3].val < MapDataGenerator.threshold) cubeindex |= 8;
                if (marchCube[4].val < MapDataGenerator.threshold) cubeindex |= 16;
                if (marchCube[5].val < MapDataGenerator.threshold) cubeindex |= 32;
                if (marchCube[6].val < MapDataGenerator.threshold) cubeindex |= 64;
                if (marchCube[7].val < MapDataGenerator.threshold) cubeindex |= 128;
                
                // Safeguard but should never happen
                if (cubeindex == 255 || cubeindex == 0)
                {
                    result = Vector3.zero;
                    return;
                }
                
                // for (int i = 0; triangulation[cubeindex * 16 + i] != -1; i += 3)
                // {
                //     NativeArray<int> triangle = new Triangle();
                //     
                //     // For each side (vertex) of the triangle
                //     for (int j = 0; j < 3; j++)
                //     {
                //         // Get corresponding edge
                //         int indexA = cornerIndexAFromEdge[triangulation[cubeindex * 16 + i + j]];
                //         int indexB = cornerIndexBFromEdge[triangulation[cubeindex * 16 + i + j]];
                //         Edge edge = new Edge(marchCube[indexA].p, marchCube[indexB].p);
                //
                //         // Keep edge for triangle, multiple triangles can reference the same edge
                //         triangle[j] = edge;
                //     }
                // }
            }
        }
    }
}
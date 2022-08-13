using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

// Second job to be called from CreateChunk after map data is generated
[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct MarchCubeJob : IJobParallelFor
{
    // Input
    [ReadOnly]
    public NativeArray<float> map;
    [ReadOnly]
    public NativeArray<int> triangulation;
    [ReadOnly]
    public NativeArray<int> cornerIndexAFromEdge;
    [ReadOnly]
    public NativeArray<int> cornerIndexBFromEdge;
    
    // Output
    [WriteOnly]
    public NativeQueue<Triangle>.ParallelWriter triangles;
    [WriteOnly]
    public NativeParallelHashMap<Edge, Vertex>.ParallelWriter vertices;

    public int resolution;
    
    public int supportedChunkSize => MapDataGenerator.ChunkSize + resolution * 3;
    
    // job is IJobParallelFor, one index = 1 cube tested
    public void Execute(int idx)
    {
        // Cube currently tested, in array so it's easier to follow
        NativeArray<CubePoint> marchCube = new NativeArray<CubePoint>(8, Allocator.Temp);
        
        // Remember :
        // Map data generates 18x18 (ignoring height) data to calculate normals
        // Marching will output a vertices array of 18x18 to allow mesh generation to calculate normals
        // but the mesh generation should output a 16x16 vertices array

        int3 xyz = to3D(idx, supportedChunkSize, MapDataGenerator.chunkHeight);

        // Don't calculate when we are too close to the edge of the chunk to prevent out of bound or if it's useless
        if (xyz.x > MapDataGenerator.ChunkSize+resolution || xyz.y >= MapDataGenerator.chunkHeight - resolution || xyz.z > MapDataGenerator.ChunkSize+resolution)
        {
            return;
        }
        
        // put march cube in array to have cleaner code after
        #region March cube definition 
        marchCube[0] = new CubePoint()
        {
            p = new int3(xyz.x, xyz.y, xyz.z),
            val = map[MathUtil.to1D(xyz.x, xyz.y, xyz.z, supportedChunkSize, MapDataGenerator.chunkHeight)]
        };
        marchCube[1] = new CubePoint()
        {
            p = new int3(xyz.x + resolution, xyz.y, xyz.z),
            val = map[MathUtil.to1D(xyz.x + resolution, xyz.y, xyz.z, supportedChunkSize, MapDataGenerator.chunkHeight)]
        };
        marchCube[2] = new CubePoint()
        {
            p = new int3(xyz.x + resolution, xyz.y, xyz.z + resolution),
            val = map[MathUtil.to1D(xyz.x + resolution, xyz.y, xyz.z + resolution, supportedChunkSize, MapDataGenerator.chunkHeight)]
        };
        marchCube[3] = new CubePoint()
        {
            p = new int3(xyz.x, xyz.y, xyz.z + resolution),
            val = map[MathUtil.to1D(xyz.x, xyz.y, xyz.z + resolution, supportedChunkSize, MapDataGenerator.chunkHeight)]
        };
        marchCube[4] = new CubePoint()
        {
            p = new int3(xyz.x, xyz.y + resolution, xyz.z),
            val = map[MathUtil.to1D(xyz.x, xyz.y + resolution, xyz.z, supportedChunkSize, MapDataGenerator.chunkHeight)]
        };
        marchCube[5] = new CubePoint()
        {
            p = new int3(xyz.x + resolution, xyz.y + resolution, xyz.z),
            val = map[MathUtil.to1D(xyz.x + resolution, xyz.y + resolution, xyz.z, supportedChunkSize, MapDataGenerator.chunkHeight)]
        };
        marchCube[6] = new CubePoint()
        {
            p = new int3(xyz.x + resolution, xyz.y + resolution, xyz.z + resolution),
            val = map[MathUtil.to1D(xyz.x + resolution, xyz.y + resolution, xyz.z + resolution, supportedChunkSize, MapDataGenerator.chunkHeight)]
        };
        marchCube[7] = new CubePoint()
        {
            p = new int3(xyz.x, xyz.y + resolution, xyz.z + resolution),
            val = map[MathUtil.to1D(xyz.x, xyz.y + resolution, xyz.z + resolution, supportedChunkSize, MapDataGenerator.chunkHeight)]
        };
        #endregion
        
        // From values of the cube corners, find the cube configuration index
        int cubeindex = 0;
        if (marchCube[0].val < MapDataGenerator.threshold) cubeindex |= 1;
        if (marchCube[1].val < MapDataGenerator.threshold) cubeindex |= 2;
        if (marchCube[2].val < MapDataGenerator.threshold) cubeindex |= 4;
        if (marchCube[3].val < MapDataGenerator.threshold) cubeindex |= 8;
        if (marchCube[4].val < MapDataGenerator.threshold) cubeindex |= 16;
        if (marchCube[5].val < MapDataGenerator.threshold) cubeindex |= 32;
        if (marchCube[6].val < MapDataGenerator.threshold) cubeindex |= 64;
        if (marchCube[7].val < MapDataGenerator.threshold) cubeindex |= 128;
        
        if (cubeindex == 255 || cubeindex == 0) 
            return;

        // From the cube configuration, add triangles & vertices to mesh 
        for (int i = 0; triangulation[cubeindex * 16 + i] != -1; i += 3)
        {
            Triangle triangle = new Triangle();
            
            // For each side (vertex) of the triangle
            for (int j = 0; j < 3; j++)
            {
                // Get corresponding edge
                int indexA = cornerIndexAFromEdge[triangulation[cubeindex * 16 + i + j]];
                int indexB = cornerIndexBFromEdge[triangulation[cubeindex * 16 + i + j]];
                Edge edge = new Edge(marchCube[indexA].p, marchCube[indexB].p);

                // Only one vertices can exist per edge
                float3 vertexPos = FindVertexPos(
                    marchCube[indexA].p, marchCube[indexB].p, marchCube[indexA].val,
                        marchCube[indexB].val) - new float3(resolution, resolution, resolution);
                
                vertices.TryAdd(new Edge(marchCube[indexA].p, marchCube[indexB].p), new Vertex(vertexPos, new float4(0,0,0,0)));
                
                // Keep edge for triangle, multiple triangles can reference the same edge
                triangle[j] = edge;
                
                // Determine if vertex is border
                bool isBorderIndexA = marchCube[indexA].p.x < resolution || marchCube[indexA].p.z < resolution || 
                                  marchCube[indexA].p.x > MapDataGenerator.ChunkSize+(resolution) || marchCube[indexA].p.z > MapDataGenerator.ChunkSize+(resolution);
                bool isBorderIndexB = marchCube[indexB].p.x < resolution || marchCube[indexB].p.z < resolution ||
                                  marchCube[indexB].p.x > MapDataGenerator.ChunkSize+(resolution) || marchCube[indexB].p.z > MapDataGenerator.ChunkSize+(resolution);
                triangle.SetEdgeBorder(j, isBorderIndexA || isBorderIndexB);
            }

            // Triangles are stored in a queue because we don't know how many triangles we will get & we can write in parallel easily in a queue
            triangles.Enqueue(triangle);
        }

        marchCube.Dispose();
    }
    
    private float3 FindVertexPos(float3 p1, float3 p2, float v1val, float v2val)
    {

        if(Mathf.Abs(v1val) < 0.0001)
        {
            return p1;
        }

        if (Mathf.Abs(v2val) < 0.0001)
        {
            return p2;
        }

        if (Mathf.Abs(v1val - v2val) < 0.0001)
        {
            return p1;
        }
        float3 position = new float3(0,0,0);
    
        float mu = (MapDataGenerator.threshold - v1val) / (v2val - v1val);
        position.x = p1.x + mu * (p2.x - p1.x);
        position.y = p1.y + mu * (p2.y - p1.y);
        position.z = p1.z + mu * (p2.z - p1.z);
    
        return position;
    }
    
    public int BiomeTo1D( int x, int z)
    {
        // Debug.Log($"[JOB] Getting biome at {x}, {z} = index{x + supportedChunkSize * z} {biomesForTerrainChunk[x + z*supportedChunkSize].id}");
        
        return x + supportedChunkSize * z;
    }

    public static int3 to3D(int i, int chunkSize, int chunkHeight)
    {
        int3 xyz = new int3();
        xyz.x = i % chunkSize;
        xyz.y = (i / chunkSize) % chunkHeight;
        xyz.z = i / (chunkSize * chunkHeight);

        return xyz;
    }
}
    
public struct CubePoint
{
    public int3 p;
    public float val;
}

public struct Vertex
{
    public float3 position;
    public float4 color;
    
    //Constructor
    public Vertex(float3 position, float4 color)
    {
        this.position = position;
        this.color = color;
    }
}

[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct Edge : IEquatable<Edge>
{
    // point 1 and point 2 should be interchangeable
    public readonly int3 point1;
    public readonly int3 point2;
    public Edge(int3 item1, int3 item2) { point1 = item1; point2 = item2;}

    public bool Equals(Edge other)
    {
        return math.all(other.point1 == point1) && math.all(other.point2 == point2) || math.all(other.point1 == point2) && math.all(other.point2 == point1) ;
    }

    public override string ToString()
    {
        return "a0: " + point1.ToString() + ", b0: " + point2.ToString();
    }

    // Addition hashcodes so edge 2, 0 and edge 0, 2 are the same
    public override int GetHashCode()
    {
        int hash = point1.GetHashCode() + point2.GetHashCode();

        return hash;
    }
}
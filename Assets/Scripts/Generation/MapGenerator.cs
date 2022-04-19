using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class MapGenerator : MonoBehaviour
{
    [SerializeField] private GameObject cube;
    [SerializeField] private Material meshMaterial;
    [SerializeField] private GameObject waterPreview;
    public float waterLevel;
    private bool waterOn = true;
    
    public int seed;
    
    public const int chunkSize = 16;
    public static int supportedChunkSize => chunkSize + 2; 
    
    // Chunk representation 
    // 0  1  2  3  4  5  6  7  8  9 10 12 13 14 15 16 17
    // .  *  *  *  *  *  *  *  *  *  *  *  *  *  *  *  .
    // 
    
    public const int chunkHeight = 128;

    private const float threshold = 0;
    

    private VanillaFunction vanillaFunction;
    NativeArray<int> cornerIndexAFromEdge;
    NativeArray<int> cornerIndexBFromEdge;
    NativeArray<int> triangulation1D;
    public struct CubePoint
    {
        public float3 p;
        public float val;
    }
    

    public struct Triangle
    {
        public float3 a;
        public float3 b;
        public float3 c;

        public float3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }

        public Triangle(float3 a, float3 b, float3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }
    
    private void Awake()
    {
        vanillaFunction = new VanillaFunction(seed);
        
        cornerIndexAFromEdge = new NativeArray<int>(12, Allocator.Persistent);
        cornerIndexAFromEdge.CopyFrom(MarchTable.cornerIndexAFromEdge);
        cornerIndexBFromEdge = new NativeArray<int>(12, Allocator.Persistent);
        cornerIndexBFromEdge.CopyFrom(MarchTable.cornerIndexBFromEdge);
        triangulation1D = new NativeArray<int>(4096, Allocator.Persistent);
        triangulation1D.CopyFrom(MarchTable.triangulation1D);
    }

    private void OnDisable()
    {
        cornerIndexAFromEdge.Dispose();
        cornerIndexBFromEdge.Dispose();
        triangulation1D.Dispose();
    }


    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MapDataJob : IJobParallelFor
    {
        public int supportedChunkSize;
        public int chunkHeight;
        public float offsetx;
        public float offsetz;
        [WriteOnly]
        public NativeArray<float> generatedMap;

        public VanillaFunction VanillaFunction;
        
        public void Execute(int idx)
        {
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % chunkHeight;
            int z = idx / (supportedChunkSize * chunkHeight);
            
            generatedMap[idx] = VanillaFunction.GetResult(x + offsetx, y, z + offsetz);
        }
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MarchCubeJob : IJobParallelFor
    {
        [Unity.Collections.ReadOnly]
        public NativeArray<float> map;
        [Unity.Collections.ReadOnly]
        public NativeArray<int> triangulation;
        [Unity.Collections.ReadOnly]
        public NativeArray<int> cornerIndexAFromEdge;
        [Unity.Collections.ReadOnly]
        public NativeArray<int> cornerIndexBFromEdge;
        [WriteOnly]
        public NativeQueue<Triangle>.ParallelWriter triangles;

        public void Execute(int index)
        {
            // Cube currently tested, in array so it's easier to follow
            NativeArray<CubePoint> marchCube = new NativeArray<CubePoint>(8, Allocator.Temp);

            int idx = index + 1;
            
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % chunkHeight;
            int z = idx / (supportedChunkSize * chunkHeight);
            
            if (x >= supportedChunkSize-2 || y >= chunkHeight-2 || z >= supportedChunkSize-2)
            {
                return;
            }

            #region cubeGridDefinition
            marchCube[0] = new CubePoint()
            {
                p = new float3(x, y, z),
                val = map[to1D(x, y, z)]
            };
            marchCube[1] = new CubePoint()
            {
                p = new float3(x + 1, y, z),
                val = map[to1D(x + 1, y, z)]
            };
            marchCube[2] = new CubePoint()
            {
                p = new float3(x + 1, y, z + 1),
                val = map[to1D(x + 1, y, z + 1)]
            };
            marchCube[3] = new CubePoint()
            {
                p = new float3(x, y, z + 1),
                val = map[to1D(x, y, z + 1)]
            };
            marchCube[4] = new CubePoint()
            {
                p = new float3(x, y + 1, z),
                val = map[to1D(x, y + 1, z)]
            };
            marchCube[5] = new CubePoint()
            {
                p = new float3(x + 1, y + 1, z),
                val = map[to1D(x + 1, y + 1, z)]
            };
            marchCube[6] = new CubePoint()
            {
                p = new float3(x + 1, y + 1, z + 1),
                val = map[to1D(x + 1, y + 1, z + 1)]
            };
            marchCube[7] = new CubePoint()
            {
                p = new float3(x, y + 1, z + 1),
                val = map[to1D(x, y + 1, z + 1)]
            };
            #endregion
            
            // From values of the cube corners, find the cube configuration index
            int cubeindex = 0;
            if (marchCube[0].val < threshold) cubeindex |= 1;
            if (marchCube[1].val < threshold) cubeindex |= 2;
            if (marchCube[2].val < threshold) cubeindex |= 4;
            if (marchCube[3].val < threshold) cubeindex |= 8;
            if (marchCube[4].val < threshold) cubeindex |= 16;
            if (marchCube[5].val < threshold) cubeindex |= 32;
            if (marchCube[6].val < threshold) cubeindex |= 64;
            if (marchCube[7].val < threshold) cubeindex |= 128;
            
            if (cubeindex == 255 || cubeindex == 0) 
                return;

            // From the cube configuration, add triangles & vertices to mesh 
            for (int i = 0; triangulation[cubeindex * 16 + i] != -1; i += 3)
            {
                // Get corners from edges
                int a0 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + i]];
                int b0 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + i]];
                
                int a1 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + (i+1)]];
                int b1 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + (i+1)]];
                
                int a2 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + (i+2)]];
                int b2 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + (i+2)]];
                
                // Find vertex position on edge & add them to vertices list
                float3 vert1 = FindVertexPos(threshold, marchCube[a0].p, marchCube[b0].p, marchCube[a0].val, marchCube[b0].val);
                float3 vert2 = FindVertexPos(threshold, marchCube[a1].p, marchCube[b1].p, marchCube[a1].val, marchCube[b1].val);
                float3 vert3 = FindVertexPos(threshold, marchCube[a2].p, marchCube[b2].p, marchCube[a2].val, marchCube[b2].val);
                
                // The point of the job is to fill this :
                triangles.Enqueue(new Triangle(vert1, vert2, vert3));
                // triangles.Add(new Triangle(vert1, vert2, vert3));
            }

            marchCube.Dispose();
        }
    }

    // TODO : Generate mesh in job
    // [BurstCompile(CompileSynchronously = true)]
    // public struct ChunkMeshJob : IJobParallelFor
    // {
    //     private NativeList<Triangle> triangles;
    //     private Mesh.MeshData meshData;
    //
    //     public void Execute(int idx)
    //     {
    //         NativeArray<float3> positions = meshData.GetVertexData<float3>();
    //         
    //     }
    // }
    
    private static float3 FindVertexPos(float threshold, float3 p1, float3 p2, float v1val, float v2val)
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
        
        float mu = (threshold - v1val) / (v2val - v1val);
        position.x = p1.x + mu * (p2.x - p1.x);
        position.y = p1.y + mu * (p2.y - p1.y);
        position.z = p1.z + mu * (p2.z - p1.z);
        
        return position;
    }
    
    
    public void CreateChunk(Vector2 position, GameObject chunkObject)
    {
        NativeArray<float> generatedMap = new NativeArray<float>(chunkHeight * supportedChunkSize * supportedChunkSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeQueue<Triangle> triangles = new NativeQueue<Triangle>(Allocator.Persistent);

        // Ask generator to get MapData with RequestMapData & start generating mesh with OnMapDataReceive
        var mapDataJob = new MapDataJob()
        {
            offsetx = position.x,
            offsetz = position.y,
            chunkHeight = chunkHeight,
            supportedChunkSize = supportedChunkSize,
            generatedMap = generatedMap,
            VanillaFunction = vanillaFunction
        };
        
        JobHandle mapDataHandle = mapDataJob.Schedule(generatedMap.Length, 4);
        
        var marchJob = new MarchCubeJob()
        {
            map = generatedMap,
            triangles = triangles.AsParallelWriter(),
            cornerIndexAFromEdge = cornerIndexAFromEdge,
            cornerIndexBFromEdge = cornerIndexBFromEdge,
            triangulation = triangulation1D
        };

        var marchHandle = marchJob.Schedule(generatedMap.Length, 4, mapDataHandle);
        // marchHandle.Complete();

        StartCoroutine(CreateChunkMesh(triangles, marchHandle, generatedMap, chunkObject));
    }

    private IEnumerator CreateChunkMesh(NativeQueue<Triangle> triangles, JobHandle job, NativeArray<float> generatedMap, GameObject chunkObject)
    {
        yield return new WaitUntil(() => job.IsCompleted);
        job.Complete();
        
        MeshFilter mf = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer mr = chunkObject.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[triangles.Count * 3];
        int[] tris = new int[triangles.Count * 3];
        int vertCount = 0;
        
        while (triangles.TryDequeue(out Triangle currentTriangle))
        {
            vertices[vertCount] = currentTriangle.a;
            vertices[vertCount+1] = currentTriangle.b;
            vertices[vertCount+2] = currentTriangle.c;

            tris[vertCount + 2] = vertCount;
            tris[vertCount + 1] = vertCount + 1;
            tris[vertCount] = vertCount + 2;
            
            vertCount += 3;
        }
        
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        mf.mesh = mesh;
        mr.material = meshMaterial;
        triangles.Dispose();
        generatedMap.Dispose();        
        chunkObject.AddComponent<MeshCollider>();


    }

    public static int to1D( int x, int y, int z)
    {
        return x + y*supportedChunkSize + z*supportedChunkSize*chunkHeight;
    }
}


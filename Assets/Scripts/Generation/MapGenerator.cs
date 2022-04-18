using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;


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
    
    private List<Mesh> meshes = new List<Mesh>();
 
    [SerializeField] private Gradient gradient;
    
    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();

    private VanillaFunction vanillaFunction;

    public struct CubePoint
    {
        public Pos3 p;
        public float val;
    }

    public struct Pos3
    {
        public float x;
        public float y;
        public float z;

        public Pos3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(Pos3 pos)
        {
            return new Vector3(pos.x, pos.y, pos.z);
        }
    }

    public struct Triangle
    {
        public Pos3 a;
        public Pos3 b;
        public Pos3 c;

        public Pos3 this [int i] {
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

        public Triangle(Pos3 a, Pos3 b, Pos3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }
    
    //Temp
    private void Awake()
    {
        vanillaFunction = new VanillaFunction(seed);
    }

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                lock (mapDataThreadInfoQueue)
                {
                    MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }
    }
    

    [BurstCompile(CompileSynchronously = true)]
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
            
            generatedMap[to1D(x,y,z)] = VanillaFunction.GetResult(x + offsetx, y, z + offsetz);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct MarchCubeJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> map;
        [ReadOnly]
        public NativeArray<int> triangulation;
        [ReadOnly]
        public NativeArray<int> cornerIndexAFromEdge;
        [ReadOnly]
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
                p = new Pos3(x, y, z),
                val = map[to1D(x, y, z)]
            };
            marchCube[1] = new CubePoint()
            {
                p = new Pos3(x + 1, y, z),
                val = map[to1D(x + 1, y, z)]
            };
            marchCube[2] = new CubePoint()
            {
                p = new Pos3(x + 1, y, z + 1),
                val = map[to1D(x + 1, y, z + 1)]
            };
            marchCube[3] = new CubePoint()
            {
                p = new Pos3(x, y, z + 1),
                val = map[to1D(x, y, z + 1)]
            };
            marchCube[4] = new CubePoint()
            {
                p = new Pos3(x, y + 1, z),
                val = map[to1D(x, y + 1, z)]
            };
            marchCube[5] = new CubePoint()
            {
                p = new Pos3(x + 1, y + 1, z),
                val = map[to1D(x + 1, y + 1, z)]
            };
            marchCube[6] = new CubePoint()
            {
                p = new Pos3(x + 1, y + 1, z + 1),
                val = map[to1D(x + 1, y + 1, z + 1)]
            };
            marchCube[7] = new CubePoint()
            {
                p = new Pos3(x, y + 1, z + 1),
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
                Pos3 vert1 = FindVertexPos(threshold, marchCube[a0].p, marchCube[b0].p, marchCube[a0].val, marchCube[b0].val);
                Pos3 vert2 = FindVertexPos(threshold, marchCube[a1].p, marchCube[b1].p, marchCube[a1].val, marchCube[b1].val);
                Pos3 vert3 = FindVertexPos(threshold, marchCube[a2].p, marchCube[b2].p, marchCube[a2].val, marchCube[b2].val);
                
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
    
    
    //  
    // // Called in thread from RequestMapData 
    // private MapData GenerateMapData(Vector2 offset)
    // {
    //     float[,,] finalMap = new float[supportedChunkSize, chunkHeight, supportedChunkSize];
    //     for (int x = 0; x < supportedChunkSize; x++)
    //     {
    //         for (int z = 0; z < supportedChunkSize; z++)
    //         {
    //             for (int y = 0; y < chunkHeight; y++)
    //             {
    //                 try
    //                 {
    //                     finalMap[x, y, z] = VanillaFunction.GetResult(x + offset.x, y, z + offset.y);
    //                 }
    //                 catch (Exception e)
    //                 {
    //                     Debug.LogError(e);
    //                     Debug.LogError($"x: {x}, y: {y}, z: {z}");
    //                 }
    //             }
    //         }
    //     }
    //
    //     return new MapData(finalMap);
    // }
    //
    // #region Mesh Handling
    //
    // // Called when TerrainChunk receives MapData with OnMapDataReceive
    // public List<CombineInstance> CreateMeshData(float[,,] map)
    // {
    //     List<CombineInstance> blockData = new List<CombineInstance>();
    //
    //     // GameObject blockMesh = Instantiate(cube, Vector3.zero, Quaternion.identity);
    //
    //     for (int x = 0; x < supportedChunkSize-2; x++)
    //     {
    //         for (int y = 0; y < chunkHeight-1; y++)
    //         {
    //             for (int z = 0; z < supportedChunkSize - 2; z++)
    //             {
    //                 int chunkBlockX = x + 1;
    //                 int chunkBlockZ = z + 1;
    //
    //                 // Marching cube !
    //                 
    //                 // Cube currently tested, in array so it's easier to follow
    //                 CubePoint[] cubeGrid = new CubePoint[8];
    //                 #region cubeGridDefinition
    //                 cubeGrid[0] = new CubePoint()
    //                 {
    //                     p = new Vector3(chunkBlockX, y, chunkBlockZ),
    //                     val = map[chunkBlockX, y, chunkBlockZ]
    //                 };
    //                 cubeGrid[1] = new CubePoint()
    //                 {
    //                     p = new Vector3(chunkBlockX + 1, y, chunkBlockZ),
    //                     val = map[chunkBlockX + 1, y, chunkBlockZ]
    //                 };
    //                 cubeGrid[2] = new CubePoint()
    //                 {
    //                     p = new Vector3(chunkBlockX + 1, y, chunkBlockZ + 1),
    //                     val = map[chunkBlockX + 1, y, chunkBlockZ + 1]
    //                 };
    //                 cubeGrid[3] = new CubePoint()
    //                 {
    //                     p = new Vector3(chunkBlockX, y, chunkBlockZ + 1),
    //                     val = map[chunkBlockX, y, chunkBlockZ + 1]
    //                 };
    //                 cubeGrid[4] = new CubePoint()
    //                 {
    //                     p = new Vector3(chunkBlockX, y + 1, chunkBlockZ),
    //                     val = map[chunkBlockX, y + 1, chunkBlockZ]
    //                 };
    //                 cubeGrid[5] = new CubePoint()
    //                 {
    //                     p = new Vector3(chunkBlockX + 1, y + 1, chunkBlockZ),
    //                     val = map[chunkBlockX + 1, y + 1, chunkBlockZ]
    //                 };
    //                 cubeGrid[6] = new CubePoint()
    //                 {
    //                     p = new Vector3(chunkBlockX + 1, y + 1, chunkBlockZ + 1),
    //                     val = map[chunkBlockX + 1, y + 1, chunkBlockZ + 1]
    //                 };
    //                 cubeGrid[7] = new CubePoint()
    //                 {
    //                     p = new Vector3(chunkBlockX, y + 1, chunkBlockZ + 1),
    //                     val = map[chunkBlockX, y + 1, chunkBlockZ + 1]
    //                 };
    //                 #endregion
    //                 
    //                 // From values of the cube corners, find the cube configuration index
    //                 int cubeindex = 0;
    //                 if (cubeGrid[0].val < threshold) cubeindex |= 1;
    //                 if (cubeGrid[1].val < threshold) cubeindex |= 2;
    //                 if (cubeGrid[2].val < threshold) cubeindex |= 4;
    //                 if (cubeGrid[3].val < threshold) cubeindex |= 8;
    //                 if (cubeGrid[4].val < threshold) cubeindex |= 16;
    //                 if (cubeGrid[5].val < threshold) cubeindex |= 32;
    //                 if (cubeGrid[6].val < threshold) cubeindex |= 64;
    //                 if (cubeGrid[7].val < threshold) cubeindex |= 128;
    //
    //                 if (cubeindex == 255 || cubeindex == 0)
    //                     continue;
    //
    //                 /*
    //                 if (MarchTable.edges[cubeindex] == 0)
    //                     continue;
    //                 
    //                 if ((MarchTable.edges[cubeindex] & 1) == 1)
    //                 {
    //                     vertlist[0] = FindVertexPos(threshold, cubeGrid[0].p, cubeGrid[1].p, cubeGrid[0].val, cubeGrid[1].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 2) == 2)
    //                 {
    //                     vertlist[1] = FindVertexPos(threshold, cubeGrid[1].p, cubeGrid[2].p, cubeGrid[1].val, cubeGrid[2].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 4) == 4)
    //                 {
    //                     vertlist[2] = FindVertexPos(threshold, cubeGrid[2].p, cubeGrid[3].p, cubeGrid[2].val, cubeGrid[3].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 8) == 8)
    //                 {
    //                     vertlist[3] = FindVertexPos(threshold, cubeGrid[3].p, cubeGrid[0].p, cubeGrid[3].val, cubeGrid[0].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 16) == 16)
    //                 {
    //                     vertlist[4] = FindVertexPos(threshold, cubeGrid[4].p, cubeGrid[5].p, cubeGrid[4].val, cubeGrid[5].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 32) == 32)
    //                 {
    //                     vertlist[5] = FindVertexPos(threshold, cubeGrid[5].p, cubeGrid[6].p, cubeGrid[5].val, cubeGrid[6].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 64) == 64)
    //                 {
    //                     vertlist[6] = FindVertexPos(threshold, cubeGrid[6].p, cubeGrid[7].p, cubeGrid[6].val, cubeGrid[7].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 128) == 128)
    //                 {
    //                     vertlist[7] = FindVertexPos(threshold, cubeGrid[7].p, cubeGrid[4].p, cubeGrid[7].val, cubeGrid[4].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 256) == 256)
    //                 {
    //                     vertlist[8] = FindVertexPos(threshold, cubeGrid[0].p, cubeGrid[4].p, cubeGrid[0].val, cubeGrid[4].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 512) == 512)
    //                 {
    //                     vertlist[9] = FindVertexPos(threshold, cubeGrid[1].p, cubeGrid[5].p, cubeGrid[1].val, cubeGrid[5].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 1024) == 1024)
    //                 {
    //                     vertlist[10] = FindVertexPos(threshold, cubeGrid[2].p, cubeGrid[6].p, cubeGrid[2].val, cubeGrid[6].val);
    //                 }
    //                 if ((MarchTable.edges[cubeindex] & 2048) == 2048)
    //                 {
    //                     vertlist[11] = FindVertexPos(threshold, cubeGrid[3].p, cubeGrid[7].p, cubeGrid[3].val, cubeGrid[7].val);
    //                 }
    //                 */
    //
    //                 // From the cube configuration, add triangles & vertices to mesh 
    //                 List<Vector3> vertices = new List<Vector3>();
    //                 List<int> triangles = new List<int>();
    //                 for (int i = 0; MarchTable.triangulation[cubeindex,i] != -1; i += 3)
    //                 {
    //                     // Get corners from edges
    //                     int a0 = MarchTable.cornerIndexAFromEdge[MarchTable.triangulation[cubeindex,i]];
    //                     int b0 = MarchTable.cornerIndexBFromEdge[MarchTable.triangulation[cubeindex,i]];
    //
    //                     int a1 = MarchTable.cornerIndexAFromEdge[MarchTable.triangulation[cubeindex,i+1]];
    //                     int b1 = MarchTable.cornerIndexBFromEdge[MarchTable.triangulation[cubeindex,i+1]];
    //
    //                     int a2 = MarchTable.cornerIndexAFromEdge[MarchTable.triangulation[cubeindex,i+2]];
    //                     int b2 = MarchTable.cornerIndexBFromEdge[MarchTable.triangulation[cubeindex,i+2]];
    //                     
    //                     // Find vertex position on edge & add them to vertices list
    //                     Vector3 vert1 = FindVertexPos(threshold, cubeGrid[a0].p, cubeGrid[b0].p, cubeGrid[a0].val, cubeGrid[b0].val);
    //                     Vector3 vert2 = FindVertexPos(threshold, cubeGrid[a1].p, cubeGrid[b1].p, cubeGrid[a1].val, cubeGrid[b1].val);
    //                     Vector3 vert3 = FindVertexPos(threshold, cubeGrid[a2].p, cubeGrid[b2].p, cubeGrid[a2].val, cubeGrid[b2].val);
    //
    //                     vertices.Add(vert1);
    //                     vertices.Add(vert2);
    //                     vertices.Add(vert3);
    //
    //                     // Add new vertices index to triangles array
    //                     triangles.Add(vertices.Count-1);
    //                     triangles.Add(vertices.Count-2);
    //                     triangles.Add(vertices.Count-3);
    //                 }
    //                 
    //                 Mesh mesh = new Mesh();
    //                 
    //                 mesh.SetVertices(vertices.ToArray());
    //                 mesh.SetTriangles(triangles.ToArray(), 0);
    //
    //                 CombineInstance ci = new CombineInstance()
    //                 {
    //                     mesh = mesh,
    //                     // Transform is important here
    //                     transform = Matrix4x4.identity
    //                 };
    //                 
    //                 blockData.Add(ci);
    //             }
    //         }
    //     }
    //     
    //     return blockData;
    // }

    private static Pos3 FindVertexPos(float threshold, Pos3 p1, Pos3 p2, float v1val, float v2val)
    {
        Pos3 position = new Pos3(0,0,0);

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
        
        float mu = (threshold - v1val) / (v2val - v1val);
        position.x = p1.x + mu * (p2.x - p1.x);
        position.y = p1.y + mu * (p2.y - p1.y);
        position.z = p1.z + mu * (p2.z - p1.z);
        
        return position;
    }

    public void CreateChunk(Vector2 position, GameObject chunkObject)
    {
        NativeArray<float> generatedMap = new NativeArray<float>(chunkHeight * supportedChunkSize * supportedChunkSize, Allocator.TempJob);
        NativeQueue<Triangle> triangles = new NativeQueue<Triangle>(Allocator.TempJob);

        NativeArray<int> cornerIndexAFromEdge = new NativeArray<int>(12, Allocator.TempJob);
        cornerIndexAFromEdge.CopyFrom(MarchTable.cornerIndexAFromEdge);
        NativeArray<int> cornerIndexBFromEdge = new NativeArray<int>(12, Allocator.TempJob);
        cornerIndexBFromEdge.CopyFrom(MarchTable.cornerIndexBFromEdge);
        NativeArray<int> triangulation1D = new NativeArray<int>(4096, Allocator.TempJob);
        triangulation1D.CopyFrom(MarchTable.triangulation1D);
        
        
        
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
        
        JobHandle mapDataHandle = mapDataJob.Schedule(generatedMap.Length, 1);
        
        var marchJob = new MarchCubeJob()
        {
            map = generatedMap,
            triangles = triangles.AsParallelWriter(),
            cornerIndexAFromEdge = cornerIndexAFromEdge,
            cornerIndexBFromEdge = cornerIndexBFromEdge,
            triangulation = triangulation1D
        };

        var marchHandle = marchJob.Schedule(generatedMap.Length, 5, mapDataHandle);
        
        marchHandle.Complete();

        StartCoroutine(CreateChunkMesh(triangles, marchHandle, generatedMap, chunkObject));
        cornerIndexAFromEdge.Dispose();
        cornerIndexBFromEdge.Dispose();
        triangulation1D.Dispose();
    }

    private IEnumerator CreateChunkMesh(NativeQueue<Triangle> triangles, JobHandle job, NativeArray<float> generatedMap, GameObject chunkObject)
    {
        yield return new WaitUntil(() => job.IsCompleted);
        
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

        IsMeshOk(mesh);
        mf.mesh = mesh;
        mr.material = meshMaterial;
        triangles.Dispose();
        generatedMap.Dispose();

    }

    public static bool IsMeshOk(Mesh m)
    {
        foreach (Vector3 v in m.vertices)
        {
            if (!float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z))
            {
                Debug.Log($"{v}");
                return false;
            }
        }
        return true;
    }

    // Called by TerrainChunk after CreateMeshData
    // public List<List<CombineInstance>> SeparateMeshData(List<CombineInstance> blockData)
    // {
    //     
    //     List<List<CombineInstance>> blockDataLists = new List<List<CombineInstance>>();
    //     int vertexCount = 0;
    //     blockDataLists.Add(new List<CombineInstance>());
    //
    //     for (int i = 0; i < blockData.Count; i++)
    //     {
    //         vertexCount += blockData[i].mesh.vertexCount;
    //         if (vertexCount > 65536)
    //         {
    //             vertexCount = 0;
    //             blockDataLists.Add(new List<CombineInstance>());
    //             i--;
    //         }
    //         else
    //         {
    //             blockDataLists.Last().Add(blockData[i]);
    //         }
    //     }
    //
    //     return blockDataLists;
    // }

    // Called by TerrainChunk after SeparateMeshData
    public void CreateMesh(Mesh mesh, GameObject g, Transform parent) 
    {
        g.transform.parent = parent;
        g.transform.localPosition = Vector3.zero;
        MeshFilter mf = g.AddComponent<MeshFilter>();
        MeshRenderer mr = g.AddComponent<MeshRenderer>();
        mr.sharedMaterial = meshMaterial;
        mf.mesh = mesh;
            
        mf.mesh.RecalculateNormals();
            
        // foreach (var vertpos in mf.mesh.vertices)  
        // {
        //     Debug.Log($"{vertpos}");
        //     Instantiate(cube, vertpos, Quaternion.identity);
        // }
    
        meshes.Add(mf.mesh);
    }

    // #endregion


    public static int to1D( int x, int y, int z)
    {
        return x + y*supportedChunkSize + z*supportedChunkSize*chunkHeight;
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public struct MapData
{
    public float[,,] noiseMap;

    public MapData(float[,,] noiseMap)
    {
        this.noiseMap = noiseMap;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DefaultNamespace;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Random = UnityEngine.Random;


public class MapGenerator : MonoBehaviour
{
    [SerializeField] private GameObject cube;
    [SerializeField] private Material meshMaterial;
    [SerializeField] private GameObject waterPreview;
    public float waterLevel;
    private bool waterOn = true;
    
    public int seed;
    
    public const int chunkSize = 16;
    public static int supportedChunkSize => chunkSize + 3; 
    
    // Chunk representation 
    // 0   1   2   3   4   5   6   7   8   9  10  12  13  14  15  16  17  18
    // .   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   .
    //     .-1-.-3-.-4-.-5-.-6-.-7-.-8-.-9-.-10.-11.-12.-13.-14.-15.-16.
    // We have 19 dots total
    // for 17 dots as chunk
    // so we have 16 blocks per chunk, hence the + 3
    
    public const int chunkHeight = 128;

    private const float threshold = 0;

    private VanillaFunction vanillaFunction;
    NativeArray<int> cornerIndexAFromEdge;
    NativeArray<int> cornerIndexBFromEdge;
    NativeArray<int> triangulation1D;
    public struct CubePoint
    {
        public int3 p;
        public float val;
    }
    

    public struct Triangle
    {
        public int3 vertexIndexA;
        public int3 vertexIndexB;
        public int3 vertexIndexC;
        
        public int3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return vertexIndexA;
                    case 1:
                        return vertexIndexB;
                    default:
                        return vertexIndexC;
                }
            }
        }

        public Triangle(int3 a, int3 b, int3 c)
        {
            this.vertexIndexA = a;
            this.vertexIndexB = b;
            this.vertexIndexC = c;
        }
    }
    
    private void Awake()
    {
        // Temporary, initialize terrain function with seed
        vanillaFunction = new VanillaFunction(seed);
        
        // Initialize march table as NativeArray for jobs
        cornerIndexAFromEdge = new NativeArray<int>(12, Allocator.Persistent);
        cornerIndexAFromEdge.CopyFrom(MarchTable.cornerIndexAFromEdge);
        cornerIndexBFromEdge = new NativeArray<int>(12, Allocator.Persistent);
        cornerIndexBFromEdge.CopyFrom(MarchTable.cornerIndexBFromEdge);
        triangulation1D = new NativeArray<int>(4096, Allocator.Persistent);
        triangulation1D.CopyFrom(MarchTable.triangulation1D);
    }
    
    private void OnDisable()
    {
        // Dispose manually March table's NativeArray 
        cornerIndexAFromEdge.Dispose();
        cornerIndexBFromEdge.Dispose();
        triangulation1D.Dispose();
    }

    // First job to be called from CreateChunk to generate MapData 
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MapDataJob : IJobParallelFor
    {
        // Input data
        public int supportedChunkSize;
        public int chunkHeight;
        public float offsetx;
        public float offsetz;
        // Output data
        public NativeArray<float> generatedMap;

        // Function reference
        public VanillaFunction VanillaFunction;
        
        public void Execute(int idx)
        {
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % chunkHeight;
            int z = idx / (supportedChunkSize * chunkHeight);

            // Since it's a IJobParallelFor, job is called for every idx, filling the generatedMap in parallel
            generatedMap[idx] = VanillaFunction.GetResult(x + (offsetx), y, z + (offsetz));
        }
    }
    
    // Second job to be called from CreateChunk after map data is generated
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
        [WriteOnly]
        public NativeHashMap<int3, float3>.ParallelWriter vertices;
        
        // job is IJobParallelFor, one index = 1 cube tested
        public void Execute(int idx)
        {
            // Cube currently tested, in array so it's easier to follow
            NativeArray<CubePoint> marchCube = new NativeArray<CubePoint>(8, Allocator.Temp);
            
            // Remember :
            // Map data generates 18x18 (ignoring height) data to calculate normals
            // Marching will output a vertices array of 18x18 to allow mesh generation to calculate normals
            // but the mesh generation should output a 16x16 vertices array
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % chunkHeight;
            int z = idx / (supportedChunkSize * chunkHeight);

            // Don't calculate when we are too close to the edge of the chunk to prevent out of bound
            if (x >= supportedChunkSize-1 || y >= chunkHeight-1 || z >= supportedChunkSize-1)
            {
                return;
            }

            // put march cube in array to have cleaner code after
            #region March cube definition 
            marchCube[0] = new CubePoint()
            {
                p = new int3(x, y, z),
                val = map[to1D(x, y, z)]
            };
            marchCube[1] = new CubePoint()
            {
                p = new int3(x + 1, y, z),
                val = map[to1D(x + 1, y, z)]
            };
            marchCube[2] = new CubePoint()
            {
                p = new int3(x + 1, y, z + 1),
                val = map[to1D(x + 1, y, z + 1)]
            };
            marchCube[3] = new CubePoint()
            {
                p = new int3(x, y, z + 1),
                val = map[to1D(x, y, z + 1)]
            };
            marchCube[4] = new CubePoint()
            {
                p = new int3(x, y + 1, z),
                val = map[to1D(x, y + 1, z)]
            };
            marchCube[5] = new CubePoint()
            {
                p = new int3(x + 1, y + 1, z),
                val = map[to1D(x + 1, y + 1, z)]
            };
            marchCube[6] = new CubePoint()
            {
                p = new int3(x + 1, y + 1, z + 1),
                val = map[to1D(x + 1, y + 1, z + 1)]
            };
            marchCube[7] = new CubePoint()
            {
                p = new int3(x, y + 1, z + 1),
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
                float3 vert0 = FindVertexPos(threshold, marchCube[a0].p, marchCube[b0].p, marchCube[a0].val, marchCube[b0].val);
                float3 vert1 = FindVertexPos(threshold, marchCube[a1].p, marchCube[b1].p, marchCube[a1].val, marchCube[b1].val);
                float3 vert2 = FindVertexPos(threshold, marchCube[a2].p, marchCube[b2].p, marchCube[a2].val, marchCube[b2].val);

                // offset every vert by -1 for some reason idk, otherwise the chunk isn't really in the middle of its supposed bounds
                // probably because of supportedChunksize offset
                vert0.xz--;
                vert1.xz--;
                vert2.xz--;
                
                // round vert position for hashmap index
                // this is to avoid having 2 vertices really close to produce smooth terrain
                int3 roundedVert0 = math.int3(math.round(vert0 * 100f));
                int3 roundedVert1 = math.int3(math.round(vert1 * 100f));
                int3 roundedVert2 = math.int3(math.round(vert2 * 100f));

                // try add vertices to hashmap with rounded value as key
                // if we already have a close vertex in the hashmap (because the rounded vertex is the same)
                // then it won't be added
                vertices.TryAdd(roundedVert0, vert0);
                vertices.TryAdd(roundedVert1, vert1);
                vertices.TryAdd(roundedVert2, vert2);

                // Triangle only holds reference to vertices array (int3)
                Triangle triangle = new Triangle()
                {
                    vertexIndexA = roundedVert0, 
                    vertexIndexB = roundedVert1,
                    vertexIndexC = roundedVert2
                };
                
                // Triangles are stored in a queue because we don't know how many triangles we will get & we can write in parallel easily in a queue
                triangles.Enqueue(triangle);
            }

            marchCube.Dispose();
        }
    }

    // Third & last job called from CreateChunk, generate mesh for marching cube data
    [BurstCompile(FloatPrecision.High, FloatMode.Fast,CompileSynchronously = true)]
    public struct ChunkMeshJob : IJob
    {
        // Input reference
        public NativeQueue<Triangle> triangles;
        public NativeHashMap<int3, float3> uniqueVertices;
        public Bounds bounds;
        // Output
        public Mesh.MeshDataArray meshDataArray;
        
        public void Execute()
        {
            // Initialize meshData
            Mesh.MeshData meshData = meshDataArray[0];
            
            #region initialize meshData
            int vertexAttributeCount = 4;
            int initVertexCount = triangles.Count * 3;
            int triangleIndexCount = triangles.Count * 3;
            
            // Attributes
            var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
                vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
            vertexAttributes[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3, stream: 1
            );
            vertexAttributes[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4, stream: 2
            );
            vertexAttributes[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2, stream: 3
            );
            #endregion
            
            // Creating vertices & triangles arrays
            
            // Since it's not possible to use float as keys in a hashmap,
            // Instead we make a hashmap that stores the indices we already added in the vertices array & where we stored it
            // so if a new triangle references an already placed vertex, we know where it is already in the array
            NativeHashMap<int3, int> matchingIndices = new NativeHashMap<int3, int>(initVertexCount, Allocator.Temp);
            // vertices & triangles arrays 
            NativeList<int> finalTriangleIndices = new NativeList<int>(triangles.Count * 3, Allocator.Temp);
            NativeList<float3> finalVertices = new NativeList<float3>(initVertexCount, Allocator.Temp);
            NativeList<float3> borderVertices = new NativeList<float3>((chunkSize * chunkHeight) * 4 + chunkHeight * 4, Allocator.Temp);
            NativeList<int> borderTriangles = new NativeList<int>(triangles.Count * 3, Allocator.Temp);
            // Transform the hashmap of int3, float3 to an array of vertices (int, float3)
            // For every triangle
            while (triangles.TryDequeue(out Triangle triangle))
            {
                bool isVertexABorder = triangle.vertexIndexA.x < 0 || triangle.vertexIndexA.z < 0 || triangle.vertexIndexA.x > chunkSize * 100 || triangle.vertexIndexA.z > chunkSize * 100;
                bool isVertexBBorder = triangle.vertexIndexB.x < 0 || triangle.vertexIndexB.z < 0 || triangle.vertexIndexB.x > chunkSize * 100 || triangle.vertexIndexB.z > chunkSize * 100;
                bool isVertexCBorder = triangle.vertexIndexC.x < 0 || triangle.vertexIndexC.z < 0 || triangle.vertexIndexC.x > chunkSize * 100 || triangle.vertexIndexC.z > chunkSize * 100;

                if (isVertexABorder || isVertexBBorder || isVertexCBorder)
                {
                    continue;
                } 

                
                // Same as above for vertex C of triangle
                if (matchingIndices.TryAdd(triangle.vertexIndexC, finalVertices.Length))
                {
                    finalVertices.Add(uniqueVertices[triangle.vertexIndexC]);
                    finalTriangleIndices.Add(finalVertices.Length-1);
                }
                else
                {
                    finalTriangleIndices.Add(matchingIndices[triangle.vertexIndexC]);
                }
                


                // Same as above for vertex B of triangle
                if (matchingIndices.TryAdd(triangle.vertexIndexB, finalVertices.Length))
                {
                    finalVertices.Add(uniqueVertices[triangle.vertexIndexB]);
                    finalTriangleIndices.Add(finalVertices.Length-1);
                }
                else
                {
                    finalTriangleIndices.Add(matchingIndices[triangle.vertexIndexB]);
                }
                
                

                // try to see if we already added the vertex 
                if (matchingIndices.TryAdd(triangle.vertexIndexA, finalVertices.Length))
                {
                    // If we didn't already added the vertex we save that we did, and where in the array we added it

                    // add it to vertices array
                    finalVertices.Add(uniqueVertices[triangle.vertexIndexA]);
                    // vertex index is the current last position (we are not writing in parallel in the array so it's ok)
                    finalTriangleIndices.Add(finalVertices.Length-1);
                }
                else
                {
                    // If we already added the vertex in the vertices array
                    // we only need to add the vertex index to the triangles array
                    finalTriangleIndices.Add(matchingIndices[triangle.vertexIndexA]);
                }
            }

            // Apply calculated array to mesh data arrays
            // we don't know in advance the vertices array length, so it's not possible to directly modify the
            // meshData vertices array
            meshData.SetVertexBufferParams(finalVertices.Length, vertexAttributes);
            meshData.SetIndexBufferParams(finalTriangleIndices.Length, IndexFormat.UInt32);
            vertexAttributes.Dispose();

            NativeArray<float3> meshDataVertices = meshData.GetVertexData<float3>();
            NativeArray<int> triangleIndices = meshData.GetIndexData<int>();
            
            finalVertices.AsArray().CopyTo(meshDataVertices);
            finalTriangleIndices.AsArray().CopyTo(triangleIndices);
            
            
            // Normal calculation
            NativeArray<float3> normals = meshData.GetVertexData<float3>(1);

            for (int i = 0; i < meshDataVertices.Length; i++)
            {
                // normals needs to be set to zero before calculating them
                normals[i] = float3.zero;
            }

            CalculateNormals(meshDataVertices, borderVertices, triangleIndices, normals);
            
            SetBasicUVs( meshDataVertices, meshData.GetVertexData<float2>(3));
            
            // Finalize meshData
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, finalTriangleIndices.Length)
            {
                bounds = bounds,
                vertexCount = finalVertices.Length
            }, MeshUpdateFlags.DontRecalculateBounds);
            
            /////////////////////////////////
            matchingIndices.Dispose();
            finalVertices.Dispose();
            finalTriangleIndices.Dispose();
            borderTriangles.Dispose();
            borderVertices.Dispose();
        }

        private void CalculateNormals(NativeArray<float3> vertices, NativeArray<float3> borderVertices, NativeArray<int> triangles, NativeArray<float3> normals)
        {
            int triangleCount = triangles.Length / 3;
            for (int i = 0; i < triangleCount; i++)
            {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = triangles[normalTriangleIndex];
                int vertexIndexB = triangles[normalTriangleIndex+1];
                int vertexIndexC = triangles[normalTriangleIndex+2];

                float3 triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC, vertices);
                // normals are set to 0 beforehand to avoid unpredictable issues
                normals[vertexIndexA] += triangleNormal;
                normals[vertexIndexB] += triangleNormal;
                normals[vertexIndexC] += triangleNormal;
            }
            
            for (int i = 0; i < normals.Length; i++)
            {
                math.normalize(normals[i]);
            }
        }

        float3 SurfaceNormal(int indexA, int indexB, int indexC, NativeArray<float3> vertices)
        {
            float3 pointA = vertices[indexA];
            float3 pointB = vertices[indexB];
            float3 pointC = vertices[indexC];
            
            float3 sideAB = pointB - pointA;
            float3 sideAC = pointC - pointA;
            return math.cross(sideAB, sideAC);
        }

        private void SetBasicUVs(NativeArray<float3> vertices, NativeArray<float2> uvs)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = vertices[i].xz;
            }
        }
    }
    
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
        // Generate mesh object with its components
        Bounds bounds = new Bounds(new Vector3(chunkSize / 2, chunkHeight / 2, chunkSize / 2), new Vector3(chunkSize, chunkHeight, chunkSize));
        Debug.Log("new bounds with center " + bounds.center + " and size " + bounds.size);
        MeshFilter mf = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer mr = chunkObject.AddComponent<MeshRenderer>();
        MeshCollider mc = chunkObject.AddComponent<MeshCollider>();
        chunkObject.AddComponent<BoundGizmo>();
            Mesh mesh = new Mesh()
        {
            bounds = bounds
        };
        mf.sharedMesh = mesh;
        mr.material = meshMaterial;

        
        // Variables that will filled & passed along jobs
        NativeArray<float> generatedMap = new NativeArray<float>(chunkHeight * supportedChunkSize * supportedChunkSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeQueue<Triangle> triangles = new NativeQueue<Triangle>(Allocator.Persistent);
        NativeHashMap<int3, float3> vertices = new NativeHashMap<int3, float3>((chunkHeight * supportedChunkSize * supportedChunkSize), Allocator.Persistent);
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);

        // Job to generate input map data for marching cube
        var mapDataJob = new MapDataJob()
        {
            // Inputs
            offsetx = position.x,
            offsetz = position.y,
            chunkHeight = chunkHeight,
            supportedChunkSize = supportedChunkSize,
            VanillaFunction = vanillaFunction,
            // Output data
            generatedMap = generatedMap
        };
        
        JobHandle mapDataHandle = mapDataJob.Schedule(generatedMap.Length, 4);
        
        // Start marching cube based on a map data
        var marchJob = new MarchCubeJob()
        {
            // Input data
            map = generatedMap,
            // March table 
            cornerIndexAFromEdge = cornerIndexAFromEdge,
            cornerIndexBFromEdge = cornerIndexBFromEdge,
            triangulation = triangulation1D,
            // Arrays to fill with this job
            triangles = triangles.AsParallelWriter(),
            vertices = vertices.AsParallelWriter()
        };

        JobHandle marchHandle = marchJob.Schedule(generatedMap.Length, 4, mapDataHandle);

        // Generate Mesh from Marching cube result
        var chunkMeshJob = new ChunkMeshJob()
        {
            triangles = triangles,
            uniqueVertices = vertices,
            bounds = bounds,
            meshDataArray = meshDataArray
        };

        JobHandle chunkMeshHandle = chunkMeshJob.Schedule(marchHandle);


        StartCoroutine(ApplyMeshData(position, meshDataArray, mesh, mc, vertices, triangles, generatedMap, chunkMeshHandle));
        
    }

    private IEnumerator ApplyMeshData(Vector2 pos, Mesh.MeshDataArray meshDataArray, Mesh mesh, MeshCollider mc, NativeHashMap<int3, float3> vertices, NativeQueue<Triangle> triangles, NativeArray<float> mapData, JobHandle job)
    {
        yield return new WaitUntil(() => job.IsCompleted);
        job.Complete();

        // DebugChunks(vertices, pos);
        
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.RecalculateTangents();
        mc.sharedMesh = mesh;
        triangles.Dispose();
        mapData.Dispose();
        vertices.Dispose();

    }
    
    
    // Call to visualize vertices positions
    public void DebugChunks(NativeHashMap<int3, float3> vertices,Vector2 offset)
    {
        var g = new GameObject("Info of " + offset.x + " " + offset.y);
        
        for (int i = 0; i < vertices.GetKeyValueArrays(Allocator.Persistent).Keys.Length; i++)
        {
            var vertex = vertices.GetKeyValueArrays(Allocator.Persistent).Values[i];
            var vertexIndex = vertices.GetKeyValueArrays(Allocator.Persistent).Keys[i];
        
            var tmp = new GameObject();
            var tmpc = tmp.AddComponent<TextMeshPro>();
            tmpc.text = vertexIndex.ToString();
            tmpc.fontSize = 0.5f;
            tmpc.alignment = TextAlignmentOptions.Midline;
            
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.transform.position = new Vector3(vertex.x + offset.x, vertex.y, offset.y + vertex.z);
            c.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            c.transform.parent = g.transform;
            tmp.transform.parent = c.transform;
            tmp.transform.localPosition = new Vector3(0, 0.5f, 0);
        }
    }
    
    public static int to1D( int x, int y, int z)
    {
        return x + y*supportedChunkSize + z*supportedChunkSize*chunkHeight;
    }
    
    public static int to1D(int3 xyz)
    {
        return xyz.x + xyz.y*supportedChunkSize + xyz.z*supportedChunkSize*chunkHeight;
    }
}


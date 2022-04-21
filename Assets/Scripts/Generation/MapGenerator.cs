using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;


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
        public int3 p;
        public float val;
    }
    

    public struct Triangle
    {
        public int2 vertexIndexA;
        public int2 vertexIndexB;
        public int2 vertexIndexC;
        
        public int2 this [int i] {
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

        public Triangle(int2 a, int2 b, int2 c)
        {
            this.vertexIndexA = a;
            this.vertexIndexB = b;
            this.vertexIndexC = c;
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


    // [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MapDataJob : IJobParallelFor
    {
        public int supportedChunkSize;
        public int chunkHeight;
        public float offsetx;
        public float offsetz;
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
        // [WriteOnly]
        public NativeQueue<Triangle>.ParallelWriter triangles;
        public NativeHashMap<int2, float3>.ParallelWriter vertices;
        
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
                Triangle triangle = new Triangle();
                
                // Get corners from edges
                int a0 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + i]];
                int b0 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + i]];
                int2 cornersPosVert0 = new int2(to1D(marchCube[a0].p), to1D(marchCube[b0].p));
                
                int a1 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + (i+1)]];
                int b1 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + (i+1)]];
                int2 cornersPosVert1 = new int2(to1D(marchCube[a1].p), to1D(marchCube[b1].p));
                
                int a2 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + (i+2)]];
                int b2 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + (i+2)]];
                int2 cornersPosVert2 = new int2(to1D(marchCube[a2].p), to1D(marchCube[b2].p));

                // Find vertex position on edge & add them to vertices list

                float3 vert0 = FindVertexPos(threshold, marchCube[a0].p, marchCube[b0].p, marchCube[a0].val, marchCube[b0].val);
                float3 vert1 = FindVertexPos(threshold, marchCube[a1].p, marchCube[b1].p, marchCube[a1].val, marchCube[b1].val);
                float3 vert2 = FindVertexPos(threshold, marchCube[a2].p, marchCube[b2].p, marchCube[a2].val, marchCube[b2].val);

                vertices.TryAdd(cornersPosVert0, vert0);
                triangle.vertexIndexA = cornersPosVert0;
                vertices.TryAdd(cornersPosVert1, vert1);
                triangle.vertexIndexB = cornersPosVert1;
                vertices.TryAdd(cornersPosVert2, vert2);
                triangle.vertexIndexC = cornersPosVert2;
                triangles.Enqueue(triangle);
            }

            marchCube.Dispose();
        }
    }

    [BurstCompile(FloatPrecision.High, FloatMode.Fast,CompileSynchronously = true)]
    public struct ChunkMeshJob : IJob
    {
        public NativeQueue<Triangle> triangles;
        public NativeHashMap<int2, float3> uniqueVertices;
        public Mesh.MeshDataArray meshDataArray;
        public Bounds bounds;
        
        public void Execute()
        {
            // string vertstr = "";
            // var vertkeyval = vertices.GetKeyValueArrays(Allocator.Temp);
            // for (int i = 0; i < vertkeyval.Length; i++)
            // {
            //     if (vertkeyval.Values[i].x == 0 && vertkeyval.Values[i].y == 0 && vertkeyval.Values[i].z == 0)
            //     {
            //         vertstr += "(" + vertkeyval.Keys[i] + " " + vertkeyval.Values[i] + "), ";
            //     }
            // }
            // Debug.Log(vertstr);

            int vertexAttributeCount = 2;
            int initVertexCount = triangles.Count * 3;
            int triangleIndexCount = triangles.Count * 3;
            
            Mesh.MeshData meshData = meshDataArray[0];

            // Attributes
            var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
                vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
            vertexAttributes[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3, stream: 1
            );
            // vertexAttributes[2] = new VertexAttributeDescriptor(
            //     VertexAttribute.Tangent, dimension: 4, stream: 2
            // );
            // vertexAttributes[3] = new VertexAttributeDescriptor(
            //     VertexAttribute.TexCoord0, dimension: 2, stream: 3
            // );
            
            
            // Finding triangles & vertices array
            NativeHashMap<int2, int> matchingIndices = new NativeHashMap<int2, int>(initVertexCount, Allocator.Temp);
            
            NativeArray<int> finalTriangleIndices = new NativeArray<int>(triangles.Count * 3, Allocator.Temp);
            NativeList<float3> finalVertices = new NativeList<float3>(initVertexCount, Allocator.Temp);
            
            
            int iterationCount = 0;
            while (triangles.TryDequeue(out Triangle triangle))
            {
                if (matchingIndices.TryAdd(triangle.vertexIndexA, finalVertices.Length))
                {
                    finalVertices.Add(uniqueVertices[triangle.vertexIndexA]);
                    finalTriangleIndices[iterationCount+2] = finalVertices.Length-1;
                }
                else
                {
                    finalTriangleIndices[iterationCount+2] = matchingIndices[triangle.vertexIndexA];
                }
                
                if (matchingIndices.TryAdd(triangle.vertexIndexB, finalVertices.Length))
                {
                    finalVertices.Add(uniqueVertices[triangle.vertexIndexB]);
                    finalTriangleIndices[iterationCount+1] = finalVertices.Length-1;
                }
                else
                {
                    finalTriangleIndices[iterationCount+1] = matchingIndices[triangle.vertexIndexB];
                }
                
                if (matchingIndices.TryAdd(triangle.vertexIndexC, finalVertices.Length))
                {
                    finalVertices.Add(uniqueVertices[triangle.vertexIndexC]);
                    finalTriangleIndices[iterationCount] = finalVertices.Length-1;
                }
                else
                {
                    finalTriangleIndices[iterationCount] = matchingIndices[triangle.vertexIndexC];
                }

                // verticesForMesh.Add(vertices[triangle.vertexIndexA]);
                // if (matchingIndices.TryAdd(triangle.vertexIndexA, verticesForMesh.Count()-1))
                //     triangleIndicesForMesh[iterationCount] = verticesForMesh.Count()-1;
                // else
                //     triangleIndicesForMesh[iterationCount] = matchingIndices[triangle.vertexIndexA];
                //
                // verticesForMesh.Add(vertices[triangle.vertexIndexB]);
                // if (matchingIndices.TryAdd(triangle.vertexIndexB, verticesForMesh.Count()-1))
                //     triangleIndicesForMesh[iterationCount+1] = verticesForMesh.Count()-1;
                // else
                //     triangleIndicesForMesh[iterationCount+1] = matchingIndices[triangle.vertexIndexB];
                //
                // verticesForMesh.Add(vertices[triangle.vertexIndexC]);
                // if (matchingIndices.TryAdd(triangle.vertexIndexC, verticesForMesh.Count()-1))
                //     triangleIndicesForMesh[iterationCount+2] = verticesForMesh.Count()-1;
                // else
                //     triangleIndicesForMesh[iterationCount+2] = matchingIndices[triangle.vertexIndexC];
                //
                
                // Normals needs to be set to 0 first before being calculated to avoid unpredictable issues
                
                iterationCount += 3;
            }

            
            meshData.SetVertexBufferParams(finalVertices.Length, vertexAttributes);
            meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);
            vertexAttributes.Dispose();

            NativeArray<float3> meshDataVertices = meshData.GetVertexData<float3>();
            NativeArray<int> triangleIndices = meshData.GetIndexData<int>();
            
            finalVertices.AsArray().CopyTo(meshDataVertices);
            finalTriangleIndices.CopyTo(triangleIndices);
            
            
            NativeArray<float3> normals = meshData.GetVertexData<float3>(1);

            
            for (int i = 0; i < meshDataVertices.Length; i++)
            {
                normals[i] = float3.zero;
            }

            CalculateNormals(meshDataVertices, triangleIndices, normals);

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, finalTriangleIndices.Length)
            {
                bounds = bounds,
                vertexCount = finalVertices.Length
            }, MeshUpdateFlags.DontRecalculateBounds);
            
            // matchingIndices.Dispose();
            // verticesForMesh.Dispose();
        }
        
        private void CalculateNormals(NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<float3> normals)
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
        NativeArray<float> generatedMap = new NativeArray<float>(chunkHeight * supportedChunkSize * supportedChunkSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        NativeQueue<Triangle> triangles = new NativeQueue<Triangle>(Allocator.Persistent);
        NativeHashMap<int2, float3> vertices = new NativeHashMap<int2, float3>((chunkHeight * supportedChunkSize * supportedChunkSize), Allocator.Persistent);

        Bounds bounds = new Bounds(new Vector3(chunkSize / 2, chunkHeight / 2, chunkSize / 2), new Vector3(chunkSize, chunkHeight, chunkSize));
        MeshFilter mf = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer mr = chunkObject.AddComponent<MeshRenderer>();
        MeshCollider mc = chunkObject.AddComponent<MeshCollider>();
        Mesh mesh = new Mesh()
        {
            bounds = bounds
        };

        mf.sharedMesh = mesh;
        mr.material = meshMaterial;
        
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
            triangulation = triangulation1D,
            vertices = vertices.AsParallelWriter()
        };

        JobHandle marchHandle = marchJob.Schedule(generatedMap.Length, 4, mapDataHandle);

        
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        
        var chunkMeshJob = new ChunkMeshJob()
        {
            triangles = triangles,
            uniqueVertices = vertices,
            bounds = bounds,
            meshDataArray = meshDataArray
        };

        JobHandle chunkMeshHandle = chunkMeshJob.Schedule(marchHandle);


        StartCoroutine(ApplyMeshData(meshDataArray, mesh, mc, vertices, triangles, generatedMap, chunkMeshHandle));
        
    }

    private IEnumerator ApplyMeshData(Mesh.MeshDataArray meshDataArray, Mesh mesh, MeshCollider mc, NativeHashMap<int2, float3> vertices, NativeQueue<Triangle> triangles, NativeArray<float> mapData, JobHandle job)
    {
        yield return new WaitUntil(() => job.IsCompleted);
        job.Complete();
        
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        foreach (var vertex in mesh.vertices)
        {
            var c =GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = vertex;
            c.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }
        
        // mc.sharedMesh = mesh;
        triangles.Dispose();
        mapData.Dispose();
        vertices.Dispose();

    }
    
    // private IEnumerator CreateChunkMesh(NativeQueue<Triangle> triangles, JobHandle job, NativeArray<float> generatedMap, GameObject chunkObject)
    // {
    //     yield return new WaitUntil(() => job.IsCompleted);
    //     job.Complete();
    //     
    //     MeshFilter mf = chunkObject.AddComponent<MeshFilter>();
    //     MeshRenderer mr = chunkObject.AddComponent<MeshRenderer>();
    //     Mesh mesh = new Mesh();
    //
    //
    //     Vector3[] vertices = new Vector3[triangles.Count * 3];
    //     int[] tris = new int[triangles.Count * 3];
    //     int vertCount = 0;
    //     
    //     while (triangles.TryDequeue(out Triangle currentTriangle))
    //     {
    //         vertices[vertCount] = currentTriangle.a;
    //         vertices[vertCount+1] = currentTriangle.b;
    //         vertices[vertCount+2] = currentTriangle.c;
    //         
    //         tris[vertCount + 2] = vertCount;
    //         tris[vertCount + 1] = vertCount + 1;
    //         tris[vertCount] = vertCount + 2;
    //         
    //         vertCount += 3;
    //     }
    //     Vector3[] normals = CalculateNormals(vertices, tris);
    //     
    //     mesh.SetVertices(vertices);
    //     mesh.SetTriangles(tris, 0);
    //     mesh.SetNormals(normals);
    //     
    //     mf.mesh = mesh;
    //     mr.material = meshMaterial;
    //     triangles.Dispose();
    //     generatedMap.Dispose();
    // }

    public static int to1D( int x, int y, int z)
    {
        return x + y*supportedChunkSize + z*supportedChunkSize*chunkHeight;
    }
    
    public static int to1D(int3 xyz)
    {
        return xyz.x + xyz.y*supportedChunkSize + xyz.z*supportedChunkSize*chunkHeight;
    }
}


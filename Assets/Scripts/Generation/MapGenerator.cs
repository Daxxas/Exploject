using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        public NativeHashMap<int3, float3>.ParallelWriter vertices;
        
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
                
                int a1 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + (i+1)]];
                int b1 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + (i+1)]];
                
                int a2 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + (i+2)]];
                int b2 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + (i+2)]];

                // Find vertex position on edge & add them to vertices list

                float3 vert0 = FindVertexPos(threshold, marchCube[a0].p, marchCube[b0].p, marchCube[a0].val, marchCube[b0].val);
                float3 vert1 = FindVertexPos(threshold, marchCube[a1].p, marchCube[b1].p, marchCube[a1].val, marchCube[b1].val);
                float3 vert2 = FindVertexPos(threshold, marchCube[a2].p, marchCube[b2].p, marchCube[a2].val, marchCube[b2].val);

                // Low res
                int3 roundedVert0 = math.int3(math.round(vert0 * 100f));
                int3 roundedVert1 = math.int3(math.round(vert1 * 100f));
                int3 roundedVert2 = math.int3(math.round(vert2 * 100f));
                
                // High res 
                // int3 roundedVert0 = math.int3(vert0 * 1000f);
                // int3 roundedVert1 = math.int3(vert1 * 1000f);
                // int3 roundedVert2 = math.int3(vert2 * 1000f);
                

                vertices.TryAdd(roundedVert0, vert0);
                vertices.TryAdd(roundedVert1, vert1);
                vertices.TryAdd(roundedVert2, vert2);

                triangle.vertexIndexA = roundedVert0;
                triangle.vertexIndexB = roundedVert1;
                triangle.vertexIndexC = roundedVert2;
                triangles.Enqueue(triangle);
            }

            marchCube.Dispose();
        }
    }
    
    // public struct MergeCloseVertJob : IJob
    // {
    //     
    //     
    //     public void Execute()
    //     {
    //         
    //     }
    // }

    [BurstCompile(FloatPrecision.High, FloatMode.Fast,CompileSynchronously = true)]
    public struct ChunkMeshJob : IJob
    {
        public NativeQueue<Triangle> triangles;
        public NativeHashMap<int3, float3> uniqueVertices;
        public Mesh.MeshDataArray meshDataArray;
        public Bounds bounds;
        
        public void Execute()
        {
            int vertexAttributeCount = 4;
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
            vertexAttributes[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4, stream: 2
            );
            vertexAttributes[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2, stream: 3
            );
            
            // Finding triangles & vertices array
            NativeHashMap<int3, int> matchingIndices = new NativeHashMap<int3, int>(initVertexCount, Allocator.Temp);
            
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
            NativeArray<float4> tangents = meshData.GetVertexData<float4>(2);
            NativeArray<float2> texCoords = meshData.GetVertexData<float2>(3);

            for (int i = 0; i < meshDataVertices.Length; i++)
            {
                normals[i] = float3.zero;
            }

            CalculateNormals(meshDataVertices, triangleIndices, normals);
            CalculateUVs(meshDataVertices, triangleIndices, normals, texCoords);

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, finalTriangleIndices.Length)
            {
                bounds = bounds,
                vertexCount = finalVertices.Length
            }, MeshUpdateFlags.DontRecalculateBounds);
            
            matchingIndices.Dispose();
            finalVertices.Dispose();
            finalTriangleIndices.Dispose();
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


        private void CalculateUVs(NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<float3> normals, NativeArray<float2> uvs)
        {
            // float scale = 0.04f;
            //
            // for(int i = 0; i < vertices.Length; i++)
            // {
            //     // float2 stx = new float2();
            //     // float2 sty = new float2();
            //     // float2 stz = new float2();
            //     //
            //     // if (normals[i].x >= 0)
            //     // {
            //     //     uvs[i] = stx;
            //     //     stx.x = vertices[i].y;
            //     // }
            //     // else
            //     //     stx.x = -vertices[i].y;
            //     //
            //     // if (normals[i].y >= 0)
            //     // {
            //     //     uvs[i] = sty;
            //     //     sty.x = vertices[i].x;
            //     // }
            //     // else
            //     //     sty.x = -vertices[i].x;
            //     //
            //     // if (normals[i].z >= 0)
            //     // {
            //     //     uvs[i] = stz;
            //     //     stz.x = vertices[i].x;
            //     // }
            //     // else
            //     //     stz.x = -vertices[i].x;
            //     //
            //     //
            //     // stx.y = sty.y = vertices[i].z;
            //     // stz.y = vertices[i].y;
            //     uvs[i] = vertices[i].xz;
            //
            // }
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
        NativeHashMap<int3, float3> vertices = new NativeHashMap<int3, float3>((chunkHeight * supportedChunkSize * supportedChunkSize), Allocator.Persistent);

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


        StartCoroutine(ApplyMeshData(position, meshDataArray, mesh, mc, vertices, triangles, generatedMap, chunkMeshHandle));
        
    }

    private IEnumerator ApplyMeshData(Vector2 position, Mesh.MeshDataArray meshDataArray, Mesh mesh, MeshCollider mc, NativeHashMap<int3, float3> vertices, NativeQueue<Triangle> triangles, NativeArray<float> mapData, JobHandle job)
    {
        yield return new WaitUntil(() => job.IsCompleted);
        job.Complete();
        
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        // for (int i = 0; i < vertices.GetKeyValueArrays(Allocator.Persistent).Keys.Length; i++)
        // {
        //     var vertex = vertices.GetKeyValueArrays(Allocator.Persistent).Values[i];
        //     var vertexIndex = vertices.GetKeyValueArrays(Allocator.Persistent).Keys[i];
        //
        //     var tmp = new GameObject();
        //     var tmpc = tmp.AddComponent<TextMeshPro>();
        //     tmpc.text = vertexIndex.ToString();
        //     tmpc.fontSize = 0.5f;
        //     tmpc.alignment = TextAlignmentOptions.Midline;
        //     
        //     var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //     c.transform.position = new Vector3(vertex.x + position.x, vertex.y, position.y + vertex.z);
        //     c.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //     c.transform.parent = g.transform;
        //     c.GetComponent<MeshRenderer>().material.color = new Color(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255));
        //     tmp.transform.parent = c.transform;
        //     tmp.transform.localPosition = new Vector3(0, 0.5f, 0);
        // }

        mesh.RecalculateTangents();
        mc.sharedMesh = mesh;
        triangles.Dispose();
        mapData.Dispose();
        vertices.Dispose();

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


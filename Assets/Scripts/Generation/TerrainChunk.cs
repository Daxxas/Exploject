using System;
using System.Collections;
using DefaultNamespace;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


public class TerrainChunk : MonoBehaviour
{
    [SerializeField] private MeshFilter mf;
    [SerializeField] private MeshRenderer mr;
    [SerializeField] private MeshCollider mc;
    [SerializeField] private Material meshMaterial;

    private MapDataGenerator mapDataGenerator;
    private Vector2 position;
    private Bounds bounds;
    private bool isInit = false;
    
    public TerrainChunk InitChunk(Vector2 coord, MapDataGenerator dataGenerator, Transform mapParent)
    {
        mapDataGenerator = dataGenerator;

        position = coord * MapDataGenerator.chunkSize;
        Vector3 positionV3 = new Vector3(position.x, 0, position.y);
        
        gameObject.name = "Chunk Terrain " + position.x + " " + position.y;
        
        transform.parent = mapParent;
        transform.position = positionV3;
        // SetVisible(false);

        // Variables that will be filled & passed along jobs
        NativeArray<float> generatedMap = new NativeArray<float>(MapDataGenerator.chunkHeight * (MapDataGenerator.supportedChunkSize) * (MapDataGenerator.supportedChunkSize), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        var mapDataHandle = mapDataGenerator.GenerateMapData(position, generatedMap);

        GenerateChunkMesh(generatedMap, MapDataGenerator.chunkSize, MapDataGenerator.chunkBorderIncrease, MapDataGenerator.chunkHeight, MapDataGenerator.threshold, mapDataHandle);

        return this;
    }
    
    public void UpdateChunk(float maxViewDst, Vector2 viewerPosition)
    {
        float viewerDistanceFromNearEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
        bool visible = viewerDistanceFromNearEdge <= maxViewDst;
        SetVisible(visible);
    }

    public void SetVisible(bool visible)
    {
        // Disabling object stops coroutine
        // which can be dangerous a coroutine waiting for jobs to finish to Dipose arrays
        if (isInit)
        {
            gameObject.SetActive(visible);
        }
        else if(visible == false)
        {
            StartCoroutine(WaitForInitBeforeVisible());
        }
        
    }

    private IEnumerator WaitForInitBeforeVisible()
    {
        yield return new WaitUntil(() => isInit);
        SetVisible(false);
    }

    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }

    private NativeQueue<Triangle> triangles;
    private NativeHashMap<int3, float3> vertices;
    private Mesh.MeshDataArray meshDataArray;
    
    public JobHandle GenerateChunkMesh(NativeArray<float> generatedMap, int chunkSize, int chunkBorderIncrease, int chunkHeight, float threshold, JobHandle mapDataHandle)
    {
        int supportedChunkSize = chunkSize + chunkBorderIncrease;
        triangles = new NativeQueue<Triangle>(Allocator.Persistent);
        vertices = new NativeHashMap<int3, float3>((chunkHeight * supportedChunkSize * supportedChunkSize), Allocator.Persistent);

        
        // Generate mesh object with its components
        bounds = new Bounds(new Vector3(chunkSize / 2, chunkHeight / 2, chunkSize / 2), new Vector3(chunkSize, chunkHeight, chunkSize));
        // gameObject.AddComponent<BoundGizmo>();
        
        Mesh mesh = new Mesh()
        {
            bounds = bounds
        };
        
        mf.sharedMesh = mesh;
        mr.material = meshMaterial;
        
        meshDataArray = Mesh.AllocateWritableMeshData(1);

        
        // Start marching cube based on a map data
        var marchJob = new MarchCubeJob()
        {
            // Input data
            map = generatedMap,
            // March table 
            cornerIndexAFromEdge = mapDataGenerator.cornerIndexAFromEdge,
            cornerIndexBFromEdge = mapDataGenerator.cornerIndexBFromEdge,
            triangulation = mapDataGenerator.triangulation1D,
            // Arrays to fill with this job
            triangles = triangles.AsParallelWriter(),
            vertices = vertices.AsParallelWriter(),
            chunkSize = chunkSize,
            chunkBorderIncrease = chunkBorderIncrease,
            chunkHeight = chunkHeight,
            threshold = threshold
        };

        JobHandle marchHandle = marchJob.Schedule(generatedMap.Length, 4, mapDataHandle);

        // Generate Mesh from Marching cube result
        var chunkMeshJob = new ChunkMeshJob()
        {
            triangles = triangles,
            uniqueVertices = vertices,
            bounds = bounds,
            meshDataArray = meshDataArray,
            map = generatedMap,
            chunkHeight = chunkHeight,
            chunkSize = chunkSize
        };

        var chunkMeshHandle = chunkMeshJob.Schedule(marchHandle);
        
        StartCoroutine(ApplyMeshData(meshDataArray, mesh, mc, vertices, triangles, generatedMap, chunkMeshHandle, chunkMeshJob));
        
        return new JobHandle();
    }
    
    private IEnumerator ApplyMeshData(Mesh.MeshDataArray meshDataArray, Mesh mesh, MeshCollider mc, NativeHashMap<int3, float3> vertices, NativeQueue<Triangle> triangles, NativeArray<float> map, JobHandle job, ChunkMeshJob chunkMeshJob)
    {
        yield return new WaitUntil(() => job.IsCompleted);
        job.Complete();
        isInit = true;
        // chunkMeshJob.Execute();
        // DebugChunks(vertices, pos);
        // DebugData(map);
        
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.RecalculateTangents();
        mc.sharedMesh = mesh;
        triangles.Dispose();
        vertices.Dispose();
        map.Dispose();
    }
    
    
    public void DebugData(NativeArray<float> map)
    {

        for (int idx = 0; idx < map.Length; idx++)
        {
            int x = idx % MapDataGenerator.supportedChunkSize;
            int y = (idx / MapDataGenerator.supportedChunkSize) % MapDataGenerator.chunkHeight;
            int z = idx / (MapDataGenerator.supportedChunkSize * MapDataGenerator.chunkHeight);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.transform.position = new Vector3(x-2, y, z-2);
            sphere.transform.localScale = new Vector3(0.1f,0.1f,0.1f);
            sphere.name = map[x + y * MapDataGenerator.supportedChunkSize + z * MapDataGenerator.supportedChunkSize * MapDataGenerator.chunkHeight].ToString();
        }
    }

    private void OnDestroy()
    {
        if (!isInit)
        {
            try
            {
                triangles.Dispose();
                vertices.Dispose();
            }
            catch (Exception e)
            {
            }
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

        public int chunkSize;
        public int chunkBorderIncrease;
        public int chunkHeight;
        public float threshold;
        private int supportedChunkSize => chunkSize + chunkBorderIncrease;
        
        // job is IJobParallelFor, one index = 1 cube tested
        public void Execute(int idx)
        {
            // Cube currently tested, in array so it's easier to follow
            NativeArray<MapDataGenerator.CubePoint> marchCube = new NativeArray<MapDataGenerator.CubePoint>(8, Allocator.Temp);
            
            // Remember :
            // Map data generates 18x18 (ignoring height) data to calculate normals
            // Marching will output a vertices array of 18x18 to allow mesh generation to calculate normals
            // but the mesh generation should output a 16x16 vertices array
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % chunkHeight;
            int z = idx / (supportedChunkSize * chunkHeight);

            // Don't calculate when we are too close to the edge of the chunk to prevent out of bound
            if (x < 1 || y < 1 || z < 1 || x >= supportedChunkSize-2 || y >= chunkHeight-2 || z >= supportedChunkSize-2)
            {
                return;
            }
            
            // put march cube in array to have cleaner code after
            #region March cube definition 
            marchCube[0] = new MapDataGenerator.CubePoint()
            {
                p = new int3(x, y, z),
                val = map[to1D(x, y, z)]
            };
            marchCube[1] = new MapDataGenerator.CubePoint()
            {
                p = new int3(x + 1, y, z),
                val = map[to1D(x + 1, y, z)]
            };
            marchCube[2] = new MapDataGenerator.CubePoint()
            {
                p = new int3(x + 1, y, z + 1),
                val = map[to1D(x + 1, y, z + 1)]
            };
            marchCube[3] = new MapDataGenerator.CubePoint()
            {
                p = new int3(x, y, z + 1),
                val = map[to1D(x, y, z + 1)]
            };
            marchCube[4] = new MapDataGenerator.CubePoint()
            {
                p = new int3(x, y + 1, z),
                val = map[to1D(x, y + 1, z)]
            };
            marchCube[5] = new MapDataGenerator.CubePoint()
            {
                p = new int3(x + 1, y + 1, z),
                val = map[to1D(x + 1, y + 1, z)]
            };
            marchCube[6] = new MapDataGenerator.CubePoint()
            {
                p = new int3(x + 1, y + 1, z + 1),
                val = map[to1D(x + 1, y + 1, z + 1)]
            };
            marchCube[7] = new MapDataGenerator.CubePoint()
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

                // offset every vert by -3 for some reason idk, otherwise the chunk isn't really in the middle of its supposed bounds
                // probably because of supportedChunksize offset
                vert0.xz -= 1;
                vert1.xz -= 1;
                vert2.xz -= 1;
                
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
        
        private float3 FindVertexPos(float threshold, float3 p1, float3 p2, float v1val, float v2val)
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
        public int to1D( int x, int y, int z)
        {
            return x + y*supportedChunkSize + z*supportedChunkSize*chunkHeight;
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
        public NativeArray<float> map;
        // Output
        public Mesh.MeshDataArray meshDataArray;

        public int chunkSize;
        public int chunkHeight;

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
            NativeList<float3> chunkVertices = new NativeList<float3>(initVertexCount, Allocator.Temp);
            NativeList<int> chunkTriangles = new NativeList<int>(triangles.Count * 3, Allocator.Temp);
            
            NativeList<float3> borderVertices = new NativeList<float3>((chunkSize * chunkHeight) * 4 + chunkHeight * 4, Allocator.Temp);
            NativeList<int> borderTriangles = new NativeList<int>(triangles.Count * 3, Allocator.Temp);
            // Transform the hashmap of int3, float3 to an array of vertices (int, float3)
            // For every triangle

            int chunkVerticesIndices = 0;
            int borderVerticesIndices = -1;
            while (triangles.TryDequeue(out Triangle triangle))
            {
                
                // If triangle is in a border
                bool isVertexABorder = triangle.vertexIndexA.x < 0 || triangle.vertexIndexA.z < 0 || triangle.vertexIndexA.x > chunkSize * 100 || triangle.vertexIndexA.z > chunkSize * 100;
                bool isVertexBBorder = triangle.vertexIndexB.x < 0 || triangle.vertexIndexB.z < 0 || triangle.vertexIndexB.x > chunkSize * 100 || triangle.vertexIndexB.z > chunkSize * 100;
                bool isVertexCBorder = triangle.vertexIndexC.x < 0 || triangle.vertexIndexC.z < 0 || triangle.vertexIndexC.x > chunkSize * 100 || triangle.vertexIndexC.z > chunkSize * 100;
                bool isBorderTriangle = isVertexABorder || isVertexBBorder || isVertexCBorder;
                
                
                // Order is C - B - A to have triangle in the right direction
                
                // Logic is :
                // Vertices included in the final mesh have positive indices starting from 0
                // Vertices on the border of the chunk have negative indices starting from 1
                // Like that, when we loop through borderTriangles, we know from which array between chunkVertices and borderVertices
                // we need to pull the vertex from

                // borderTriangles have a mix of indices from chunkVertices and borderVertices (negative and positive indices)

                if (isVertexCBorder)
                {
                    // If the vertex is on the border, meaning it's not in the final mesh
                    // we try to add it to the matching indicies with the borderVerticesIndices as the matching index of the vertex 
                    if (matchingIndices.TryAdd(triangle.vertexIndexC, borderVerticesIndices))
                    {
                        // add vertex to borderVertices for futur normal calculation
                        borderVertices.Add(uniqueVertices[triangle.vertexIndexC]);
                        // add vertex index to borderTriangles
                        borderTriangles.Add(borderVerticesIndices);
                        // borderVerticesIndices is a negative index, so we decrement it
                        borderVerticesIndices--;
                    }
                    else
                    {
                        // Vertex outside of mesh already exists, we only add it to borderTriangles
                        borderTriangles.Add(matchingIndices[triangle.vertexIndexC]);
                    }
                }
                else
                {
                    // The vertex is in the chunk mesh
                    if (matchingIndices.TryAdd(triangle.vertexIndexC, chunkVerticesIndices))
                    {
                        // if it's a vertex we don't know, add it to chunkVertices no matter if the vertex comes from
                        // a border triangle or not.
                        // If the vertex comes from a border triangle, it's ok, we will find it back through a mesh chunk triangle
                        chunkVertices.Add(uniqueVertices[triangle.vertexIndexC]);
                        
                        if (!isBorderTriangle)
                        {
                            chunkTriangles.Add(chunkVerticesIndices);
                        }
                        else
                        {
                            // if the triangle we are currently looping through if a triangle in the border
                            // we add the vertex index to borderTriangles
                            // This is why borderTriangles contains positive indices 
                            borderTriangles.Add(chunkVerticesIndices);
                        }
                        chunkVerticesIndices++;
                    }
                    else
                    {
                        // Same logic as commment above
                        if (!isBorderTriangle)
                        {
                            chunkTriangles.Add(matchingIndices[triangle.vertexIndexC]);
                        }
                        else
                        {
                            borderTriangles.Add(matchingIndices[triangle.vertexIndexC]);
                        }
                    }
                }
                
                // Same logic for vertex B of triangle
                if (isVertexBBorder)
                {
                    if (matchingIndices.TryAdd(triangle.vertexIndexB, borderVerticesIndices))
                    {
                        borderVertices.Add(uniqueVertices[triangle.vertexIndexB]);
                        borderTriangles.Add(borderVerticesIndices);
                        borderVerticesIndices--;
                    }
                    else
                    {
                        borderTriangles.Add(matchingIndices[triangle.vertexIndexB]);
                    }
                }
                else
                {
                    if (matchingIndices.TryAdd(triangle.vertexIndexB, chunkVerticesIndices))
                    {
                        chunkVertices.Add(uniqueVertices[triangle.vertexIndexB]);
                        if (!isBorderTriangle)
                        {
                            chunkTriangles.Add(chunkVerticesIndices);
                        }
                        else
                        {
                            borderTriangles.Add(chunkVerticesIndices);
                        }
                        chunkVerticesIndices++;
                    }
                    else
                    {
                        if (!isBorderTriangle)
                        {
                            chunkTriangles.Add(matchingIndices[triangle.vertexIndexB]);
                        }
                        else
                        {
                            borderTriangles.Add(matchingIndices[triangle.vertexIndexB]);
                        }
                    }
                }
                
                // Same logic for vertex A of triangle
                if (isVertexABorder)
                {
                    if (matchingIndices.TryAdd(triangle.vertexIndexA, borderVerticesIndices))
                    {
                        borderVertices.Add(uniqueVertices[triangle.vertexIndexA]);
                        borderTriangles.Add(borderVerticesIndices);
                        borderVerticesIndices--;
                    }
                    else
                    {
                        borderTriangles.Add(matchingIndices[triangle.vertexIndexA]);
                    }
                }
                else
                {

                    if (matchingIndices.TryAdd(triangle.vertexIndexA, chunkVerticesIndices))
                    {
                        chunkVertices.Add(uniqueVertices[triangle.vertexIndexA]);
                        if (!isBorderTriangle)
                        {
                            chunkTriangles.Add(chunkVerticesIndices);
                        }
                        else
                        {
                            borderTriangles.Add(chunkVerticesIndices);
                        }
                        chunkVerticesIndices++;
                    }
                    else
                    {
                        if (!isBorderTriangle)
                        {
                            chunkTriangles.Add(matchingIndices[triangle.vertexIndexA]);
                        }
                        else
                        {
                            borderTriangles.Add(matchingIndices[triangle.vertexIndexA]);
                        }
                    }
                }
            }
            
            // Apply calculated array to mesh data arrays
            // we don't know in advance the vertices array length, so it's not possible to directly modify the
            // meshData vertices array
            meshData.SetVertexBufferParams(chunkVertices.Length, vertexAttributes);
            meshData.SetIndexBufferParams(chunkTriangles.Length, IndexFormat.UInt32);
            vertexAttributes.Dispose();

            NativeArray<float3> meshDataVertices = meshData.GetVertexData<float3>();
            NativeArray<int> triangleIndices = meshData.GetIndexData<int>();
            
            chunkVertices.AsArray().CopyTo(meshDataVertices);
            chunkTriangles.AsArray().CopyTo(triangleIndices);
            
            
            // Normal calculation
            NativeArray<float3> normals = meshData.GetVertexData<float3>(1);

            for (int i = 0; i < meshDataVertices.Length; i++)
            {
                // normals needs to be set to zero before calculating them
                normals[i] = float3.zero;
            }

            CalculateNormals(chunkVertices, borderVertices, borderTriangles, triangleIndices, normals);
            
            // Not necessary, only to have non triplanar material working a bit
            // Non-triplanar materials will be streched vertically
            SetBasicUVs( meshDataVertices, meshData.GetVertexData<float2>(3));
            
            // Finalize meshData
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, chunkTriangles.Length)
            {
                bounds = bounds,
                vertexCount = chunkVertices.Length
            }, MeshUpdateFlags.DontRecalculateBounds);
            
            /////////////////////////////////
            matchingIndices.Dispose();
            chunkVertices.Dispose();
            chunkTriangles.Dispose();
            borderTriangles.Dispose();
            borderVertices.Dispose();
        }

        private void CalculateNormals(NativeArray<float3> vertices, NativeArray<float3> borderVertices, NativeArray<int> borderTriangles, NativeArray<int> triangles, NativeArray<float3> normals)
        {
            // Loop through chunk triangles
            for (int i = 0; i < vertices.Length; i++)
            {
                float3 normal = SurfaceNormal(i, vertices);
                normals[i] = normal;
            }
        }

        float3 SurfaceNormal(int pointIndex, NativeArray<float3> vertices)
        {
            float3 returnNormal = float3.zero;
            float3 currentVert = vertices[pointIndex];
            // var parent = new GameObject(currentVert.ToString());
            
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    for (int z = -1; z < 2; z++)
                    {
                        if (!(x == 0 && y == 0 && z == 0))
                        {
                            float3 offsetValuePos = new float3(currentVert.x + x, currentVert.y + y, currentVert.z + z);
                            float3 gradientVec = currentVert - offsetValuePos;
                            
                            float valueAtOffsetPos = GetInterpolatedValue(offsetValuePos);
                            // var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            // go.transform.parent = parent.transform;
                            // go.transform.position = offsetValuePos;
                            // go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            
                            if (valueAtOffsetPos >= 0)
                            {
                                // var np = go.AddComponent<NormalPreview>();
                                // np.normal = math.normalize(gradientVec * valueAtOffsetPos);
                                // np.valueAtThisPos = valueAtOffsetPos;
                                returnNormal += math.normalize(gradientVec * valueAtOffsetPos);
                            }
                            else
                            {
                                // Destroy(go);
                            }
                            
                            // Debug.Log($"For value {currentVert}, testing {offsetValuePos}, gradient is {gradientVec}, final math is : {math.normalize(gradientVec * valueAtOffsetPos)}");
                        }
                    }
                }
            }

            return returnNormal;
        }

        private float GetInterpolatedValue(float3 xyz)
        {
            int3 xyz0 = ((int3) math.floor(xyz));
            int3 xyz1 = xyz0 + 1;

            // for (int i = 0; i < 2; i++)
            // {
            //     for (int j = 0; j < 2; j++)
            //     {
            //         for (int k = 0; k < 2; k++)
            //         {
            //             var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //             go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            //             Vector3 finalPos = Vector3.zero;
            //             if (i == 0)
            //                 finalPos.x = xyz0.x;
            //             else
            //                 finalPos.x = xyz1.x;
            //
            //             if (j == 0)
            //                 finalPos.y = xyz0.y;
            //             else
            //                 finalPos.y = xyz1.y;
            //
            //             if (k == 0)
            //                 finalPos.z = xyz0.z;
            //             else
            //                 finalPos.z = xyz1.z;
            //             
            //             go.transform.parent = parent;
            //             go.transform.position = finalPos;
            //             go.name = map[to1D((int3) new float3(finalPos.x, finalPos.y, finalPos.z))].ToString();
            //             finalPos.x++;
            //             finalPos.z++;
            //             // go.AddComponent<NormalPreview>().valueAtThisPos = map[to1D((int) finalPos.x, (int) finalPos.y, (int) finalPos.z)];
            //
            //         }
            //     }
            // }
            
            float xd = (xyz.x - xyz0.x) / (xyz1.x - xyz0.x);
            float yd = (xyz.y - xyz0.y) / (xyz1.y - xyz0.y);
            float zd = (xyz.z - xyz0.z) / (xyz1.z - xyz0.z);

            
            // Debug.Log($"xyz: {xyz} xyz0: {xyz0}, xyz1: {xyz1} c001: {to1D(xyz0.x, xyz0.y, xyz1.z)}, c101: {to1D(to1D(xyz1.x, xyz0.y, xyz1.z))} \r\n" +
            //           $"({xyz0.x}+1) + {xyz0.y}*{MapDataGenerator.supportedChunkSize} + ({xyz1.z}+1)*{MapDataGenerator.supportedChunkSize}*{chunkHeight};");
            float c00 = map[to1D(xyz0)] * (1 - xd) + map[to1D(xyz1.x, xyz0.y, xyz0.z)] * xd;
            float c01 = map[to1D(xyz0.x, xyz0.y, xyz1.z)] * (1 - xd) + map[to1D(xyz1.x, xyz0.y, xyz1.z)] * xd;
            float c10 = map[to1D(xyz0.x, xyz1.y, xyz0.z)] * (1 - xd) + map[to1D(xyz1.x, xyz1.y, xyz0.z)] * xd;
            float c11 = map[to1D(xyz0.x, xyz1.y, xyz1.z)] * (1 - xd) + map[to1D(xyz1)] * xd;
            
            float c0 = c00 * (1 - yd) + c10 * yd;
            float c1 = c01 * (1 - yd) + c11 * yd;
            
            float c = c0 * (1 - zd) + c1 * zd;
            
            return c;

        }

        
        public int to1D( int x, int y, int z)
        {
            return (x+1) + y*MapDataGenerator.supportedChunkSize + (z+1)*MapDataGenerator.supportedChunkSize*chunkHeight;
        }

        public int to1D(int3 xyz)
        {
            return to1D(xyz.x, xyz.y, xyz.z);
        }
        
        private void SetBasicUVs(NativeArray<float3> vertices, NativeArray<float2> uvs)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = vertices[i].xz;
            }
        }
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
}

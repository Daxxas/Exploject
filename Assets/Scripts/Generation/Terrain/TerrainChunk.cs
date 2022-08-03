using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public class TerrainChunk : MonoBehaviour
{
    [SerializeField] private MeshFilter mf;
    [SerializeField] private MeshRenderer mr;
    [SerializeField] private MeshCollider mc;
    [SerializeField] private Material meshMaterial;

    private Vector2 position;
    public Vector2 Position => position;

    private Vector2 chunkPos;
    public Vector2 ChunkPos => chunkPos;

    private Bounds bounds;
    
    private int resolution;
    private int supportedChunkSize => MapDataGenerator.ChunkSize + resolution * 3;

    private JobHandle chunkMeshHandle;
    private JobHandle marchHandle;
    private JobHandle mapDataHandle;
    private JobHandle colliderJobHandle;

    private bool safeToRemove = false;
    private bool removeRequest = false;
    public bool SafeToRemove => safeToRemove;

    public static NativeArray<int> cornerIndexAFromEdge;
    public static NativeArray<int> cornerIndexBFromEdge;
    public static NativeArray<int> triangulation1D;

    // TODO : Dynamic LOD System
    [SerializeField] private GameObject chunkLOD1;
    
    private NativeQueue<Triangle> triangles;
    private NativeParallelHashMap<Edge, float3> uniqueVertices;
    private NativeParallelHashMap<BiomeHolder, UnsafeList<int>> chunkTriangles;
    private NativeList<Vector3> chunkVertices;
    private NativeList<Vector3> chunkNormals;
    private NativeArray<float> generatedMap;
    private NativeArray<BiomeHolder> biomesForTerrainChunk;
    private NativeList<BiomeHolder> biomesInChunk;


    public static void InitMarchCubeArrays()
    {
        if (!cornerIndexAFromEdge.IsCreated)
        {
            cornerIndexAFromEdge = new NativeArray<int>(12, Allocator.Persistent);
            cornerIndexAFromEdge.CopyFrom(MarchTable.cornerIndexAFromEdgeArray);
        }
        if (!cornerIndexBFromEdge.IsCreated)
        {
            cornerIndexBFromEdge = new NativeArray<int>(12, Allocator.Persistent);
            cornerIndexBFromEdge.CopyFrom(MarchTable.cornerIndexBFromEdgeArray);
        }
        if (!triangulation1D.IsCreated)
        {
            triangulation1D = new NativeArray<int>(4096, Allocator.Persistent);
            triangulation1D.CopyFrom(MarchTable.triangulation1DArray);
        }
    }
    
    /// <summary>
    /// Initialize chunk and starts generation
    /// </summary>
    /// <param name="coord">Chunk coordinates</param>
    /// <param name="biomeFunctionPointers">2D Array of biomes for every xz positions</param>
    /// <param name="biomesWidth"></param>
    /// <param name="resolution">Expected mesh resolution generation</param>
    /// <returns></returns>
    public TerrainChunk InitChunk(Vector2 coord, int resolution)
    {
        position = coord * MapDataGenerator.ChunkSize;
        chunkPos = coord;
        
        transform.position = new Vector3(position.x, 0, position.y);

        // TODO : Change this for LODs support
        this.resolution = resolution;

        // Variables that will be filled & passed along jobs
        generatedMap = new NativeArray<float>(MapDataGenerator.chunkHeight * supportedChunkSize * supportedChunkSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        biomesForTerrainChunk = BiomeGenerator.Instance.GetBiomesOfTerrainChunkForJob(coord, resolution);
        biomesInChunk = BiomeGenerator.Instance.GetBiomesInChunk(coord, resolution);
        
        mapDataHandle = MapDataGenerator.Instance.GenerateMapData(position, generatedMap);

        chunkMeshHandle = GenerateChunkMesh(generatedMap, biomesForTerrainChunk, biomesInChunk, mapDataHandle);
        
        return this;
    }
    
    /// <summary>
    /// Update chunk visibility based on distance from player location
    /// </summary>
    public void UpdateVisibility()
    {
        Vector3 transposedViewerPosition = new Vector3(EndlessTerrain.viewerPosition.x, MapDataGenerator.chunkHeight / 2, EndlessTerrain.viewerPosition.y);
        
        var viewerDistanceFromNearEdge = Mathf.Sqrt(bounds.SqrDistance(transposedViewerPosition));
        
        bool visible = viewerDistanceFromNearEdge <= EndlessTerrain.Instance.UnitViewDistance;
        SetVisible(visible);
    }
    
    /// <summary>
    /// Set chunk visibility
    /// </summary>
    /// <param name="visible">Chunk visibility</param>
    public void SetVisible(bool visible)
    {
        chunkLOD1.SetActive(visible);
    }

    /// <summary>
    /// Destroy chunk, if chunk has jobs running, wait for them to finish before destroying chunk
    /// </summary>
    public void DestroyChunk()
    {
        if (SafeToRemove)
        {
            Destroy(this);
        }
        else
        {
            removeRequest = true;
        }
    }

    public void DisposeTerrainChunk()
    {
        mapDataHandle.Complete();
        chunkMeshHandle.Complete();
        marchHandle.Complete();
        colliderJobHandle.Complete();
        
        if(triangles.IsCreated) triangles.Dispose();
        if(uniqueVertices.IsCreated) uniqueVertices.Dispose();

        foreach (var chunkTriangle in chunkTriangles)
        {
            if(chunkTriangle.Value.IsCreated) chunkTriangle.Value.Dispose();
        }
        
        if(chunkTriangles.IsCreated) chunkTriangles.Dispose();
        
        if(chunkVertices.IsCreated) chunkVertices.Dispose();
        if(chunkNormals.IsCreated) chunkNormals.Dispose();
        if(generatedMap.IsCreated) generatedMap.Dispose();
        if(biomesForTerrainChunk.IsCreated) biomesForTerrainChunk.Dispose();
        if(biomesInChunk.IsCreated) biomesInChunk.Dispose();
        
    }

    public static void DisposeMarchCubeArrays()
    {
        if(cornerIndexAFromEdge.IsCreated) cornerIndexAFromEdge.Dispose();
        if(cornerIndexBFromEdge.IsCreated) cornerIndexBFromEdge.Dispose();
        if(triangulation1D.IsCreated) triangulation1D.Dispose();
    }
    
    //////////////////////////////////////////////////////////////////
    // Jobs
    //////////////////////////////////////////////////////////////////
    
    /// <summary>
    /// Generate a mesh from a map data array
    /// </summary>
    /// <param name="generatedMap">Data array for marching cube</param>
    /// <param name="mapDataHandle">Handle for the data job</param>
    /// <returns></returns>
    public JobHandle GenerateChunkMesh(NativeArray<float> generatedMap, NativeArray<BiomeHolder> biomesForTerrainChunk, NativeList<BiomeHolder> biomesInChunk, JobHandle mapDataHandle)
    {
        triangles = new NativeQueue<Triangle>(Allocator.TempJob);
        uniqueVertices = new NativeParallelHashMap<Edge, float3>((MapDataGenerator.chunkHeight * supportedChunkSize * supportedChunkSize), Allocator.Persistent);
        chunkTriangles = new NativeParallelHashMap<BiomeHolder, UnsafeList<int>>(biomesInChunk.Length,Allocator.TempJob);
        
        foreach (var biomeTriangles in chunkTriangles)
        {
            biomeTriangles.Value = new UnsafeList<int>(MapDataGenerator.chunkHeight * supportedChunkSize * supportedChunkSize * 3, Allocator.TempJob);
        }
        
        chunkVertices = new NativeList<Vector3>(Allocator.TempJob);
        chunkNormals = new NativeList<Vector3>(Allocator.TempJob);
        
        Mesh mesh = new Mesh();

        mf.sharedMesh = mesh;

        // Start marching cube based on a map data
        var marchJob = new MarchCubeJob()
        {
            // Input data
            map = generatedMap,
            resolution = resolution,
            biomesForTerrainChunk = biomesForTerrainChunk,
            // March table 
            cornerIndexAFromEdge = cornerIndexAFromEdge,
            cornerIndexBFromEdge = cornerIndexBFromEdge,
            triangulation = triangulation1D,
            // Arrays to fill with this job
            triangles = triangles.AsParallelWriter(),
            vertices = uniqueVertices.AsParallelWriter(),
        };
        
        marchHandle = marchJob.Schedule(generatedMap.Length, 100, mapDataHandle);

        // Generate Mesh from Marching cube result
        var chunkMeshJob = new ChunkMeshJob()
        {
            // Input
            triangles = triangles,
            biomesInChunk = biomesInChunk,
            uniqueVertices = uniqueVertices,
            resolution = resolution,
            map = generatedMap,
            // Output 
            chunkTriangles = chunkTriangles,
            chunkNormals = chunkNormals,
            chunkVertices = chunkVertices
        };

        var chunkMeshHandle = chunkMeshJob.Schedule(marchHandle);

        triangles.Dispose(chunkMeshHandle);
        uniqueVertices.Dispose(chunkMeshHandle);
        generatedMap.Dispose(chunkMeshHandle);
        biomesInChunk.Dispose(chunkMeshHandle);
        biomesForTerrainChunk.Dispose(marchHandle);

        StartCoroutine(ApplyMeshData(mesh, chunkVertices, chunkNormals, chunkTriangles, chunkMeshHandle));

        return chunkMeshHandle;
    }
    
    /// <summary>
    /// Apply mesh data from ChunkMeshJob on chunk mesh
    /// </summary>
    private IEnumerator ApplyMeshData(Mesh mesh, NativeList<Vector3> verts, NativeList<Vector3> normals, NativeParallelHashMap<BiomeHolder, UnsafeList<int>> chunkTriangles, JobHandle job)
    {
        yield return new WaitUntil(() => job.IsCompleted);
        job.Complete();

        if (removeRequest)
        {
            verts.Dispose();
            normals.Dispose();
            foreach (var biomeTriangles in chunkTriangles)
            {
                biomeTriangles.Value.Dispose();
            }
            chunkTriangles.Dispose();
            safeToRemove = true;
            DestroyChunk();
        }
        else
        {
            mesh.subMeshCount = chunkTriangles.Count();
            Material[] chunkMaterials = new Material[chunkTriangles.Count()];

            int chunkMaterialsCount = 0;
            foreach (var biome in chunkTriangles)
            {
                chunkMaterials[chunkMaterialsCount] = BiomeGenerator.Instance.GetBiomeFromId(biome.Key.id).biomeMaterial;
                chunkMaterialsCount++;
            }

            mr.materials = chunkMaterials;
            
            mesh.SetVertices(verts.ToArray());
            mesh.SetNormals(normals.ToArray());

            int biomeCount = 0;
            foreach (var biomeTriangles in chunkTriangles)
            {
                NativeArray<int> biomeTrianglesArray = GetTrianglesFromUnsafeList(biomeTriangles.Value);
                mesh.SetTriangles(biomeTrianglesArray.ToArray(), biomeCount);
                biomeCount++;
                biomeTrianglesArray.Dispose();
            }

            mesh.bounds = new Bounds(new Vector3(((float) MapDataGenerator.ChunkSize) / 2, ((float) MapDataGenerator.chunkHeight)  / 2, ((float) MapDataGenerator.ChunkSize) / 2), 
                new Vector3(MapDataGenerator.ChunkSize, MapDataGenerator.chunkHeight, MapDataGenerator.ChunkSize));

            bounds = mr.bounds;
            
            mc.sharedMesh = mesh;
                
            var colliderJob = new MeshColliderBakeJob()
            {
                meshId = mesh.GetInstanceID()
            };

            colliderJobHandle = colliderJob.Schedule();

            mesh.RecalculateTangents();

            verts.Dispose(colliderJobHandle);
            normals.Dispose(colliderJobHandle);
            
            foreach (var biomeTriangles in chunkTriangles)
            {
                biomeTriangles.Value.Dispose(colliderJobHandle);
            }
            chunkTriangles.Dispose(colliderJobHandle);
            safeToRemove = true;
        }
    }

    private unsafe NativeArray<int> GetTrianglesFromUnsafeList(UnsafeList<int> inputList)
    {
        NativeArray<int> returnArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(inputList.Ptr, inputList.Length, Allocator.None);

        AtomicSafetyHandle safety = AtomicSafetyHandle.Create();
        
        AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(safety);
        AtomicSafetyHandle.UseSecondaryVersion(ref safety);
        AtomicSafetyHandle.SetAllowSecondaryVersionWriting(safety, false);
        
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref returnArray, safety);

        return returnArray;
    }

    //////////////////////////////////////////////////////////////////
    // Debug Methods
    //////////////////////////////////////////////////////////////////

    public void DebugData(NativeArray<float> map, int resolution)
    {
        GameObject g = new GameObject("Chunk map " + position);
        
        for (int idx = 0; idx < map.Length; idx++)
        {
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % MapDataGenerator.chunkHeight;
            int z = idx / (supportedChunkSize * MapDataGenerator.chunkHeight);

            if (y == 48)
            {
                float pointValue =
                    map[
                        x + y * supportedChunkSize +
                        z * supportedChunkSize * MapDataGenerator.chunkHeight];

                if (!float.IsNaN(pointValue))
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.GetComponent<MeshRenderer>().material.color = Color.blue;
                    sphere.transform.position = new Vector3(x-resolution, y-resolution, z-resolution);
                    sphere.transform.localScale = new Vector3(0.25f,0.25f,0.25f);
                    sphere.name = new Vector3(x,y,z).ToString();
                    sphere.transform.parent = g.transform;
                }
            }
            
            
        }
    }

    public void DebugTriangles(NativeArray<Triangle> triangles, NativeParallelHashMap<Edge, float3> vertices)
    {
        var g = new GameObject("Chunk " + position);
        foreach (var vertex in vertices)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.transform.position = new Vector3(vertex.Value.x, vertex.Value.y, vertex.Value.z) + new Vector3(position.x, 0, position.y);
            c.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            c.transform.parent = g.transform;
            c.gameObject.name = vertex.Key.ToString() + " : " + vertex.Key.GetHashCode();

            if (Array.Exists(triangles.ToArray(), triangle => triangle.BorderFromEdge(vertex.Key)))
            {
                c.GetComponent<MeshRenderer>().material.color = Color.red;
            }

        }
    }
    
    // Call to visualize vertices positions
    public void DebugChunk(NativeParallelHashMap<Edge, float3> vertices)
    {
        var g = new GameObject("Info");
        
        var vertexKeyValues = vertices.GetKeyValueArrays(Allocator.Persistent);
        var vertex = vertexKeyValues.Values;
        var vertexIndex = vertexKeyValues.Keys;
        
        for (int i = 0; i < vertexIndex.Length; i++)
        {
            var tmp = new GameObject();
            var tmpc = tmp.AddComponent<TextMeshPro>();
            tmpc.text = vertexIndex[i].ToString();
            tmpc.fontSize = 0.5f;
            tmpc.alignment = TextAlignmentOptions.Midline;
            
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.transform.position = new Vector3(vertex[i].x, vertex[i].y,vertex[i].z);
            c.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            c.transform.parent = g.transform;
            tmp.transform.parent = c.transform;
            tmp.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        vertexKeyValues.Dispose();
    }
}

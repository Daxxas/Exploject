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
        // SetVisible(visible);
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


    [ContextMenu("Unity recalculate normal")]
    public void DebugRecalculateNormals()
    {
        mf.mesh.RecalculateNormals();
    }
    
    //////////////////////////////////////////////////////////////////
    // Jobs
    //////////////////////////////////////////////////////////////////
    
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
            threshold = threshold,
            marchCubeSize = 1
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
}

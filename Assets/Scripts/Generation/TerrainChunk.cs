using System;
using System.Collections;
using System.Numerics;
using DefaultNamespace;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


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

    private CustomSampler sampler;

    private void Start()
    {
        sampler = CustomSampler.Create("TerrainChunk");
    }

    public TerrainChunk InitChunk(Vector2 coord, MapDataGenerator dataGenerator, Transform mapParent)
    {
        mapDataGenerator = dataGenerator;
        position = coord * MapDataGenerator.ChunkSize;
        gameObject.name = "Chunk Terrain " + coord.x + " " + coord.y;
        
        transform.parent = mapParent;
        transform.position = new Vector3(position.x, 0, position.y);

        // Variables that will be filled & passed along jobs
        NativeArray<float> generatedMap = new NativeArray<float>(MapDataGenerator.chunkHeight * MapDataGenerator.supportedChunkSize * MapDataGenerator.supportedChunkSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        var mapDataHandle = mapDataGenerator.GenerateMapData(position, generatedMap);

        GenerateChunkMesh(generatedMap, mapDataHandle);
        
        SetVisible(false);
        
        return this;
    }


    public void UpdateChunkVisibility(float maxViewDst, Vector2 viewerPosition)
    {
        Vector3 transposedViewerPosition = new Vector3(viewerPosition.x, MapDataGenerator.chunkHeight / 2, viewerPosition.y) -
                      transform.position;
        
        float viewerDistanceFromNearEdge = Mathf.Sqrt(bounds.SqrDistance(transposedViewerPosition));
        
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
        else
        {
            StartCoroutine(WaitForInitBeforeVisible(visible));
        }
        
    }

    private IEnumerator WaitForInitBeforeVisible(bool visible)
    {
        yield return new WaitUntil(() => isInit);
        SetVisible(visible);
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
    private NativeParallelHashMap<Edge, float3> uniqueVertices;
    private Mesh.MeshDataArray meshDataArray;
    
    public JobHandle GenerateChunkMesh(NativeArray<float> generatedMap, JobHandle mapDataHandle)
    {
        triangles = new NativeQueue<Triangle>(Allocator.TempJob);
        uniqueVertices = new NativeParallelHashMap<Edge, float3>((MapDataGenerator.chunkHeight * MapDataGenerator.supportedChunkSize * MapDataGenerator.supportedChunkSize), Allocator.Persistent);

        NativeList<int> chunkTriangles = new NativeList<int>(Allocator.TempJob);
        NativeList<Vector3> chunkVertices = new NativeList<Vector3>(Allocator.TempJob);
        NativeList<Vector3> chunkNormals = new NativeList<Vector3>(Allocator.TempJob);
        
        // Generate mesh object with its components
        bounds = new Bounds(new Vector3(((float) MapDataGenerator.ChunkSize) / 2, ((float) MapDataGenerator.chunkHeight)  / 2, ((float) MapDataGenerator.ChunkSize) / 2), 
            new Vector3(MapDataGenerator.ChunkSize , MapDataGenerator.chunkHeight, MapDataGenerator.ChunkSize));
        // gameObject.AddComponent<BoundGizmo>();

        Mesh mesh = new Mesh();

        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
        mr.material = meshMaterial;

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
            vertices = uniqueVertices.AsParallelWriter(),
        };
        
        JobHandle marchHandle = marchJob.Schedule(generatedMap.Length, 100, mapDataHandle);

        // marchHandle.Complete();
        //
        // DebugData(generatedMap); 
        // DebugTriangles(triangles.ToArray(Allocator.Persistent), uniqueVertices); 
        
        // Generate Mesh from Marching cube result
        var chunkMeshJob = new ChunkMeshJob()
        {
            triangles = triangles,
            uniqueVertices = uniqueVertices,
            map = generatedMap,
            chunkTriangles = chunkTriangles,
            chunkNormals = chunkNormals,
            chunkVertices = chunkVertices
        };

        var chunkMeshHandle = chunkMeshJob.Schedule(marchHandle);
        
        
        triangles.Dispose(chunkMeshHandle);
        uniqueVertices.Dispose(chunkMeshHandle);
        generatedMap.Dispose(chunkMeshHandle);

        StartCoroutine(ApplyMeshData(mesh, chunkVertices, chunkNormals, chunkTriangles, mc, chunkMeshHandle));
        

        
        return chunkMeshHandle;
    }
    
    private IEnumerator ApplyMeshData(Mesh mesh, NativeList<Vector3> verts, NativeList<Vector3> normals, NativeList<int> tris, MeshCollider mc, JobHandle job)
    {
        yield return new WaitUntil(() => job.IsCompleted);
        job.Complete();
        
        
        mesh.SetVertices(verts.ToArray());
        mesh.SetNormals(normals.ToArray());
        mesh.SetTriangles(tris.ToArray(),0);
        
        mesh.bounds = new Bounds(new Vector3(((float) MapDataGenerator.ChunkSize) / 2, ((float) MapDataGenerator.chunkHeight)  / 2, ((float) MapDataGenerator.ChunkSize) / 2), 
            new Vector3(MapDataGenerator.ChunkSize , MapDataGenerator.chunkHeight, MapDataGenerator.ChunkSize));

            
        var colliderJob = new MeshColliderBakeJob()
        {
            meshId = mesh.GetInstanceID()
        };

        JobHandle colliderJobHandle = colliderJob.Schedule();
        isInit = true;
        
        mesh.RecalculateTangents();

        sampler.Begin();
        verts.Dispose(colliderJobHandle);
        normals.Dispose(colliderJobHandle);
        tris.Dispose(colliderJobHandle);
        sampler.End();
    }


    public void DebugData(NativeArray<float> map)
    {
        GameObject g = new GameObject("Chunk map " + position);
        
        for (int idx = 0; idx < map.Length; idx++)
        {
            int x = idx % MapDataGenerator.supportedChunkSize;
            int y = (idx / MapDataGenerator.supportedChunkSize) % MapDataGenerator.chunkHeight;
            int z = idx / (MapDataGenerator.supportedChunkSize * MapDataGenerator.chunkHeight);

            if (y == 48)
            {
                float pointValue =
                    map[
                        x + y * MapDataGenerator.supportedChunkSize +
                        z * MapDataGenerator.supportedChunkSize * MapDataGenerator.chunkHeight];

                if (!float.IsNaN(pointValue))
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.GetComponent<MeshRenderer>().material.color = Color.blue;
                    sphere.transform.position = new Vector3(x-MapDataGenerator.resolution, y-MapDataGenerator.resolution, z-MapDataGenerator.resolution);
                    sphere.transform.localScale = new Vector3(0.25f,0.25f,0.25f);
                    sphere.name = new Vector3(x,y,z).ToString();
                    sphere.transform.parent = g.transform;
                }
            }
            
            
        }
    }

    private void OnDestroy() 
    {
        if (!isInit)
        {
            try
            {
                triangles.Dispose();
                uniqueVertices.Dispose();
            }
            catch (Exception e)
            {
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

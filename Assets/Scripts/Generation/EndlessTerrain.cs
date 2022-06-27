using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using Priority_Queue;
using Unity.Collections;

public class EndlessTerrain : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private int chunkViewDistance = 8;
    private int unitViewDistance => chunkViewDistance * MapDataGenerator.ChunkSize;
    [SerializeField] private int farChunkViewDistance = 100;
    [SerializeField] private int maxChunksPerFrame = 5;
    
    [Header("References")]
    [SerializeField] private Transform viewer;
    [SerializeField] private MapDataGenerator dataGenerator;
    [SerializeField] private GameObject chunkObject;
    [SerializeField] private Transform mapParent;
    
    [ShowInInspector] public static Vector2 viewerPosition;
    
    private Dictionary<Vector2, TerrainChunk> terrainChunkDic = new Dictionary<Vector2, TerrainChunk>();
    private FastPriorityQueue<ChunkPos> chunkToLoadQueue = new FastPriorityQueue<ChunkPos>(15000);
    private Queue<ChunkPos> chunkToRemove = new Queue<ChunkPos>();
    
    private Vector2 viewerChunkPos;

    private int unitFarViewDistance => farChunkViewDistance * MapDataGenerator.ChunkSize;
    
    public static NativeArray<int> cornerIndexAFromEdge;
    public static NativeArray<int> cornerIndexBFromEdge;
    public static NativeArray<int> triangulation1D;

    private void Start()
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
        
        UpdateViewerPos();
    }

    private void Update()
    {
        UpdateViewerPos();
        
        ClearChunksToRemove();

        UpdateChunksPriorities();
        
        UpdateVisibleChunks();

        GenerateChunksForFrame();
    }

    /// <summary>
    /// Update the viewers coordinates in chunk coords & normal coords
    /// </summary>
    private void UpdateViewerPos()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        viewerChunkPos = new Vector2(Mathf.RoundToInt(viewerPosition.x / MapDataGenerator.ChunkSize),
            Mathf.RoundToInt(viewerPosition.y / MapDataGenerator.ChunkSize));
    }
    
    /// <summary>
    /// Dequeue the chunks to generate a limited number of time for a frame
    /// </summary>
    private void GenerateChunksForFrame()
    {
        if (chunkToLoadQueue.Count > 0)
        {
            var currentChunkPos = chunkToLoadQueue.Dequeue();
            int i = 0;
            while(i < maxChunksPerFrame)
            {
                currentChunkPos.chunk.InitChunk(currentChunkPos.pos, dataGenerator, dataGenerator.resolution);
                i++;
            }
        }
    }
    
    /// <summary>
    /// Update chunks visibility & calls the creation of chunks if needed
    /// </summary>
    private void UpdateVisibleChunks()
    {
        foreach (var chunk in terrainChunkDic)
        {
            chunk.Value.UpdateChunk(unitViewDistance, viewerPosition);
        }

        for (int zOffset = -chunkViewDistance; zOffset <= chunkViewDistance ; zOffset++)
        {
            for (int xOffset = -chunkViewDistance; xOffset <= chunkViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(viewerChunkPos.x + xOffset, viewerChunkPos.y + zOffset);
                
                if (!terrainChunkDic.ContainsKey(viewedChunkCoord))
                {
                    CreateNewChunk(viewedChunkCoord);
                }
            }
        }
    }

    /// <summary>
    /// Creates a new chunk gameobject at a given position and enqueue it to the chunk to generate
    /// </summary>
    /// <param name="chunkCoord">Chunk coordinates</param>
    private void CreateNewChunk(Vector2 chunkCoord)
    {
        var instantiatedChunk = Instantiate(chunkObject, transform);
        instantiatedChunk.name = $"Chunk Terrain {chunkCoord.x} {chunkCoord.y}";
        var chunk = instantiatedChunk.GetComponent<TerrainChunk>();
        var chunkPos = new ChunkPos()
        {
            pos = chunkCoord,
            chunk = chunk
        };
        chunkToLoadQueue.Enqueue(chunkPos, Vector2.Distance(viewerChunkPos, viewerPosition));
        terrainChunkDic.Add(chunkCoord, chunk);
    }
    
    /// <summary>
    /// Dequeue the chunks to remove
    /// </summary>
    private void ClearChunksToRemove()
    {
        while (chunkToRemove.TryDequeue(out var chunkPos))
        {
            if (chunkToLoadQueue.Contains(chunkPos))
            {
                chunkToLoadQueue.Remove(chunkPos);
            }

            chunkPos.chunk.DestroyChunk();
            terrainChunkDic.Remove(chunkPos.pos);
        }
    }

    /// <summary>
    /// Updates all the chunks to generate priorities in function of the distance from the player
    /// </summary>
    private void UpdateChunksPriorities()
    {
        foreach (var chunkPos in chunkToLoadQueue)
        {
            float chunkDistance = Vector2.Distance(chunkPos.pos, viewerChunkPos);
            if (chunkDistance <= unitViewDistance)
            {
                chunkToLoadQueue.UpdatePriority(chunkPos, Vector2.Distance(chunkPos.pos, viewerChunkPos));
            }
            else
            {
                chunkToRemove.Enqueue(chunkPos);
            }
        }
    }
    
    
    private void OnDestroy()
    {
        foreach (var terrainChunk in terrainChunkDic)
        {
            terrainChunk.Value.marchHandle.Complete();
            terrainChunk.Value.chunkMeshJob.Complete();
        }
        
        if(cornerIndexAFromEdge.IsCreated) cornerIndexAFromEdge.Dispose();
        if(cornerIndexBFromEdge.IsCreated) cornerIndexBFromEdge.Dispose();
        if(triangulation1D.IsCreated) triangulation1D.Dispose();
    }
    
    private void OnDrawGizmos()
    {
        // int currentChunkCoordX = Mathf.RoundToInt((viewerPosition.x / chunkSize)) * chunkSize  + chunkSize/2;
        // int currentChunkCoordZ = Mathf.RoundToInt((viewerPosition.y / chunkSize)) * chunkSize  + chunkSize/2;
        //
        // Vector3 cubepos = new Vector3(currentChunkCoordX, 128, currentChunkCoordZ);
        // Handles.Label(cubepos + Vector3.up * 3, $"{cubepos}");
        // Gizmos.color = Color.magenta;
        // Gizmos.DrawWireCube(cubepos, new Vector3(chunkSize,256,chunkSize));
    }

    private class ChunkPos : FastPriorityQueueNode
    {
        public Vector2 pos;
        public TerrainChunk chunk; 
    }
}
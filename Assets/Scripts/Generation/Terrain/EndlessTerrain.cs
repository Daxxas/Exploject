using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using Priority_Queue;
using Unity.Collections;
using Vector2 = UnityEngine.Vector2;

public class EndlessTerrain : MonoBehaviour
{
    private static EndlessTerrain instance;
    public static EndlessTerrain Instance => instance;

    [Header("Parameters")]
    [SerializeField] private int chunkViewDistance = 8;

    public int ChunkViewDistance => chunkViewDistance;
    public int UnitViewDistance => chunkViewDistance * MapDataGenerator.ChunkSize;
    public int FarChunkViewDistance => ChunkViewDistance + farChunkViewDistance;
    public int UnitFarChunkViewDistance => FarChunkViewDistance * MapDataGenerator.ChunkSize;

    [SerializeField] private int farChunkViewDistance = 100;
    [SerializeField] private int maxChunksPerFrame = 5;
    
    [Header("References")]
    [SerializeField] private Transform viewer;
    [SerializeField] private GameObject chunkObject;
    [SerializeField] private Transform mapParent;
    
    [SerializeField] public static Vector2 viewerPosition;
    
    private Dictionary<Vector2, TerrainChunk> terrainChunkDic = new Dictionary<Vector2, TerrainChunk>();
    private FastPriorityQueue<ChunkPos> chunkLoadQueue = new FastPriorityQueue<ChunkPos>(15000);
    private Queue<ChunkPos> chunkToStopLoad = new Queue<ChunkPos>();
    private Queue<Vector2> chunkToRemove = new Queue<Vector2>();
    
    private Vector2 viewerChunkPos;
    public Vector2 ViewerChunkPos => viewerChunkPos;

    private int unitFarViewDistance => farChunkViewDistance * MapDataGenerator.ChunkSize;
    
    public static NativeArray<int> cornerIndexAFromEdge;
    public static NativeArray<int> cornerIndexBFromEdge;
    public static NativeArray<int> triangulation1D;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }
        
    }

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
        viewerChunkPos = new Vector2(Mathf.RoundToInt(viewerPosition.x / MapDataGenerator.ChunkSize), Mathf.RoundToInt(viewerPosition.y / MapDataGenerator.ChunkSize));
    }
    
    /// <summary>
    /// Dequeue the chunks to generate a limited number of time for a frame
    /// </summary>
    private void GenerateChunksForFrame()
    {
        if (chunkLoadQueue.Count > 0)
        {
            var currentChunkPos = chunkLoadQueue.Dequeue();
            int i = 0;
            while(i < maxChunksPerFrame)
            {
                currentChunkPos.chunk.InitChunk(currentChunkPos.pos, MapDataGenerator.Instance.resolution);
                i++;
            }
        }
    }
    
    /// <summary>
    /// Update chunks visibility & initiate the creation of chunks if needed
    /// </summary>
    private void UpdateVisibleChunks()
    {
        foreach (var chunk in terrainChunkDic)
        {
            chunk.Value.UpdateVisibility();
            
            // If chunk is too far, delete it completely
            float chunkDistance = Vector2.Distance(chunk.Key, viewerChunkPos);
            if (chunkDistance > FarChunkViewDistance)
            {
                chunkToRemove.Enqueue(chunk.Key);
            }
        }

        // TODO : Load chunk in circle instead of square
        for (int zOffset = -chunkViewDistance; zOffset <= chunkViewDistance; zOffset++)
        {
            for (int xOffset = -chunkViewDistance; xOffset <= chunkViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(viewerChunkPos.x + xOffset, viewerChunkPos.y + zOffset);
                float chunkDistance = Vector2.Distance(viewedChunkCoord, viewerChunkPos);
                if (!terrainChunkDic.ContainsKey(viewedChunkCoord) && chunkDistance <= ChunkViewDistance)
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
        // Generate chunk object
        var instantiatedChunk = Instantiate(chunkObject, mapParent);
        instantiatedChunk.name = $"Chunk Terrain {chunkCoord.x} {chunkCoord.y}";
        var chunk = instantiatedChunk.GetComponent<TerrainChunk>();
        terrainChunkDic.Add(chunkCoord, chunk);
        
        // Queue chunk to load queue
        var chunkPos = new ChunkPos()
        {
            pos = chunkCoord,
            chunk = chunk
        };
        chunkLoadQueue.Enqueue(chunkPos, Vector2.Distance(viewerChunkPos, viewerPosition));
    }
    
    /// <summary>
    /// Removes chunks objects when they are too far/don't need to be loaded anymore
    /// </summary>
    private void ClearChunksToRemove()
    {
        // Remove chunks in load queue that are useless to load
        while (chunkToStopLoad.TryDequeue(out var chunkPos))
        {
            if (chunkLoadQueue.Contains(chunkPos))
            {
                chunkLoadQueue.Remove(chunkPos);
            }
            terrainChunkDic.Remove(chunkPos.pos);
            chunkPos.chunk.DestroyChunk();
        }

        // Remove chunks that are too far from player
        while (chunkToRemove.TryDequeue(out var pos))
        {
            if (terrainChunkDic.ContainsKey(pos))
            {
                terrainChunkDic[pos].DestroyChunk();
                terrainChunkDic.Remove(pos);
            }
        }
    }

    /// <summary>
    /// Updates all the chunks priorities in the load queue in function of the distance from the player
    /// </summary>
    private void UpdateChunksPriorities()
    {
        foreach (var chunkPos in chunkLoadQueue)
        {
            float chunkDistance = Vector2.Distance(chunkPos.pos, viewerChunkPos);
            // Update priority
            if (chunkDistance <= ChunkViewDistance)
            {
                chunkLoadQueue.UpdatePriority(chunkPos, chunkDistance);
            }
            // Chunk (in load queue) is too far, there's no need to load it anymore
            else
            {
                chunkToStopLoad.Enqueue(chunkPos);
            }
        }
    }

    private void GetBiomeForPos(Vector2 pos)
    {
        
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
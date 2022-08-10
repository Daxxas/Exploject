using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Priority_Queue;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class FixedTerrain : MonoBehaviour
{
    [SerializeField] private int terrainSize = 10;
    [SerializeField] private Transform terrainParent;
    [SerializeField] private GameObject chunkObject;
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private bool performanceMode = true;
    [SerializeField] private int maxChunkPerFrame = 2;
    private List<TerrainChunk> chunks = new List<TerrainChunk>();
    private Queue<ChunkPos> chunkPosQueue = new Queue<ChunkPos>();

    private Timer generationTimer;
    
    private void Awake()
    {
        TerrainChunk.InitMarchCubeArrays();
    }

    private void OnDestroy()
    {
        TerrainChunk.DisposeMarchCubeArrays();

        MapDataGenerator.Instance.Dispose();
    }

    private void Start()
    {
        // GenerateTerrain();
    }

    private void Update()
    {
        int chunkThisFrame = 0;

        while (chunkPosQueue.Count > 0 && chunkThisFrame < maxChunkPerFrame)
        {
            var chunkPos = chunkPosQueue.Dequeue();
            chunkPos.chunk.InitChunk(chunkPos.pos, MapDataGenerator.resolution);
            chunkThisFrame++;
        }
    }

    [Button]
    public void GenerateTerrain()
    {
        if (performanceMode)
        {
            GenerateTerrainFastMode();
        }
        else
        {
            GenerateTerrainTimerMode();
        }
        
    }
    
    
    public void GenerateTerrainFastMode()
    {
        ClearTerrain();
        var watch = new Stopwatch();
        watch.Start();
            
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                Vector2 chunkCoord = new Vector2(x, z) + offset;
                
                var instantiatedChunk = Instantiate(chunkObject, terrainParent);
                var chunk = instantiatedChunk.GetComponent<TerrainChunk>();
                var chunkPos = new ChunkPos()
                {
                    pos = chunkCoord,
                    chunk = chunk
                };
                chunkPosQueue.Enqueue(chunkPos);
                
                chunks.Add(chunk);
            }
        }
        
        watch.Stop();

        float miliseconds =  watch.ElapsedMilliseconds / 1000f;
        
        Debug.Log( $"Generated {terrainSize * terrainSize} chunks in " + miliseconds + " seconds");
    }
    
    public void GenerateTerrainTimerMode()
    {
        ClearTerrain();
        var watch = new Stopwatch();
        watch.Start();
            
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                Vector2 chunkCoord = new Vector2(x, z) + offset;
                
                var instantiatedChunk = Instantiate(chunkObject, terrainParent);
                var chunk = instantiatedChunk.GetComponent<TerrainChunk>();
                chunk.InitChunk(chunkCoord, MapDataGenerator.resolution);
                chunks.Add(chunk);
            }
        }
        watch.Stop();

        float miliseconds =  watch.ElapsedMilliseconds / 1000f;
        
        Debug.Log( $"Generated {terrainSize * terrainSize} chunks in " + miliseconds + " seconds");
    }

    private void ClearTerrain()
    {
        foreach (var chunk in chunks)
        {
            Destroy(chunk.gameObject);
        }
        
        chunks.Clear();
        chunkPosQueue.Clear();
    }
}
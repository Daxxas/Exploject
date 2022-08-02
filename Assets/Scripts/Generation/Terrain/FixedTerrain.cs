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
    private Queue<ChunkPos> chunkLoadQueue = new Queue<ChunkPos>();

    private List<TerrainChunk> chunks = new List<TerrainChunk>();

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
        GenerateTerrain();
    }

    private void Update()
    {
        // int chunkLoadCount = 0;
        // while (chunkLoadQueue.Count > 0 && chunkLoadCount < maxChunkPerFrame)
        // {
        //     ChunkPos chunkPos = chunkLoadQueue.Dequeue();
        //     chunkPos.chunk.InitChunk(chunkPos.pos, MapDataGenerator.Instance.resolution);
        //     chunkLoadCount++;
        // }
    }
    
    [Button]
    public void GenerateTerrain()
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
                chunk.InitChunk(chunkCoord, MapDataGenerator.Instance.resolution);
                chunks.Add(chunk);
                // chunkLoadQueue.Enqueue(chunkPos);
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
    }
}
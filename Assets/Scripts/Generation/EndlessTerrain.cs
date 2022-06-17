using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    [SerializeField] private int chunkViewDistance = 8;
    [SerializeField] private Transform viewer;
    [SerializeField] private MapDataGenerator dataGenerator;
    [SerializeField] private GameObject chunkObject;
    [SerializeField] private Transform mapParent;
    
    [ShowInInspector] public static Vector2 viewerPosition;
    private int chunkSize;
    private int chunkHeight;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDic = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    private void Start()
    {
        chunkSize = MapDataGenerator.chunkSize; 
        chunkHeight = MapDataGenerator.chunkHeight; 
        // Debug.Log($"chunksVisibleInViewDst: {chunksVisibleInViewDst}");
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }


    private void UpdateVisibleChunks()
    {
        foreach (var terrainChunk in terrainChunksVisibleLastUpdate)
        {
            // terrainChunk.SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int zOffset = -chunkViewDistance; zOffset <= chunkViewDistance ; zOffset++)
        {
            for (int xOffset = -chunkViewDistance; xOffset <= chunkViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordZ + zOffset);
                
                if (terrainChunkDic.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDic[viewedChunkCoord].UpdateChunkVisibility(chunkViewDistance * MapDataGenerator.chunkSize, viewerPosition);
                    if (terrainChunkDic[viewedChunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDic[viewedChunkCoord]);
                    }
                }
                else
                {
                    var instantiatedChunk = Instantiate(chunkObject);
                    terrainChunkDic.Add(viewedChunkCoord, instantiatedChunk.GetComponent<TerrainChunk>().InitChunk(viewedChunkCoord, dataGenerator, mapParent));
                }
            }
        }
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
}
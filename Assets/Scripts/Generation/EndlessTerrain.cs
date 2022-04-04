using System;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    [SerializeField] private float maxViewDst = 8;
    [SerializeField] private Transform viewer;
    [SerializeField] private MapGenerator generator;
    [SerializeField] private Transform mapParent;
    
    public static Vector2 viewerPosition;
    private int chunkSize;
    private int chunksVisibleInViewDst;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDic = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    private void Start()
    {
        chunkSize = generator.chunkSize; 
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        Debug.Log($"chunksVisibleInViewDst: {chunksVisibleInViewDst}");
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }


    private void UpdateVisibleChunks()
    {
        // foreach (var terrainChunk in terrainChunksVisibleLastUpdate)
        // {
        //     terrainChunk.SetVisible(false);
        // }
        // terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int zOffset = -chunksVisibleInViewDst; zOffset <= chunksVisibleInViewDst ; zOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordZ + zOffset);
                
                if (terrainChunkDic.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDic[viewedChunkCoord].UpdateChunk();
                    if (terrainChunkDic[viewedChunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDic[viewedChunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDic.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, generator, mapParent));
                }
            }
        }
    }
}
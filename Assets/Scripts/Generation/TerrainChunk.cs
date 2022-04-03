using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    private MapGenerator generator;
    private Vector2 position;
    private GameObject chunkObject;
    private Bounds bounds;
        
    public TerrainChunk(Vector2 coord, int size, MapGenerator generator, Transform mapParent)
    {
        this.generator = generator;
        position = coord * size;
        bounds = new Bounds(position, Vector2.one * size);
        Vector3 positionV3 = new Vector3(position.x, 0, position.y);
        chunkObject = new GameObject("Chunk Terrain");
        chunkObject.transform.parent = mapParent;
        chunkObject.transform.position = positionV3;
        // SetVisible(false);
            
        generator.RequestMapData(OnMapDataReceive, position);
    }

    void OnMapDataReceive(MapData mapData)
    {
        List<CombineInstance> blockData = generator.CreateMeshData(mapData.noiseMap);
        
        var blockDataLists = generator.SeparateMeshData(blockData);
        
        generator.CreateMesh(blockDataLists, chunkObject.transform);
    }

    public void UpdateChunk()
    {
        // float viewerDistanceFromNearEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
        // bool visible = viewerDistanceFromNearEdge <= maxViewDst;
        // SetVisible(visible);
    }

    public void SetVisible(bool visible)
    {
        chunkObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return chunkObject.activeSelf;
    }
}
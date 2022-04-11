using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

public class TerrainChunk
{
    private MapGenerator generator;
    private Vector2 position;
    private GameObject chunkObject;
    private Bounds bounds;

    
    [BurstCompile(CompileSynchronously = true)]
    public struct GenerateJob : IJob
    {
        public Vector3 position;
        public MapGenerator generator;
        public MapData mapData;
        public GameObject chunkObject;
        public void Execute()
        {
            MapData mapData = generator.GenerateMapData(position);
            
            List<CombineInstance> blockData = generator.CreateMeshData(mapData.noiseMap);
        
            var blockDataLists = generator.SeparateMeshData(blockData);
        
            generator.CreateMesh(blockDataLists, chunkObject.transform);
        }
    }
    
    // Called by Endless Terrain when it needs a new chunk
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
        
        GenerateJob job = new GenerateJob()
        {
            position = position,
            generator = generator,
            chunkObject = chunkObject,
        };
        
        job.Execute();
        // Ask generator to get MapData with RequestMapData & start generating mesh with OnMapDataReceive
        // generator.RequestMapData(OnMapDataReceive, position);
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
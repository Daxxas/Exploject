using System.Collections.Generic;
using UnityEngine;

public class FiniteTerrain : MonoBehaviour
{
    [SerializeField] private int previewSize;
    [SerializeField] private int increment;
    [SerializeField] private MapGenerator generator;
    [SerializeField] private Transform mapParent;   
    
    private Dictionary<Vector2, TerrainChunk> terrainChunkDic = new Dictionary<Vector2, TerrainChunk>();

    [ContextMenu(nameof(GenerateTerrain))]
    public void GenerateTerrain()
    {
        for (int x = 0; x < previewSize; x++)
        {
            new FiniteTerrainChunk(new Vector2(x, 0), new Vector2(0, increment), new Vector2(x, previewSize-1),
                generator.chunkSize, generator, mapParent);
        }
    }

    public void ResetTerrain()
    {
        for (int i = mapParent.childCount - 1; i >= 0; i--)
        {
            Destroy(mapParent.GetChild(i).gameObject);
        }
    }
    
    public class FiniteTerrainChunk
    {
        private MapGenerator generator;
        private Vector2 position;
        private Vector2 increment;
        private Vector2 chunkCoord;
        private Vector2 nextChunkCoord;
        private Vector2 maxCoord;
        private GameObject chunkObject;
        private Bounds bounds;
        private Transform mapParent;
        
        
        public FiniteTerrainChunk(Vector2 coord, Vector2 increment, Vector2 maxCoord, int size, MapGenerator generator, Transform mapParent)
        {
            this.generator = generator;
            this.maxCoord = maxCoord;
            this.chunkCoord = coord;
            this.increment = increment;
            this.mapParent = mapParent;
            
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            chunkObject = new GameObject("Chunk Terrain");
            chunkObject.transform.parent = mapParent;
            chunkObject.transform.position = positionV3;
            
            generator.RequestMapData(OnMapDataReceive, position);
        }

        void OnMapDataReceive(MapData mapData)
        {
            List<CombineInstance> blockData = generator.CreateMeshData(mapData.noiseMap);
        
            var blockDataLists = generator.SeparateMeshData(blockData);
        
            generator.CreateMesh(blockDataLists, chunkObject.transform);

            if (!chunkCoord.Equals(maxCoord))
            {
                new FiniteTerrainChunk(chunkCoord + increment, increment, maxCoord, generator.chunkSize, generator, mapParent);
            }
        }
    }
}

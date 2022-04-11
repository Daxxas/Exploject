using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private GameObject cube;
    [SerializeField] private Material meshMaterial;
    [SerializeField] private GameObject waterPreview;
    public float waterLevel;
    private bool waterOn = true;
    
    public int seed;
    
    public int chunkSize = 16;
    public int supportedChunkSize => chunkSize + 2;
    
    // Chunk idea 
    // 0  1  2  3  4  5  6  7  8  9 10 12 13 14 15 16 17
    // .  *  *  *  *  *  *  *  *  *  *  *  *  *  *  *  .
    // 
    
    public int chunkHeight;

    private const float threshold = 0;
    
    private List<Mesh> meshes = new List<Mesh>();
    
    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();

    // private EquationHandler equationHandler;

    public struct GenerateMapDataJob : IJob
    {
        public Vector2 position;
        public int supportedChunkSize;
        public int chunkHeight;
        public NativeArray<float> returnMap;
        public void Execute()
        {
            GenerateMapData(position);
        }
        
        private void GenerateMapData(Vector2 offset)
        {
            for (int x = 0; x < supportedChunkSize; x++)
            {
                for (int z = 0; z < supportedChunkSize; z++)
                {
                    for (int y = 0; y < chunkHeight; y++)
                    {
                        try
                        {
                            returnMap[x + supportedChunkSize * (y + chunkHeight * z)] = VanillaFunction.GetResult(x + offset.x, y, z + offset.y);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                            Debug.LogError($"x: {x}, y: {y}, z: {z}");
                        }
                    }
                }
            }
        }
    }

    public struct GenerateChunkMeshJob : IJob
    {
        public void Execute()
        {
            
        }
        
        public NativeArray<CombineInstance> CreateMeshData(NativeArray<float> map)
        {
            List<CombineInstance> blockData = new List<CombineInstance>();

            MeshFilter blockMesh = Instantiate(cube, Vector3.zero, Quaternion.identity).GetComponent<MeshFilter>();

            for (int x = 0; x < supportedChunkSize-2; x++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    for (int z = 0; z < supportedChunkSize-2; z++)
                    {
                        int chunkBlockX = x + 1;
                        int chunkBlockZ = z + 1;
                    
                        float noiseBlock = map[chunkBlockX, y, chunkBlockZ];

                        if (noiseBlock > threshold && IsBorder(chunkBlockX,y,chunkBlockZ,threshold,map))
                        {
                            blockMesh.transform.position = new Vector3(chunkBlockX, y, chunkBlockZ);
                            // Set mesh vertices
                            List<Vector3> vertices = new List<Vector3>();
                            // Center
                            vertices.Add(new Vector3(chunkBlockX, y, chunkBlockZ));
                            // Borders
                            Mesh mesh = new Mesh();

                            mesh.SetVertices(vertices);
                            CombineInstance ci = new CombineInstance()
                            {
                                mesh = blockMesh.mesh,
                                transform = blockMesh.transform.localToWorldMatrix
                            };
                        
                            blockData.Add(ci);
                        }
                    }
                }
            }
        
            DestroyImmediate(blockMesh.gameObject);

            return blockData;
        }
    }
    
    //Temp
    private void Awake()
    {
        PlainNoise.SetupNoise(seed);
    }

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                lock (mapDataThreadInfoQueue)
                {
                    MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }
    }

    // Called when TerrainChunk is initialized 
    public void RequestMapData(Action<MapData> callback, Vector2 offset)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback, offset);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Action<MapData> callback, Vector2 offset)
    {
        MapData mapData = GenerateMapData(offset);
        
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
        
    }
    
    // Called in thread from RequestMapData 
    private MapData GenerateMapData(Vector2 offset)
    {
        float[,,] finalMap = new float[supportedChunkSize, chunkHeight, supportedChunkSize];
        for (int x = 0; x < supportedChunkSize; x++)
        {
            for (int z = 0; z < supportedChunkSize; z++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    try
                    {
                        finalMap[x, y, z] = VanillaFunction.GetResult(x + offset.x, y, z + offset.y);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        Debug.LogError($"x: {x}, y: {y}, z: {z}");
                    }
                }
            }
        }

        return new MapData(finalMap);
    }

    #region Mesh Handling
    
    // Called when TerrainChunk receives MapData with OnMapDataReceive
    public List<CombineInstance> CreateMeshData(float[,,] map)
    {
        List<CombineInstance> blockData = new List<CombineInstance>();

        MeshFilter blockMesh = Instantiate(cube, Vector3.zero, Quaternion.identity).GetComponent<MeshFilter>();

        for (int x = 0; x < supportedChunkSize-2; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < supportedChunkSize-2; z++)
                {
                    int chunkBlockX = x + 1;
                    int chunkBlockZ = z + 1;
                    
                    float noiseBlock = map[chunkBlockX, y, chunkBlockZ];

                    if (noiseBlock > threshold && IsBorder(chunkBlockX,y,chunkBlockZ,threshold,map))
                    {
                        blockMesh.transform.position = new Vector3(chunkBlockX, y, chunkBlockZ);
                        // Set mesh vertices
                        List<Vector3> vertices = new List<Vector3>();
                        // Center
                        vertices.Add(new Vector3(chunkBlockX, y, chunkBlockZ));
                        // Borders
                        Mesh mesh = new Mesh();

                        mesh.SetVertices(vertices);
                        CombineInstance ci = new CombineInstance()
                        {
                            mesh = blockMesh.mesh,
                            transform = blockMesh.transform.localToWorldMatrix
                        };
                        
                        blockData.Add(ci);
                    }
                }
            }
        }
        
        DestroyImmediate(blockMesh.gameObject);

        return blockData;
    }

    // Called by TerrainChunk after CreateMeshData
    public List<List<CombineInstance>> SeparateMeshData(List<CombineInstance> blockData)
    {
        List<List<CombineInstance>> blockDataLists = new List<List<CombineInstance>>();
        int vertexCount = 0;
        blockDataLists.Add(new List<CombineInstance>());

        for (int i = 0; i < blockData.Count; i++)
        {
            vertexCount += blockData[i].mesh.vertexCount;
            if (vertexCount > 65536)
            {
                vertexCount = 0;
                blockDataLists.Add(new List<CombineInstance>());
                i--;
            }
            else
            {
                blockDataLists.Last().Add(blockData[i]);
            }
        }

        return blockDataLists;
    }

    // Called by TerrainChunk after SeparateMeshData
    public void CreateMesh(List<List<CombineInstance>> blockDataLists, Transform parent)
    {
        foreach (List<CombineInstance> data in blockDataLists)
        {
            GameObject g = new GameObject($"Chunk Part {blockDataLists.IndexOf(data)}");
            g.transform.parent = parent;
            g.transform.localPosition = Vector3.zero;
            MeshFilter mf = g.AddComponent<MeshFilter>();
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterial = meshMaterial;
            mf.mesh.CombineMeshes(data.ToArray());

            meshes.Add(mf.mesh);

            g.AddComponent<MeshCollider>();
        }
    }

    private bool IsBorder(int x, int y, int z, float threshold, float[,,] map)
    {
        bool isBorder = false;

        if (map[x, y, z] > threshold)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                // int currentX = (x + xOffset < supportedChunkSize && x + xOffset > 0) ? xOffset : 0;

                if (map[x + xOffset, y, z] < threshold)
                {
                    isBorder = true;
                }
            }
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                int currentY = (y + yOffset < chunkHeight && y + yOffset > 0) ? yOffset : 0;

                if (map[x, y + currentY, z] < threshold)
                {
                    isBorder = true;
                }
            }

            for (int zOffset = -1; zOffset <= 1; zOffset++)
            {
                // int currentZ = (z + zOffset < supportedChunkSize && z + zOffset > 0) ? zOffset : 0;

                if (map[x, y , z + zOffset] < threshold)
                {
                    isBorder = true;
                }
            }

        }
        return isBorder;
    }

    private LinkedList<Vector3> GetNeighbors(int x, int y, int z, float[,,] map, float threshold)
    {
        LinkedList<Vector3> neighbors = new LinkedList<Vector3>();
        
        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                int currentY = (y + yOffset < chunkHeight && y + yOffset > 0) ? yOffset : 0;

                for (int zOffset = -1; zOffset <= 1; zOffset++)
                {
                    if (IsBorder(x + xOffset, y + currentY, z + zOffset, threshold, map))
                    {
                        neighbors.AddLast(new Vector3(x + xOffset, y + currentY, z + zOffset));
                    }
                }

            }
        }

        return neighbors;
    }

    #endregion


    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public struct MapData
{
    public float[,,] noiseMap;

    public MapData(float[,,] noiseMap)
    {
        this.noiseMap = noiseMap;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    [SerializeField] private int chunkHeight;

    private const float threshold = 0;
    
    private List<Mesh> meshes = new List<Mesh>();

    [SerializeField] private Gradient gradient;
    
    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();

    // private EquationHandler equationHandler;


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
    
    private MapData GenerateMapData(Vector2 offset)
    {
        float[,,] finalMap = new float[chunkSize, chunkHeight, chunkSize];
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkHeight; y++)
                {
                    finalMap[x, y, z] = VanillaFunction.GetResult(x+offset.x,y,z+offset.y);
                }
            }
        }

        return new MapData(finalMap);
    }

    public void ShowWater()
    {
        waterOn = !waterOn;
        waterPreview.SetActive(waterOn);
    }
    
    public void SetWaterLevel(float level)
    {
        waterLevel = level;
    }
    
    public void UpdateWaterPreview()
    {
        waterPreview.transform.position = new Vector3((chunkSize / 2), (waterLevel/2), (chunkSize / 2));
        waterPreview.transform.localScale = new Vector3(chunkSize - 1.01f, waterLevel, chunkSize + .99f);
    }
    

    #region Mesh Handling
    
    public List<CombineInstance> CreateMeshData(float[,,] map)
    {
        List<CombineInstance> blockData = new List<CombineInstance>();

        MeshFilter blockMesh = Instantiate(cube, Vector3.zero, Quaternion.identity).GetComponent<MeshFilter>();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    float noiseBlock = map[x, y, z];


                    // if (x == 0 && z == 0)
                    // {
                    //     Debug.Log(map[x,y,z]);
                    // }
                    
                    if (noiseBlock > threshold)
                    {
                        blockMesh.transform.position = new Vector3(x, y, z);
                        
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
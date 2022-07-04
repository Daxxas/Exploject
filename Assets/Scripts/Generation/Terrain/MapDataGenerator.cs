using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class MapDataGenerator : MonoBehaviour
{
    private static MapDataGenerator instance;
    public static MapDataGenerator Instance => instance;

    [SerializeField] private const int chunkSize = 16;
    [SerializeField] public const int chunkHeight = 128;
    [SerializeField] public const float threshold = 0;
    [SerializeField] public int resolution = 2;
    public static int ChunkSize => chunkSize;
    public int supportedChunkSize => ChunkSize + resolution * 3;
    
    // Chunk representation 
    // 0   1   2   3   4   5   6   7   8   9  10  12  13  14  15  16  17  18
    // .   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   .
    //     .-1-.-3-.-4-.-5-.-6-.-7-.-8-.-9-.-10.-11.-12.-13.-14.-15.-16.
    // We have 19 dots total
    // for 17 dots as chunk
    // so we have 16 blocks per chunk, hence the + 3

    private VanillaFunction vanillaFunction;

    [ExecuteAlways]
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
    }

    private void Start()
    {
        vanillaFunction = new VanillaFunction(GenerationInfo.GetRandomSeed());
    }

    // First job to be called from CreateChunk to generate MapData 
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MapDataJob : IJobParallelFor
    {
        // Input data
        public float offsetx;
        public float offsetz;

        public int resolution;
        // Output data
        public NativeArray<float> generatedMap;

        // Function reference
        public VanillaFunction vanillaFunction;
        
        public int supportedChunkSize => ChunkSize + resolution * 3;

        
        public void Execute(int idx)
        {
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % chunkHeight;
            int z = idx / (supportedChunkSize * chunkHeight);

            if (x % resolution == 0 && y % resolution == 0 && z % resolution == 0)
            {
                // Since it's a IJobParallelFor, job is called for every idx, filling the generatedMap in parallel
                generatedMap[idx] = vanillaFunction.GetResult(x + (offsetx), y, z + (offsetz));
            }
            else
            {
                generatedMap[idx] = math.NAN;
            }
        }
    }
    
    public JobHandle GenerateMapData(Vector2 offset, NativeArray<float> generatedMap)
    {
        // Job to generate input map data for marching cube
        var mapDataJob = new MapDataJob()
        {
            // Inputs
            offsetx = offset.x-resolution,
            offsetz = offset.y-resolution,
            vanillaFunction = vanillaFunction,
            resolution = resolution,
            // Output data
            generatedMap = generatedMap
        };
        
        JobHandle mapDataHandle = mapDataJob.Schedule(generatedMap.Length, 100);
        
        return mapDataHandle;
    }
}


using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class MapDataGenerator : MonoBehaviour
{
    [SerializeField] private Biome debugBiome;
    
    private static MapDataGenerator instance;
    public static MapDataGenerator Instance => instance;

    [SerializeField] private SourceNoise sourceNoise;    
    [HideInInspector] public AnimationCurve continentCurve;
    private SampledNoiseCurve sampledContinentCurve;
    
    [SerializeField] private int curveSampleCount = 512;
    [SerializeField] private const int chunkSize = 16;
    [SerializeField] public const int chunkHeight = 120;
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

    private VanillaEquation vanillaEquation;
    private FunctionPointer<TerrainEquation.TerrainEquationDelegate> compiledDelegate;


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

        sampledContinentCurve = new SampledNoiseCurve(continentCurve, curveSampleCount);
    }

    private void OnDestroy()
    {
        sampledContinentCurve.Dispose();
    }

    // First job to be called from CreateChunk to generate MapData 
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)] 
    public struct MapDataJob : IJobParallelFor
    {
        // Input data
        public float offsetx;
        public float offsetz;

        public int seed;
        public int resolution;
        // Output data
        public NativeArray<float> generatedMap;
        
        [ReadOnly]
        public SampledNoiseCurve continentCurve;
        [ReadOnly]
        public FastNoiseLite continentalness;
        
        // Function reference
        // [ReadOnly]
        // public NativeArray<FunctionPointer<TerrainEquation.TerrainEquationDelegate>> functionPointers;

        public int supportedChunkSize => ChunkSize + resolution * 3;
        
        public void Execute(int idx)
        {
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % chunkHeight;
            int z = idx / (supportedChunkSize * chunkHeight);

            int biomeIdx = supportedChunkSize * x + z;
            
            if (x % resolution == 0 && y % resolution == 0 && z % resolution == 0)
            {
                // Since it's a IJobParallelFor, job is called for every idx, filling the generatedMap in parallel
                // generatedMap[idx] = functionPointers[biomeIdx].Invoke(seed, x + (offsetx), y, z + (offsetz));
                generatedMap[idx] = continentCurve.EvaluateLerp(continentalness.GetNoise(seed, x + (offsetx), z + (offsetz))) - y;
            }   
            else
            {
                generatedMap[idx] = math.NAN; 
            }
        }
    }
    
    public JobHandle GenerateMapData(Vector2 offset, NativeArray<FunctionPointer<TerrainEquation.TerrainEquationDelegate>> biomeFunctionPointers, NativeArray<float> generatedMap)
    {
        // Job to generate input map data for marching cube
        var mapDataJob = new MapDataJob()
        {
            // Inputs
            offsetx = offset.x-resolution,
            offsetz = offset.y-resolution,
            seed = GenerationInfo.seed,
            continentCurve = sampledContinentCurve,
            continentalness = sourceNoise.continentalness,
            resolution = resolution,
            // Output data
            generatedMap = generatedMap
        };
        
        JobHandle mapDataHandle = mapDataJob.Schedule(generatedMap.Length, 100);

        biomeFunctionPointers.Dispose(mapDataHandle);
        
        return mapDataHandle;
    }
}


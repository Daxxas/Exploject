using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class MapDataGenerator : MonoBehaviour
{
    private static MapDataGenerator instance;
    public static MapDataGenerator Instance => instance;

    [SerializeField] private GenerationConfiguration generationConfiguration;
    public GenerationConfiguration GenerationConfiguration => generationConfiguration;

    [SerializeField] private int curveSampleCount = 512;
    private const int chunkSize = 16;
    public const int chunkHeight = 230;
    public const float threshold = 0;
    public const int resolution = 2;
    public static int ChunkSize => chunkSize;
    public static int SupportedChunkSize => ChunkSize + resolution * 3;
    
    // Chunk representation 
    // 0   1   2   3   4   5   6   7   8   9  10  12  13  14  15  16  17  18
    // .   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   .
    //     .-1-.-3-.-4-.-5-.-6-.-7-.-8-.-9-.-10.-11.-12.-13.-14.-15.-16.
    // We have 19 dots total
    // for 17 dots as chunk
    // so we have 16 blocks per chunk, hence the + 3


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

        generationConfiguration.sampledsquashContinentCurve = new SampledNoiseCurve(generationConfiguration.squashContinentCurve, curveSampleCount);
        generationConfiguration.sampledyContinentCurve = new SampledNoiseCurve(generationConfiguration.yContinentCurve, curveSampleCount);
    }

    public void Dispose()
    {
        generationConfiguration.sampledsquashContinentCurve.Dispose();
        generationConfiguration.sampledyContinentCurve.Dispose();
    }

    public JobHandle GenerateMapData(Vector2 offset, NativeArray<float> generatedMap)
    {
        // Job to generate input map data for marching cube
        var mapDataJob = new MapDataJob()
        {
            // Inputs
            offsetx = offset.x-resolution,
            offsetz = offset.y-resolution,
            seed = GenerationInfo.seed,
            squashContinentCurve = generationConfiguration.sampledsquashContinentCurve,
            squashContinentalness = generationConfiguration.squashContinentalness,
            yContinentalness = generationConfiguration.yContinentalness,
            yContinentCurve = generationConfiguration.sampledyContinentCurve,
            resolution = resolution,
            // Output data
            generatedMap = generatedMap
        };
        
        JobHandle mapDataHandle = mapDataJob.Schedule(generatedMap.Length, 100);
        
        return mapDataHandle;
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
        public SampledNoiseCurve yContinentCurve;
        [ReadOnly]
        public FastNoiseLite yContinentalness;
        
        
        [ReadOnly]
        public SampledNoiseCurve squashContinentCurve;
        [ReadOnly]
        public FastNoiseLite squashContinentalness;
        
        // Function reference
        // [ReadOnly]
        // public NativeArray<FunctionPointer<TerrainEquation.TerrainEquationDelegate>> functionPointers;

        public int supportedChunkSize => ChunkSize + resolution * 3;
        
        public void Execute(int idx)
        {
            int3 xyz = to3D(idx, supportedChunkSize, chunkHeight);

            if (xyz.x % resolution == 0 && xyz.y % resolution == 0 && xyz.z % resolution == 0)
            {
                // Since it's a IJobParallelFor, job is called for every idx, filling the generatedMap in parallel
                // generatedMap[idx] = functionPointers[biomeIdx].Invoke(seed, x + (offsetx), y, z + (offsetz));
                float yContinent = yContinentCurve.EvaluateLerp(yContinentalness.GetNoise(seed, xyz.x + (offsetx), xyz.z + (offsetz)));
                float squashContinent = squashContinentCurve.EvaluateLerp(yContinentalness.GetNoise(seed, xyz.x + (offsetx), xyz.z + (offsetz)));

                // Debug.Log($"[DATAJOB] Sampling {x + (offsetx)}, {z + (offsetz)} = {yContinentalness.GetNoise(seed, x + (offsetx), z + (offsetz))}");
                
                // generatedMap[idx] = (-(y - yContinent) / squashContinent) + 1 + squashContinentalness.GetNoise(seed, x + (offsetx), y, z + (offsetz)); 
                generatedMap[idx] = (-xyz.y / yContinent) + 1; //+ squashContinentalness.GetNoise(seed, x + (offsetx), y, z + (offsetz)); 
            }
            else
            {
                generatedMap[idx] = 0f;
            }
        }
        
        public static int3 to3D(int i, int chunkSize, int chunkHeight)
        {
            int3 xyz = new int3();
            xyz.x = i % chunkSize;
            xyz.y = (i / chunkSize) % chunkHeight;
            xyz.z = i / (chunkSize * chunkHeight);

            return xyz;
        }
    }
}


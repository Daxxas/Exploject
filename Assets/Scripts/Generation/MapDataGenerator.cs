using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DefaultNamespace;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Random = UnityEngine.Random;


public class MapDataGenerator : MonoBehaviour
{
    public int seed;
    
    [ShowInInspector] public const int chunkSize = 4;
    [ShowInInspector] public const int chunkHeight = 128;
    [ShowInInspector] public const float threshold = 0;
    public const int chunkBorderIncrease = 5;
    public static int supportedChunkSize => chunkSize + chunkBorderIncrease;
    
    // Chunk representation 
    // 0   1   2   3   4   5   6   7   8   9  10  12  13  14  15  16  17  18
    // .   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   *   .
    //     .-1-.-3-.-4-.-5-.-6-.-7-.-8-.-9-.-10.-11.-12.-13.-14.-15.-16.
    // We have 19 dots total
    // for 17 dots as chunk
    // so we have 16 blocks per chunk, hence the + 3

    public NativeArray<int> cornerIndexAFromEdge;
    public NativeArray<int> cornerIndexBFromEdge;
    public NativeArray<int> triangulation1D;

    private VanillaFunction vanillaFunction;
    public struct CubePoint
    {
        public int3 p;
        public float val;
    }

    private void Awake()
    {
        // Temporary, initialize terrain function with seed
        vanillaFunction = new VanillaFunction(seed);
        cornerIndexAFromEdge = new NativeArray<int>(12, Allocator.Persistent);
        cornerIndexAFromEdge.CopyFrom(MarchTable.cornerIndexAFromEdgeArray);
        cornerIndexBFromEdge = new NativeArray<int>(12, Allocator.Persistent);
        cornerIndexBFromEdge.CopyFrom(MarchTable.cornerIndexBFromEdgeArray);
        triangulation1D = new NativeArray<int>(4096, Allocator.Persistent);
        triangulation1D.CopyFrom(MarchTable.triangulation1DArray);
    }
    
    private void OnDisable()
    {
        // Dispose manually March table's NativeArray 
        // TODO : Find a way to do this in MarchTable class directly
        cornerIndexAFromEdge.Dispose();
        cornerIndexBFromEdge.Dispose();
        triangulation1D.Dispose();
    }

    // First job to be called from CreateChunk to generate MapData 
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MapDataJob : IJobParallelFor
    {
        // Input data
        public int supportedChunkSize;
        public int chunkHeight;
        public float offsetx;
        public float offsetz;
        // Output data
        public NativeArray<float> generatedMap;

        // Function reference
        public VanillaFunction VanillaFunction;
        
        public void Execute(int idx)
        {
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % chunkHeight;
            int z = idx / (supportedChunkSize * chunkHeight);

            // Since it's a IJobParallelFor, job is called for every idx, filling the generatedMap in parallel
            generatedMap[idx] = VanillaFunction.GetResult(x + (offsetx), y, z + (offsetz));
        }
    }
    
    


    public JobHandle GenerateMapData(Vector2 offset, NativeArray<float> generatedMap)
    {
        // Job to generate input map data for marching cube
        var mapDataJob = new MapDataJob()
        {
            // Inputs
            offsetx = offset.x,
            offsetz = offset.y,
            chunkHeight = chunkHeight,
            supportedChunkSize = supportedChunkSize,
            VanillaFunction = vanillaFunction,
            // Output data
            generatedMap = generatedMap
        };
        
        JobHandle mapDataHandle = mapDataJob.Schedule(generatedMap.Length, 4);
        
        return mapDataHandle;
    }
    
    // Call to visualize vertices positions
    public void DebugChunks(NativeHashMap<int3, float3> vertices,Vector2 offset)
    {
        var g = new GameObject("Info of " + offset.x + " " + offset.y);
        
        var vertexKeyValues = vertices.GetKeyValueArrays(Allocator.Persistent);
        var vertex = vertexKeyValues.Values;
        var vertexIndex = vertexKeyValues.Keys;
        
        for (int i = 0; i < vertexIndex.Length; i++)
        {
        
            var tmp = new GameObject();
            var tmpc = tmp.AddComponent<TextMeshPro>();
            tmpc.text = vertexIndex.ToString();
            tmpc.fontSize = 0.5f;
            tmpc.alignment = TextAlignmentOptions.Midline;
            
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.transform.position = new Vector3(vertex[i].x + offset.x, vertex[i].y, offset.y + vertex[i].z);
            c.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            c.transform.parent = g.transform;
            tmp.transform.parent = c.transform;
            tmp.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        vertexKeyValues.Dispose();
    }
    
    public static int to1D(int3 xyz)
    {
        return xyz.x + xyz.y*supportedChunkSize + xyz.z*supportedChunkSize*chunkHeight;
    }
}


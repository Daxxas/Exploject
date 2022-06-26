using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

// Second job to be called from CreateChunk after map data is generated
[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct MarchCubeJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> map;
        [ReadOnly]
        public NativeArray<int> triangulation;
        [ReadOnly]
        public NativeArray<int> cornerIndexAFromEdge;
        [ReadOnly]
        public NativeArray<int> cornerIndexBFromEdge;
        [WriteOnly]
        public NativeQueue<Triangle>.ParallelWriter triangles;
        [WriteOnly]
        public NativeParallelHashMap<Edge, float3>.ParallelWriter vertices;

        public int resolution;
        
        public int supportedChunkSize => MapDataGenerator.ChunkSize + resolution * 3;
        
        // job is IJobParallelFor, one index = 1 cube tested
        public void Execute(int idx)
        {
            // Cube currently tested, in array so it's easier to follow
            NativeArray<CubePoint> marchCube = new NativeArray<CubePoint>(8, Allocator.Temp);
            
            // Remember :
            // Map data generates 18x18 (ignoring height) data to calculate normals
            // Marching will output a vertices array of 18x18 to allow mesh generation to calculate normals
            // but the mesh generation should output a 16x16 vertices array
            int x = idx % supportedChunkSize;
            int y = (idx / supportedChunkSize) % MapDataGenerator.chunkHeight;
            int z = idx / (supportedChunkSize * MapDataGenerator.chunkHeight);

            // Don't calculate when not in Resolution
            // if (x % MapDataGenerator.resolution != 0 && y % MapDataGenerator.resolution != 0 && z % MapDataGenerator.resolution != 0)
            // {
            //     return;
            // }
            
            
            // Don't calculate when we are too close to the edge of the chunk to prevent out of bound or if it's useless
            if (x > MapDataGenerator.ChunkSize+resolution || y >= MapDataGenerator.chunkHeight - resolution || z > MapDataGenerator.ChunkSize+resolution)
            {
                return;
            }
            
            // put march cube in array to have cleaner code after
            #region March cube definition 
            marchCube[0] = new CubePoint()
            {
                p = new int3(x, y, z),
                val = map[to1D(x, y, z)]
            };
            marchCube[1] = new CubePoint()
            {
                p = new int3(x + resolution, y, z),
                val = map[to1D(x + resolution, y, z)]
            };
            marchCube[2] = new CubePoint()
            {
                p = new int3(x + resolution, y, z + resolution),
                val = map[to1D(x + resolution, y, z + resolution)]
            };
            marchCube[3] = new CubePoint()
            {
                p = new int3(x, y, z + resolution),
                val = map[to1D(x, y, z + resolution)]
            };
            marchCube[4] = new CubePoint()
            {
                p = new int3(x, y + resolution, z),
                val = map[to1D(x, y + resolution, z)]
            };
            marchCube[5] = new CubePoint()
            {
                p = new int3(x + resolution, y + resolution, z),
                val = map[to1D(x + resolution, y + resolution, z)]
            };
            marchCube[6] = new CubePoint()
            {
                p = new int3(x + resolution, y + resolution, z + resolution),
                val = map[to1D(x + resolution, y + resolution, z + resolution)]
            };
            marchCube[7] = new CubePoint()
            {
                p = new int3(x, y + resolution, z + resolution),
                val = map[to1D(x, y + resolution, z + resolution)]
            };
            #endregion
            
            // From values of the cube corners, find the cube configuration index
            int cubeindex = 0;
            if (marchCube[0].val < MapDataGenerator.threshold) cubeindex |= 1;
            if (marchCube[1].val < MapDataGenerator.threshold) cubeindex |= 2;
            if (marchCube[2].val < MapDataGenerator.threshold) cubeindex |= 4;
            if (marchCube[3].val < MapDataGenerator.threshold) cubeindex |= 8;
            if (marchCube[4].val < MapDataGenerator.threshold) cubeindex |= 16;
            if (marchCube[5].val < MapDataGenerator.threshold) cubeindex |= 32;
            if (marchCube[6].val < MapDataGenerator.threshold) cubeindex |= 64;
            if (marchCube[7].val < MapDataGenerator.threshold) cubeindex |= 128;
            
            if (cubeindex == 255 || cubeindex == 0) 
                return;

            // From the cube configuration, add triangles & vertices to mesh 
            for (int i = 0; triangulation[cubeindex * 16 + i] != -1; i += 3)
            {
                Triangle triangle = new Triangle();
                
                for (int j = 0; j < 3; j++)
                {
                    int a0 = cornerIndexAFromEdge[triangulation[cubeindex * 16 + i + j]];
                    int b0 = cornerIndexBFromEdge[triangulation[cubeindex * 16 + i + j]];
                    Edge edge0 = new Edge(marchCube[a0].p, marchCube[b0].p);

                    vertices.TryAdd(new Edge(marchCube[a0].p, marchCube[b0].p), FindVertexPos(
                        marchCube[a0].p, marchCube[b0].p, marchCube[a0].val,
                        marchCube[b0].val) - new float3(resolution, resolution, resolution));

                    triangle[j] = edge0;
                    
                    // Determine if vertex is border
                    bool isBordera0 = marchCube[a0].p.x < resolution || marchCube[a0].p.z < resolution || 
                                      marchCube[a0].p.x > MapDataGenerator.ChunkSize+(resolution) || marchCube[a0].p.z > MapDataGenerator.ChunkSize+(resolution);
                    bool isBorderb0 = marchCube[b0].p.x < resolution || marchCube[b0].p.z < resolution ||
                                      marchCube[b0].p.x > MapDataGenerator.ChunkSize+(resolution) || marchCube[b0].p.z > MapDataGenerator.ChunkSize+(resolution);
                    triangle.SetEdgeBorder(j, isBordera0 || isBorderb0);
                }
                
                // Triangles are stored in a queue because we don't know how many triangles we will get & we can write in parallel easily in a queue
                triangles.Enqueue(triangle);
            }

            marchCube.Dispose();
        }
        
        private float3 FindVertexPos(float3 p1, float3 p2, float v1val, float v2val)
        {

            if(Mathf.Abs(v1val) < 0.0001)
            {
                return p1;
            }

            if (Mathf.Abs(v2val) < 0.0001)
            {
                return p2;
            }

            if (Mathf.Abs(v1val - v2val) < 0.0001)
            {
                return p1;
            }
            float3 position = new float3(0,0,0);
        
            float mu = (MapDataGenerator.threshold - v1val) / (v2val - v1val);
            position.x = p1.x + mu * (p2.x - p1.x);
            position.y = p1.y + mu * (p2.y - p1.y);
            position.z = p1.z + mu * (p2.z - p1.z);
        
            return position;
        }
        public int to1D( int x, int y, int z)
        {
            return x + y*supportedChunkSize + z*supportedChunkSize*MapDataGenerator.chunkHeight;
        }
        
        public struct CubePoint
        {
            public int3 p;
            public float val;
        }

    }
    
[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct Edge : IEquatable<Edge>
{
    public readonly int3 point1;
    public readonly int3 point2;
    public Edge(int3 item1, int3 item2) { point1 = item1; point2 = item2;}

    public bool Equals(Edge other)
    {
        return math.all(other.point1 == point1) && math.all(other.point2 == point2) || math.all(other.point1 == point2) && math.all(other.point2 == point1) ;
    }

    public override string ToString()
    {
        return "a0: " + point1.ToString() + ", b0: " + point2.ToString();
    }

    public override int GetHashCode()
    {
        int hash = point1.GetHashCode() + point2.GetHashCode();

        return hash;
    }
}
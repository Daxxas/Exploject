using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


// Third & last job called from CreateChunk, generate mesh for marching cube data
[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct ChunkMeshJob : IJob
    {
        // Input reference
        public NativeQueue<Triangle> triangles;
        [ReadOnly] [NativeDisableParallelForRestriction]
        public NativeParallelHashMap<Edge, float3> uniqueVertices;
        [ReadOnly]
        public NativeArray<float> map;
        // Output
        public NativeList<Vector3> chunkVertices;
        public NativeList<int> chunkTriangles;
        public NativeList<Vector3> chunkNormals;

        public int resolution;
        public int supportedChunkSize => MapDataGenerator.ChunkSize + resolution * 3;
        
        
        public void Execute()
        {
            
            // Creating vertices & triangles arrays
            
            // Since it's not possible to use float as keys in a hashmap,
            // Instead we make a hashmap that stores the indices we already added in the vertices array & where we stored it
            // so if a new triangle references an already placed vertex, we know where it is already in the array
            int supposedVertexCount = triangles.Count * 3;
            NativeParallelHashMap<Edge, int> matchingIndices = new NativeParallelHashMap<Edge, int>(supposedVertexCount, Allocator.Temp);
            // vertices & triangles arrays 
            
            NativeList<Vector3> borderVertices = new NativeList<Vector3>(supposedVertexCount, Allocator.Temp);
            NativeList<int> borderTriangles =   new NativeList<int>(supposedVertexCount, Allocator.Temp);

            int chunkVerticesIndices = 0;
            int borderVerticesIndices = -1;
            
            while (triangles.TryDequeue(out Triangle triangle))
            {
                // Order is C - B - A to have triangle in the right direction
                
                // Logic is :
                // Vertices included in the final mesh have positive indices starting from 0
                // Vertices on the border of the chunk have negative indices starting from 1
                // Like that, when we loop through borderTriangles, we know from which array between chunkVertices and borderVertices
                // we need to pull the vertex from

                // borderTriangles have a mix of indices from chunkVertices and borderVertices (negative and positive indices)

                // The vertex is in the chunk mesh

                // For every vertex
                for (int i = 2; i >= 0; i--)
                {
                    // If the vertex is on the border, meaning it's not in the final mesh
                    // we try to add it to the matching indicies with the borderVerticesIndices as the matching index of the vertex 
                    if (triangle.GetIsBorder(i))
                    {
                        if (matchingIndices.TryAdd(triangle[i], borderVerticesIndices))
                        {
                            // add vertex to borderVertices for futur normal calculation
                            borderVertices.Add(uniqueVertices[triangle[i]]);
                            // add vertex index to borderTriangles
                            borderTriangles.Add(borderVerticesIndices);
                            // borderVerticesIndices is a negative index, so we decrement it
                            borderVerticesIndices--;
                        }
                        else
                        {
                            // Vertex outside of mesh already exists, we only add it to borderTriangles
                            borderTriangles.Add(matchingIndices[triangle[i]]);
                        }
                    }
                    else
                    {
                        // The vertex is in the chunk mesh
                        if (matchingIndices.TryAdd(triangle[i], chunkVerticesIndices))
                        {
                            // if it's a vertex we don't know, add it to chunkVertices no matter if the vertex comes from
                            // a border triangle or not.
                            // If the vertex comes from a border triangle, it's ok, it will be part of a triangle through a mesh chunk triangle
                            chunkVertices.Add(uniqueVertices[triangle[i]]);

                            if (!triangle.isBorderTriangle)
                            {
                                chunkTriangles.Add(chunkVerticesIndices);
                            }
                            else
                            {
                                // if the triangle we are currently looping through if a triangle in the border
                                // we add the vertex index to borderTriangles
                                // This is why borderTriangles contains positive indices 
                                borderTriangles.Add(chunkVerticesIndices);
                            }

                            chunkVerticesIndices++;
                        }
                        else
                        {
                            // Same logic as commment above
                            if (!triangle.isBorderTriangle)
                            {
                                chunkTriangles.Add(matchingIndices[triangle[i]]);
                            }
                            else
                            {
                                borderTriangles.Add(matchingIndices[triangle[i]]);
                            }
                        }
                    }
                }
            }


            // Normal calculation
            for (int i = 0; i < chunkVertices.Length; i++)
            {
                // normals needs to be set to zero before calculating them
                chunkNormals.Add(float3.zero);
            }

            CalculateNormals(chunkVertices.AsArray(), borderVertices.AsArray(), borderTriangles.AsArray(), chunkTriangles.AsArray(), chunkNormals);
            
            // TODO Set UVs somewhere
            
            /////////////////////////////////
            matchingIndices.Dispose();
        }

        private void CalculateNormals(NativeArray<Vector3> vertices, NativeArray<Vector3> borderVertices, NativeArray<int> borderTriangles, NativeArray<int> triangles, NativeArray<Vector3> normals)
        {
            // Loop through border triangles
            for (int i = 0; i < borderTriangles.Length / 3; i++)
            {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = borderTriangles[normalTriangleIndex];
                int vertexIndexB = borderTriangles[normalTriangleIndex+1];
                int vertexIndexC = borderTriangles[normalTriangleIndex+2];
                
                Vector3 triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC, vertices, borderVertices);
                
                // Only update vertex in the triangle that are in the chunk mesh
                if (vertexIndexA >= 0)
                {
                    normals[vertexIndexA] += triangleNormal;
                }
                if (vertexIndexB >= 0)
                {
                    normals[vertexIndexB] += triangleNormal;
                }
                if (vertexIndexC >= 0)
                {
                    normals[vertexIndexC] += triangleNormal;
                }
            }
            
            // Loop through chunk triangles
            for (int i = 0; i < triangles.Length  / 3; i++)
            {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = triangles[normalTriangleIndex];
                int vertexIndexB = triangles[normalTriangleIndex+1];
                int vertexIndexC = triangles[normalTriangleIndex+2];

                Vector3 triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC, vertices, borderVertices);
                // normals are set to 0 beforehand to avoid unpredictable issues
                normals[vertexIndexA] += triangleNormal;
                normals[vertexIndexB] += triangleNormal;
                normals[vertexIndexC] += triangleNormal;
            }
            
            for (int i = 0; i < normals.Length; i++)
            {
                math.normalize(normals[i]);
            }
        }

        Vector3 SurfaceNormal(int indexA, int indexB, int indexC, NativeArray<Vector3> vertices, NativeArray<Vector3> borderVertices)
        {
            // Get vertex from the right vertices array based on the index in the triangle
            // This is the reason why we have negative indices in the triangle array for the border
            float3 pointA = (indexA < 0) ? borderVertices[-indexA-1] : vertices[indexA];
            float3 pointB = (indexB < 0) ? borderVertices[-indexB-1] : vertices[indexB];
            float3 pointC = (indexC < 0) ? borderVertices[-indexC-1] : vertices[indexC];
            
            float3 sideAB = pointB - pointA;
            float3 sideAC = pointC - pointA;
            return math.cross(sideAB, sideAC);
        }
        

        public int to1D( int x, int y, int z)
        {
            return (x+1) + y*supportedChunkSize + (z+1)*supportedChunkSize*MapDataGenerator.chunkHeight;
        }

        public int to1D(int3 xyz)
        {
            return to1D(xyz.x, xyz.y, xyz.z);
        }
        
        private void SetBasicUVs(NativeArray<Vector3> vertices, NativeArray<float2> uvs)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = new float2(vertices[i].x,vertices[i].z);
            }
        }
    }
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


// Third & last job called from CreateChunk, generate mesh for marching cube data
[BurstCompile(FloatPrecision.High, FloatMode.Fast,CompileSynchronously = true)]
public struct ChunkMeshJob : IJob
    {
        // Input reference
        public NativeQueue<Triangle> triangles;
        public NativeHashMap<int3, float3> uniqueVertices;
        public Bounds bounds;
        public NativeArray<float> map;
        // Output
        public Mesh.MeshDataArray meshDataArray;

        public int chunkSize;
        public int chunkHeight;

        public void Execute()
        {
            // Initialize meshData
            Mesh.MeshData meshData = meshDataArray[0];
            
            #region initialize meshData
            
            int vertexAttributeCount = 4;
            int initVertexCount = triangles.Count * 3;
            int triangleIndexCount = triangles.Count * 3;
            
            // Attributes
            var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
                vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
            vertexAttributes[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3, stream: 1
            );
            vertexAttributes[2] = new VertexAttributeDescriptor(
                VertexAttribute.Tangent, dimension: 4, stream: 2
            );
            vertexAttributes[3] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2, stream: 3
            );
            #endregion
            
            // Creating vertices & triangles arrays
            
            // Since it's not possible to use float as keys in a hashmap,
            // Instead we make a hashmap that stores the indices we already added in the vertices array & where we stored it
            // so if a new triangle references an already placed vertex, we know where it is already in the array
            NativeHashMap<int3, int> matchingIndices = new NativeHashMap<int3, int>(initVertexCount, Allocator.Temp);
            // vertices & triangles arrays 
            NativeList<float3> chunkVertices = new NativeList<float3>(initVertexCount, Allocator.Temp);
            NativeList<int> chunkTriangles = new NativeList<int>(triangles.Count * 3, Allocator.Temp);
            

            int chunkVerticesIndices = 0;
            int borderVerticesIndices = -1;
            while (triangles.TryDequeue(out Triangle triangle))
            {
                
                // If triangle is in a border
                bool isVertexABorder = triangle.vertexIndexA.x < 0 || triangle.vertexIndexA.z < 0 || triangle.vertexIndexA.x > chunkSize * 100 || triangle.vertexIndexA.z > chunkSize * 100;
                bool isVertexBBorder = triangle.vertexIndexB.x < 0 || triangle.vertexIndexB.z < 0 || triangle.vertexIndexB.x > chunkSize * 100 || triangle.vertexIndexB.z > chunkSize * 100;
                bool isVertexCBorder = triangle.vertexIndexC.x < 0 || triangle.vertexIndexC.z < 0 || triangle.vertexIndexC.x > chunkSize * 100 || triangle.vertexIndexC.z > chunkSize * 100;
                bool isBorderTriangle = isVertexABorder || isVertexBBorder || isVertexCBorder;
                if (isBorderTriangle)
                    continue;
                
                // Order is C - B - A to have triangle in the right direction
                
                // Logic is :
                // Vertices included in the final mesh have positive indices starting from 0
                // Vertices on the border of the chunk have negative indices starting from 1
                // Like that, when we loop through borderTriangles, we know from which array between chunkVertices and borderVertices
                // we need to pull the vertex from

                // borderTriangles have a mix of indices from chunkVertices and borderVertices (negative and positive indices)

                // The vertex is in the chunk mesh
                if (matchingIndices.TryAdd(triangle.vertexIndexC, chunkVerticesIndices))
                {
                    // if it's a vertex we don't know, add it to chunkVertices no matter if the vertex comes from
                    // a border triangle or not.
                    // If the vertex comes from a border triangle, it's ok, we will find it back through a mesh chunk triangle
                    chunkVertices.Add(uniqueVertices[triangle.vertexIndexC]);
                    chunkTriangles.Add(chunkVerticesIndices);

                    chunkVerticesIndices++;
                }
                else
                {
                    // Same logic as commment above
                    chunkTriangles.Add(matchingIndices[triangle.vertexIndexC]);
                }
                
                // Same logic for vertex B of triangle
                if (matchingIndices.TryAdd(triangle.vertexIndexB, chunkVerticesIndices))
                {
                    chunkVertices.Add(uniqueVertices[triangle.vertexIndexB]);
                    chunkTriangles.Add(chunkVerticesIndices);

                    chunkVerticesIndices++;
                }
                else
                {
                    chunkTriangles.Add(matchingIndices[triangle.vertexIndexB]);

                }
                
                // Same logic for vertex A of triangle
                if (matchingIndices.TryAdd(triangle.vertexIndexA, chunkVerticesIndices))
                {
                    chunkVertices.Add(uniqueVertices[triangle.vertexIndexA]);
                    chunkTriangles.Add(chunkVerticesIndices);
                    chunkVerticesIndices++;
                }
                else
                {
                    chunkTriangles.Add(matchingIndices[triangle.vertexIndexA]);

                }
            }
            
            // Apply calculated array to mesh data arrays
            // we don't know in advance the vertices array length, so it's not possible to directly modify the
            // meshData vertices array
            meshData.SetVertexBufferParams(chunkVertices.Length, vertexAttributes);
            meshData.SetIndexBufferParams(chunkTriangles.Length, IndexFormat.UInt32);
            vertexAttributes.Dispose();

            NativeArray<float3> meshDataVertices = meshData.GetVertexData<float3>();
            NativeArray<int> triangleIndices = meshData.GetIndexData<int>();
            
            chunkVertices.AsArray().CopyTo(meshDataVertices);
            chunkTriangles.AsArray().CopyTo(triangleIndices);
            
            
            // Normal calculation
            NativeArray<float3> normals = meshData.GetVertexData<float3>(1);

            for (int i = 0; i < meshDataVertices.Length; i++)
            {
                // normals needs to be set to zero before calculating them
                normals[i] = float3.zero;
            }

            CalculateNormals(chunkVertices, normals);
            
            // Not necessary, only to have non triplanar material working a bit
            // Non-triplanar materials will be streched vertically
            SetBasicUVs( meshDataVertices, meshData.GetVertexData<float2>(3));
            
            // Finalize meshData
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, chunkTriangles.Length)
            {
                bounds = bounds,
                vertexCount = chunkVertices.Length
            }, MeshUpdateFlags.DontRecalculateBounds);
            
            /////////////////////////////////
            matchingIndices.Dispose();
            chunkVertices.Dispose();
            chunkTriangles.Dispose();
            // borderTriangles.Dispose();
            // borderVertices.Dispose();
        }

        private void CalculateNormals(NativeArray<float3> vertices, NativeArray<float3> normals)
        {
            // Loop through chunk triangles
            for (int i = 0; i < vertices.Length; i++)
            {
                float3 normal = SurfaceNormal(i, vertices);
                normals[i] = math.normalize(normal);
                // var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                // var np = go.AddComponent<NormalPreview>();
                // np.normal = normals[i];
                // np.normalColor = Color.blue;
                // np.transform.position = vertices[i];
            }
        }

        float3 SurfaceNormal(int pointIndex, NativeArray<float3> vertices)
        {
            float3 returnNormal = float3.zero;
            float3 currentVert = vertices[pointIndex];
            // var parent = new GameObject(currentVert.ToString());
            
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    for (int z = -1; z < 2; z++)
                    {
                        if (!(x == 0 && y == 0 && z == 0))
                        {
                            float3 offsetValuePos = new float3(currentVert.x + x, currentVert.y + y, currentVert.z + z);
                            float3 gradientVec = currentVert - offsetValuePos;
                            
                            float valueAtOffsetPos = GetInterpolatedValue(offsetValuePos);
                            // var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            // go.transform.parent = parent.transform;
                            // go.transform.position = offsetValuePos;
                            // go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            
                            if (valueAtOffsetPos >= 0)
                            {
                                // var np = go.AddComponent<NormalPreview>();
                                // np.normal = math.normalize(gradientVec * valueAtOffsetPos);
                                // np.valueAtThisPos = valueAtOffsetPos;
                                returnNormal += math.normalize(gradientVec * valueAtOffsetPos);
                            }
                            // else
                            // {
                            //     // Destroy(go);
                            // }
                            
                            // Debug.Log($"For value {currentVert}, testing {offsetValuePos}, gradient is {gradientVec}, final math is : {math.normalize(gradientVec * valueAtOffsetPos)}");
                        }
                    }
                }
            }

            return returnNormal;
        }

        private float GetInterpolatedValue(float3 xyz)
        {
            int3 xyz0 = ((int3) math.floor(xyz));
            int3 xyz1 = xyz0 + 1;

            // for (int i = 0; i < 2; i++)
            // {
            //     for (int j = 0; j < 2; j++)
            //     {
            //         for (int k = 0; k < 2; k++)
            //         {
            //             var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //             go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            //             Vector3 finalPos = Vector3.zero;
            //             if (i == 0)
            //                 finalPos.x = xyz0.x;
            //             else
            //                 finalPos.x = xyz1.x;
            //
            //             if (j == 0)
            //                 finalPos.y = xyz0.y;
            //             else
            //                 finalPos.y = xyz1.y;
            //
            //             if (k == 0)
            //                 finalPos.z = xyz0.z;
            //             else
            //                 finalPos.z = xyz1.z;
            //             
            //             go.transform.parent = parent;
            //             go.transform.position = finalPos;
            //             go.name = map[to1D((int3) new float3(finalPos.x, finalPos.y, finalPos.z))].ToString();
            //             finalPos.x++;
            //             finalPos.z++;
            //             // go.AddComponent<NormalPreview>().valueAtThisPos = map[to1D((int) finalPos.x, (int) finalPos.y, (int) finalPos.z)];
            //
            //         }
            //     }
            // }
            
            // Trilinear interpolation
            float xd = (xyz.x - xyz0.x) / (xyz1.x - xyz0.x);
            float yd = (xyz.y - xyz0.y) / (xyz1.y - xyz0.y);
            float zd = (xyz.z - xyz0.z) / (xyz1.z - xyz0.z);

            float c00 = map[to1D(xyz0)] * (1 - xd) + map[to1D(xyz1.x, xyz0.y, xyz0.z)] * xd;
            float c01 = map[to1D(xyz0.x, xyz0.y, xyz1.z)] * (1 - xd) + map[to1D(xyz1.x, xyz0.y, xyz1.z)] * xd;
            float c10 = map[to1D(xyz0.x, xyz1.y, xyz0.z)] * (1 - xd) + map[to1D(xyz1.x, xyz1.y, xyz0.z)] * xd;
            float c11 = map[to1D(xyz0.x, xyz1.y, xyz1.z)] * (1 - xd) + map[to1D(xyz1)] * xd;
            
            float c0 = c00 * (1 - yd) + c10 * yd;
            float c1 = c01 * (1 - yd) + c11 * yd;
            
            float c = c0 * (1 - zd) + c1 * zd;
            
            return c;

        }

        
        public int to1D( int x, int y, int z)
        {
            return (x+1) + y*MapDataGenerator.supportedChunkSize + (z+1)*MapDataGenerator.supportedChunkSize*chunkHeight;
        }

        public int to1D(int3 xyz)
        {
            return to1D(xyz.x, xyz.y, xyz.z);
        }
        
        private void SetBasicUVs(NativeArray<float3> vertices, NativeArray<float2> uvs)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = vertices[i].xz;
            }
        }
    }
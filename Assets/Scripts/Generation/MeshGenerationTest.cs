using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerationTest : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Material testMaterial; 
    
    private int cubeSize = 3;
    
    private void Start()
    {
        GenerateTestStructure();
    }

    public void GenerateTestStructure()
    {
        float[,,] mapData = new float[cubeSize,cubeSize,cubeSize];

        for (int x = 0; x < cubeSize; x++)
        {
            for (int y = 0; y < cubeSize; y++)
            {
                for (int z = 0; z < cubeSize; z++)
                {
                    if (y == cubeSize - 1)
                    {
                        mapData[x, y, z] = 0;
                    }
                    else
                    {
                        mapData[x, y, z] = 1;
                    }

                }
            }
        }

        GenerateMesh(mapData);
    }

    private void GenerateMesh(float[,,] map)
    {
        CombineInstance ci = new CombineInstance();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();


        for (int x = 0; x < cubeSize; x++)
        {
            for (int y = 0; y < cubeSize; y++)
            {
                for (int z = 0; z < cubeSize; z++)
                {
                    if (map[x, y, z] == 1)
                    {
                        vertices.Add(new Vector3(x,y,z));
                    }
                        
                }
            }
        }
        
        AddTriangles(1,1,1,map,ref triangles);
        

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);


        string str = "";
        foreach (var val in triangles)
        {
            str += val + ", ";
        }
        Debug.Log(str);

        str = "";
        for (int i = 0; i < vertices.Count; i++)
        {
            str += i + ", ";
        }
        Debug.Log(str);

        mesh.SetTriangles(triangles.ToArray(), 0);
        
        GameObject go = new GameObject();

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = testMaterial;

        mf.sharedMesh = mesh;
    }

    private void AddTriangles(int x, int y, int z, float[,,] map, ref List<int> triangles)
    {

        for (int offsetX = 0; offsetX < cubeSize; offsetX++)
        {
            for (int offsetY = 0; offsetY < cubeSize; offsetY++)
            {
                for (int offsetZ = 0; offsetZ < cubeSize; offsetZ++)
                {
                    if (!(offsetX == x && offsetY == y && offsetZ == z))
                    {
                        if (map[offsetX, offsetY, offsetZ] == 1 && map[offsetX, y, z] == 1 && map[x,y,z] == 1)
                        {
                            int testedX = offsetX + x;
                            int testedY = offsetY + y;
                            int testedZ = offsetZ + z;
                            triangles.Add(GetVerticesIndexFromPosition(testedX, y, z));
                            triangles.Add(GetVerticesIndexFromPosition(x,y,z));
                            triangles.Add(GetVerticesIndexFromPosition(testedX, testedY, testedZ));
                        }
                    }
                }
            }
        }
    }

    private int GetVerticesIndexFromPosition(int x, int y, int z)
    {
        // x = 0, y = 1, z = 2
        //    0       1       2       3       4       5       6       7       8       9       10      11      12
        // (0,0,0) (0,0,1) (0,0,2) (0,1,0) (0,1,1) (0,1,2) (0,2,0) (1,0,0) (1,0,1) (1,0,2) (1,1,0) (1,1,1) (1,1,2)
        // 
        
        return cubeSize*3*x + (cubeSize)*y + z;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerationTest : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;

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
                    mapData[x, y, z] = 1;

                }
            }
        }

        mapData[1, 1, 1] = 0;
        GenerateMesh(mapData);
    }

    private void GenerateMesh(float[,,] map)
    {
        CombineInstance ci = new CombineInstance();
        List<Vector3> vertices = new List<Vector3>();
        for (int x = 0; x < cubeSize; x++)
        {
            for (int y = 0; y < cubeSize; y++)
            {
                for (int z = 0; z < cubeSize; z++)
                {
                    if (map[x, y, z] == 1)
                    {
                        vertices.Add(new Vector3(x,y,z));
                        Instantiate(cubePrefab, new Vector3(x, y, z), Quaternion.identity);
                    }
                        
                }
            }
        }
        
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(new int, );

    }
}

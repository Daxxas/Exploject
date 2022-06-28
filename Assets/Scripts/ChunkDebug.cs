using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class BoundGizmo : MonoBehaviour
    {
        [SerializeField] private bool displayGizmos = false;

        private TerrainChunk terrainChunk;
        
        private MeshRenderer mr;
        private MeshFilter mf;

        [SerializeField] private float distanceFromPlayer;
        
        private void Start()
        {
            mr = GetComponentInChildren<MeshRenderer>();
            mf = GetComponentInChildren<MeshFilter>();
            terrainChunk = GetComponent<TerrainChunk>();
        }

        private void Update()
        {
            float chunkDistance = Vector2.Distance(terrainChunk.ChunkPos, EndlessTerrain.Instance.ViewerChunkPos);
        }

        private void OnDrawGizmos()
        {
            if (displayGizmos)
            {
                Gizmos.DrawWireCube(mr.bounds.center, mr.bounds.size);
                Gizmos.DrawWireSphere(mr.bounds.center, 1f);
                Gizmos.DrawRay(mr.bounds.center, Vector3.up* 50);

                for (int i = 0; i < mf.mesh.vertices.Length; i++)
                {
                    Gizmos.color = Color.cyan; 
                    Gizmos.DrawRay(transform.position +  mf.mesh.vertices[i], mf.mesh.normals[i].normalized);
                }
            }
        }
    }
}
using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class BoundGizmo : MonoBehaviour
    {
        public bool displayGizmos = false;
        
        private MeshRenderer mr;
        private MeshFilter mf;

        private void Start()
        {
            mr = GetComponent<MeshRenderer>();
            mf = GetComponent<MeshFilter>();
        }

        private void OnDrawGizmos()
        {
            if (displayGizmos)
            {
                Gizmos.DrawWireCube(mr.bounds.center, mr.bounds.size);
                // Gizmos.DrawWireSphere(mr.bounds.center, 1f);
                Gizmos.DrawRay(mr.bounds.center, Vector3.up* 25 );

                for (int i = 0; i < mf.mesh.vertices.Length; i++)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(mf.mesh.vertices[i], mf.mesh.normals[i].normalized);
                }
            }
        }
    }
}
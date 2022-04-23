using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class BoundGizmo : MonoBehaviour
    {
        private MeshRenderer mr;

        private void Start()
        {
            mr = GetComponent<MeshRenderer>();
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(mr.bounds.center, mr.bounds.size);
            Gizmos.DrawWireSphere(mr.bounds.center, 1f);
            Gizmos.DrawRay(mr.bounds.center, Vector3.up* 50 ); 
        }
    }
}
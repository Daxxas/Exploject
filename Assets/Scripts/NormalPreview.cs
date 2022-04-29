using System;
using Unity.Mathematics;
using UnityEngine;


public class NormalPreview : MonoBehaviour
{
    public float3 normal;
    public float valueAtThisPos;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, normal);
    }
}

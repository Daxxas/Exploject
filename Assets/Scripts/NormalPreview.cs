using System;
using Unity.Mathematics;
using UnityEngine;


public class NormalPreview : MonoBehaviour
{
    public float3 normal;
    public Color normalColor = Color.green;
    public float valueAtThisPos;

    private void OnDrawGizmos()
    {
        Gizmos.color = normalColor;
        Gizmos.DrawRay(transform.position, normal);
    }
}
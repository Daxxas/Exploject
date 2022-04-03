using System;
using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

public class PlayerController : MonoBehaviour, ICharacterController
{
    // Start is called before the first frame update

    [SerializeField] private KinematicCharacterMotor motor;

    private float gravity = 9.81f;
    
    private void Awake()
    {
        motor.CharacterController = this;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity.y += -(gravity * Time.deltaTime);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
        
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
        Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
        
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
        
    }
}

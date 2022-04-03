using System;
using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;

[RequireComponent(typeof(InputProvider))]
[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerController : MonoBehaviour, ICharacterController
{
    // External references
    [SerializeField] private KinematicCharacterMotor motor;
    private InputProvider inputProvider;
    
    // Component parameters
    [SerializeField] private float speed = 50;
    [SerializeField] private float mouseSensitivity = 200;

    // Internal variables
    private float gravity = 9.81f;
    private Vector2 mouseTurn;
    
    
    private void Awake()
    {
        motor.CharacterController = this;
    }

    void Start()
    {
        inputProvider = GetComponent<InputProvider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        mouseTurn.x += Mathf.Clamp(inputProvider.MouseDelta.x, -10, 10) * mouseSensitivity * Time.deltaTime;
        mouseTurn.y += Mathf.Clamp(inputProvider.MouseDelta.y, -10, 10) * mouseSensitivity * Time.deltaTime;
        currentRotation = Quaternion.Euler(-mouseTurn.y, mouseTurn.x, 0);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        
        currentVelocity = transform.forward * inputProvider.MoveDirection.y * speed * Time.deltaTime;
        currentVelocity += transform.right * inputProvider.MoveDirection.x * speed * Time.deltaTime;

        // currentVelocity.y += -(gravity * Time.deltaTime);
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

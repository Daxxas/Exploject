using System;
using UnityEngine;

public class InputProvider : MonoBehaviour
{
    private InputMap inputMap;

    private Vector2 moveDirection;
    public Vector2 MoveDirection => moveDirection;

    private Vector2 mouseDelta;
    public Vector2 MouseDelta => mouseDelta;

    

    private void OnEnable()
    {
        inputMap = new InputMap();
        
        inputMap.Enable();

        inputMap.Base.MoveDirection.performed += ctx => moveDirection = ctx.ReadValue<Vector2>();
        inputMap.Base.MouseDelta.performed += ctx => mouseDelta = ctx.ReadValue<Vector2>();
    }

    private void OnDisable()
    {
        inputMap.Disable();
    }
}

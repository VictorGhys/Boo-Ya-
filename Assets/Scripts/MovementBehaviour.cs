using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementBehaviour : MonoBehaviour
{
    [SerializeField]
    protected float _movementSpeed = 5.0f;
    [SerializeField]
    protected float _rotateSpeed = 5.0f;

    protected Rigidbody _rigidBody;

    public Vector3 DesiredMovementDirection { get; set; } = Vector3.zero;

    protected virtual void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    protected virtual void Update()
    {
        HandleRotation();
    }
    protected virtual void FixedUpdate()
    {
        HandleMovement();
    }

    protected virtual void HandleMovement()
    {
        Vector3 movement = DesiredMovementDirection.normalized;
        movement *= _movementSpeed * Time.deltaTime;

        _rigidBody.MovePosition(_rigidBody.position + movement);
    }

    protected virtual void HandleRotation()
    {
        if (DesiredMovementDirection == Vector3.zero)
        {
            return;
        }
        Quaternion desiredLookRotation = Quaternion.LookRotation(DesiredMovementDirection.normalized * 10, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredLookRotation, _rotateSpeed * Time.deltaTime);
    }
}

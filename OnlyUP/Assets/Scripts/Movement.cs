using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float CurrentSpeed;

    [SerializeField] private CharacterController _controller;
    [SerializeField] private StarterAssetsInputs _input;

    public float WalkSpeed {get;} = 0.5f;
    public  float SprintSpeed {get;} = 5.5f;
    private const float MAX_SPEED = 6.0f;
    private const float JUMP_HEIGHT = 1.2f;
    private const float GRAVITY = -15.0f;

    [Header("Player Grounded")]
    [SerializeField] private LayerMask GroundLayers;
    [SerializeField] private bool Grounded = true;
    [SerializeField] private float GroundedOffset = -0.14f;
    [SerializeField] private float GroundedRadius = 0.28f;

    [SerializeField] private GameObject _mainCamera;
    private float _currentRotation;
    private float _rotationVelocity;
    private float _verticalVelocity;
    [SerializeField] private float ROTATION_SMOTH_TIME = 0.12f;
    private Vector3 CurrentDirection;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public float CameraAngleOverride = 0.0f;
    public bool LockCameraPosition = false;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    private const float _threshold = 0.01f;



    private void Update()
    {
        Jump();
        Gravity();
        CameraRotation();
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    public void Move(Vector3 velocity)
    {
        CurrentSpeed = velocity.magnitude * MAX_SPEED;
        Vector3 inputDirection = velocity.normalized;

        if (CurrentSpeed > 0.5)
        {
            _currentRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _currentRotation, ref _rotationVelocity, ROTATION_SMOTH_TIME);

            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        CurrentDirection = Quaternion.Euler(0.0f, _currentRotation, 0.0f) * Vector3.forward;
        _controller.Move(CurrentDirection * (CurrentSpeed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
       
        //CameraRotation();
    }

    private void Jump()
    {
        if (_controller.isGrounded)
        {
            _verticalVelocity = 8f;
        }
    }

    private void Gravity()
    {
        if (!_controller.isGrounded)
        {
            _verticalVelocity = GRAVITY * Time.deltaTime;
        }
        else
        {
            _verticalVelocity = -0.5f;
        }
    }

    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            _cinemachineTargetYaw += _input.look.x * Time.deltaTime;
            _cinemachineTargetPitch += _input.look.y * Time.deltaTime;
            Debug.Log($"{_input.look.x} && {_input.look.y}");
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}

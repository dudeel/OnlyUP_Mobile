 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        [SerializeField] private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        [SerializeField] private GameObject _mainCamera;
        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        [Header("SMBEvents")]
        [SerializeField] private SMBEventHandler _smbEventHandler;

        [Header("Climb Settings")]
        [SerializeField] private CapsuleCollider _capsule;
        [SerializeField] private float _wallAngelMax;
        [SerializeField] private float _groundAngleMax;
        [SerializeField] private LayerMask _layerMaskClimb;

        [SerializeField] private float _overpassHeight;
        [SerializeField] private float _hangHeight;
        [SerializeField] private float _climbUpHeight;
        [SerializeField] private float _vaultHeight;
        [SerializeField] private float _stepHeight;

        [SerializeField] private Vector3 _endOffset;
        [SerializeField] private Vector3 _hangOffset;
        [SerializeField] private Vector3 _climpOriginDown;
        [SerializeField] private bool _climbing;
        private Vector3 _endPosition;
        private Vector3 _matchTargetPosition;
        private Quaternion _matchTargetRotation;
        private Quaternion _forwardNormalXZRotation;
        private RaycastHit _downRaycastHit;
        private RaycastHit _forwardRaycastHit;
        private MatchTargetWeightMask _weightMask = new MatchTargetWeightMask(Vector3.one, 1);

        [Header("Animation Settings")]
        public CrossFadeSettings _standToFreeHandSettings;
        public CrossFadeSettings _climbUpSettings;
        public CrossFadeSettings _vaultSettings;
        public CrossFadeSettings _stepUpSettings;
        public CrossFadeSettings _dropSettings;
        public CrossFadeSettings _dropToAirSettings;

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _smbEventHandler.Event.AddListener(OnSMBEvent);

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#endif

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            if (!_climbing)
            {
                GroundedCheck();
                Move();
                JumpAndGravity();

                if ((!Grounded && CanClimb(out _downRaycastHit, out _forwardRaycastHit, out _endPosition)) || (_input.jump && CanClimb(out _downRaycastHit, out _forwardRaycastHit, out _endPosition)))
                {
                    IntiateClimb();
                }
            }
        }

        private bool CanClimb(out RaycastHit downRaycastHit, out RaycastHit forwardRaycastHit,  out Vector3 endPosition)
        {
            endPosition = Vector3.zero;
            downRaycastHit = new RaycastHit();
            forwardRaycastHit = new RaycastHit();

            bool downHit;
            bool forwardHit;
            bool overpassHit;
            float climbHeight;
            float groundAngle;
            float wallAngle;

            RaycastHit overpassRaycastHit;

            Vector3 forwardDirectionXZ;
            Vector3 forwardNormalXZ;

            Vector3 downDirection = Vector3.down;
            Vector3 downOrigin = transform.TransformPoint(_climpOriginDown);

            downHit = Physics.Raycast(downOrigin, downDirection, out downRaycastHit, _climpOriginDown.y - _stepHeight, _layerMaskClimb);
            if (downHit)
            {
                float forwardDistance = _climpOriginDown.z;
                Vector3 forwardOrigin = new Vector3(transform.position.x, downRaycastHit.point.y - 0.1f, transform.position.z);
                Vector3 overpassOrigin = new Vector3(transform.position.x, _overpassHeight, transform.position.z);

                forwardDirectionXZ = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                forwardHit = Physics.Raycast(forwardOrigin, forwardDirectionXZ, out forwardRaycastHit, forwardDistance, _layerMaskClimb);
                overpassHit = Physics.Raycast(overpassOrigin, forwardDirectionXZ, out overpassRaycastHit, forwardDistance, _layerMaskClimb);
                climbHeight = downRaycastHit.point.y - transform.position.y;

                if (forwardHit)
                    if (overpassHit || climbHeight < _overpassHeight)
                    {
                        forwardNormalXZ = Vector3.ProjectOnPlane(forwardRaycastHit.normal, Vector3.up);
                        groundAngle = Vector3.Angle(downRaycastHit.normal, Vector3.up);
                        wallAngle = Vector3.Angle(-forwardNormalXZ, forwardDirectionXZ);

                        if (wallAngle <= _wallAngelMax)
                            if (groundAngle <= _groundAngleMax)
                            {
                                //End offset
                                Vector3 vectSurface = Vector3.ProjectOnPlane(forwardDirectionXZ, downRaycastHit.normal);
                                endPosition = downRaycastHit.point + Quaternion.LookRotation(vectSurface, Vector3.up) * _endOffset;

                                //De-penetration
                                Collider colliderB = downRaycastHit.collider;
                                bool penetrationOverlap = Physics.ComputePenetration(
                                    colliderA: _capsule,
                                    positionA: endPosition,
                                    rotationA: transform.rotation,
                                    colliderB: colliderB,
                                    positionB: colliderB.transform.position,
                                    rotationB: colliderB.transform.rotation,
                                    direction: out Vector3 penetrationDirection,
                                    distance: out float penetrationDistance);
                                if (penetrationOverlap)
                                    endPosition += penetrationDirection * penetrationDistance;

                                //Up Sweep
                                float inflate = -0.05f;
                                float upsweepDistance = downRaycastHit.point.y - transform.position.y;
                                Vector3 upSweepDirection = transform.up;
                                Vector3 upSweepOrigin = transform.position;
                                bool upSweepHit = CharacterSweep(
                                    position: upSweepOrigin,
                                    rotation: transform.rotation,
                                    direction: upSweepDirection,
                                    distance: upsweepDistance,
                                    layerMask: _layerMaskClimb,
                                    inflate: inflate);
                                
                                //Forward Sweep
                                Vector3 forwardSweepOrigin = transform.position + upSweepDirection * upsweepDistance;
                                Vector3 forwardSweepVector = endPosition - forwardSweepOrigin;
                                bool forwardSweepHit = CharacterSweep(
                                    position: forwardSweepOrigin,
                                    rotation: transform.rotation,
                                    direction: forwardSweepVector.normalized,
                                    distance: forwardSweepVector.magnitude,
                                    layerMask: _layerMaskClimb,
                                    inflate: inflate);

                                if (!upSweepHit && !forwardSweepHit)
                                {
                                    _endPosition = endPosition;
                                    _downRaycastHit = downRaycastHit;
                                    _forwardRaycastHit = forwardRaycastHit;

                                    return true;
                                }
                            }
                    }
            }
            return false; 
        }

        private bool CharacterSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, LayerMask layerMask, float inflate)
        {
            float heightScale = Mathf.Abs(transform.lossyScale.y);
            float radiusScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));

            float radius = _capsule.radius * radiusScale;
            float totalHeaight = Mathf.Max(_capsule.height * heightScale, radius * 2);

            Vector3 capsuleUp = rotation * Vector3.up;
            Vector3 center = position + rotation * _capsule.center;
            Vector3 top = center + capsuleUp * (totalHeaight / 2 - radius);
            Vector3 bottom = center - capsuleUp * (totalHeaight / 2 - radius);

            bool sweepHit = Physics.CapsuleCast(
                point1: bottom,
                point2: top,
                radius: radius,
                direction: direction, 
                maxDistance: distance,
                layerMask: layerMask);
            
            return sweepHit;
        }

        private void IntiateClimb()
        {
            _climbing = true;
            _speed = 0;
            _animator.SetFloat(_animIDSpeed, 0);
            _capsule.enabled = false;
            
            float _climbingHeight = _downRaycastHit.point.y - transform.position.y;
            Vector3 _forwardNormalXZ = Vector3.ProjectOnPlane(_forwardRaycastHit.normal, Vector3.up);
            _forwardNormalXZRotation = Quaternion.LookRotation(-_forwardNormalXZ, Vector3.up);

            if (_climbingHeight > _hangHeight)
            {
                _matchTargetPosition = _forwardRaycastHit.point + _forwardNormalXZRotation * _hangOffset;
                _matchTargetRotation = _forwardNormalXZRotation;
                _animator.CrossFadeInFixedTime(_standToFreeHandSettings);
            }
            else if (_climbingHeight > _climbUpHeight)
            {
                _matchTargetPosition = _endPosition;
                _matchTargetRotation = _forwardNormalXZRotation;
                _animator.CrossFadeInFixedTime(_climbUpSettings);
            }
            else if (_climbingHeight > _vaultHeight)
            {
                _matchTargetPosition = _endPosition;
                _matchTargetRotation = _forwardNormalXZRotation;
                _animator.CrossFadeInFixedTime(_vaultSettings);
            }
            else if (_climbingHeight > _stepHeight)
            {
                _matchTargetPosition = _endPosition;
                _matchTargetRotation = _forwardNormalXZRotation;
                _animator.CrossFadeInFixedTime(_stepUpSettings);
            }
            else
            {
                _climbing = false;
            }
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private const float MAX_SPEED = 6.0f;
        private void Move()
        {
            float targetSpeed = _input.move.magnitude * MAX_SPEED;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else _speed = targetSpeed;

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);

                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _animator.SetBool(_animIDJump, true);
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f) _fallTimeoutDelta -= Time.deltaTime;
                else _animator.SetBool(_animIDFreeFall, true);

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity) _verticalVelocity += Gravity * Time.deltaTime;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnAnimatorMove()
        {
            if (_animator.isMatchingTarget)
                _animator.ApplyBuiltinRootMotion();
        }

        public void OnSMBEvent(string eventName)
        {
            switch(eventName)
            {
                case "StandToFreehangEnter":
                    _animator.MatchTarget(_matchTargetPosition, _matchTargetRotation, AvatarTarget.Root, _weightMask, 0.3f, 0.65f);
                    break;
                case "ClimbUpEnter":
                    _animator.MatchTarget(_matchTargetPosition, _matchTargetRotation, AvatarTarget.Root, _weightMask, 0, 0.9f);
                    break;
                case "VaultEnter":
                    _animator.MatchTarget(_matchTargetPosition, _matchTargetRotation, AvatarTarget.Root, _weightMask, 0, 0.65f);
                    break;
                case "StepUpEnter":
                    _animator.MatchTarget(_matchTargetPosition, _matchTargetRotation, AvatarTarget.Root, _weightMask, 0.3f, 0.8f);
                    break;
                case "DropEnter":
                    _animator.MatchTarget(_matchTargetPosition, _matchTargetRotation, AvatarTarget.Root, _weightMask, 0.2f, 0.5f);
                    break;

                case "StandToFreehangExit":
                    break;
                case "ClimbUpExit":
                case "VaultExit":
                case "StepUpExit":
                case "DropExit":
                    _climbing = false;
                    _capsule.enabled = true;
                    break;
                default:
                    break;
            }
        }
    }
}
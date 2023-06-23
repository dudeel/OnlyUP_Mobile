using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class P_Controller : MonoBehaviour
{
    [Header("Player")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;

    //Как быстро персонаж поворачивается лицом к направлению движения
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    //Ускорение и замедление
    public float SpeedChangeRate = 10.0f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    //Высота, на которую игрок может прыгнуть
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

    [Space(10)]
    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    public LayerMask GroundLayers;
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public float CameraAngleOverride = 0.0f;
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
    private Animator _animator;
    private CharacterController _controller;
    private StarterAssetsInputs _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;


    private void Awake()
    {
        if (_mainCamera == null)  _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        
        _animator =GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();

        // SetAnimationID();

        //Восстановление тайм-аутов при запуске
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        JumpAndGravity();
        GroundedCheck();
        Move();
        CameraRotation();
    }

    // private void AssignAnimationIDs()
    // {
    //     _animIDSpeed = Animator.StringToHash("Speed");
    //     _animIDGrounded = Animator.StringToHash("Grounded");
    //     _animIDJump = Animator.StringToHash("Jump");
    //     _animIDFreeFall = Animator.StringToHash("FreeFall");
    //     _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    // }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

        //_animator.SetBool(_animIDGrounded, Grounded);
    }

    private void Move()
    {
        //установите целевую скорость на основе скорости перемещения, скорости спринта и нажатия кнопки sprint
        float targetSpeed;
        if (_input.sprint) targetSpeed = SprintSpeed;
        else targetSpeed = MoveSpeed;

        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        //Ссылка на текущую горизонтальную скорость игрока
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;

        float inputMagnitude;
        if (_input.analogMovement) inputMagnitude = _input.move.magnitude;
        else inputMagnitude = 1f;

        //Ускорение или замедление до заданной скорости
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else _speed = targetSpeed;

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        //Нормализовать направление движения
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // примечание: Оператор Vector2 != использует аппроксимацию, поэтому не подвержен ошибкам с плавающей запятой и дешевле, чем величина
        // если есть ввод перемещения, поверните проигрыватель, когда он движется
        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

            //Поворот персонажа относительно положения камеры
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        //Передвижение игрока
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        //_animator.SetFloat(_animIDSpeed, _animationBlend);
        //_animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            //Сброс таймера таймаута при падении
            _fallTimeoutDelta = FallTimeout;

            //_animator.SetBool(_animIDJump, false);
            //_animator.SetBool(_animIDFreeFall, false);

            //Остановите бесконечное падение нашей скорости при заземлении
            if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

            // Прыжок
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                //квадратный корень из H * -2 * G = скорость необходимая для достижения желаемой высоты
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                //_animator.SetBool(_animIDJump, true);
            }

            //Таймаут прыжка
            if (_jumpTimeoutDelta >= 0.0f) _jumpTimeoutDelta -= Time.deltaTime;
        }
        else
        {
            //Сброс таймера таймаута при падении
            _jumpTimeoutDelta = JumpTimeout;

            //Время падения
            if (_fallTimeoutDelta >= 0.0f) _fallTimeoutDelta -= Time.deltaTime;
            //else _animator.SetBool(_animIDFreeFall, true);

            //Если не на земле, то нельзя прыгать
            _input.jump = false;
        }

        //Применяйте гравитацию с течением времени, если находитесь под терминалом (умножьте на дельта-время дважды, чтобы линейно ускоряться с течением времени)
        if (_verticalVelocity < _terminalVelocity) _verticalVelocity += Gravity * Time.deltaTime;
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

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            _cinemachineTargetYaw += _input.look.x * Time.deltaTime;
            _cinemachineTargetPitch += _input.look.y * Time.deltaTime;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }
}
using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    /* ============ SERIALIZE FIELD ============ */
    [SerializeField]
    private Transform _cameraTransform;

    [SerializeField]
    private CameraManager _cameraManager;

    [SerializeField]
    private float _walkSpeed = 350;

    [SerializeField]
    private float _sprintSpeed;

    [SerializeField]
    private InputManager _input;

    [SerializeField]
    private float _rotationSmoothTime = 0.1f;

    [SerializeField]
    private float _walkSprintTransition;

    [SerializeField]
    private float _jumpForce;

    [SerializeField]
    private Transform _groundDetector;

    [SerializeField]
    private float _detectorRadius;

    [SerializeField]
    private LayerMask _groundLayer;

    [SerializeField]
    private Vector3 _upperStepOffset;

    [SerializeField]
    private float _stepCheckerDistance;

    [SerializeField]
    private float _stepForce;

    [SerializeField]
    private Transform _climbDetector;

    [SerializeField]
    private float _climbSpeed;

    [SerializeField]
    private float _climbCheckDistance;

    [SerializeField]
    private LayerMask _climbaleLayer;

    [SerializeField]
    private Vector3 _climbOffset;

    [SerializeField]
    private float _crouchSpeed;

    [SerializeField]
    private float _glideSpeed;

    [SerializeField]
    private float _airDrag;

    [SerializeField]
    private Vector3 _glideRotationSpeed;

    [SerializeField]
    private float _minGlideRotationX;

    [SerializeField]
    private float _maxGlideRotationX;

    [SerializeField]
    private float _resetComboInterval;

    [SerializeField]
    private Transform _hitDetector;

    [SerializeField]
    private float _hitDetectoRadius;

    [SerializeField]
    private LayerMask _hitLayer;

    [SerializeField]
    private PlayerAudioManager _playerAudioManager;

    /* ============ PRIVATE VARIABLE ============ */
    private bool _isGrounded;
    private float _speed;
    private float _rotationSmoothVelocity;
    private bool _isPunching;
    private int _combo = 0;
    private Coroutine _resetCombo;

    private Rigidbody _rigidbody;
    private PlayerStance _playerStance;
    private Animator _animator;
    private CapsuleCollider _collider;

    /* ============ UNITY FUNCTION ============ */
    private void Awake()
    {
        HideAndLockCursor();

        _speed = _walkSpeed;
        _playerStance = PlayerStance.Stand;

        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<CapsuleCollider>();
    }

    void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _input.OnCrouchInput += Crouch;
        _input.OnGlideInput += StartGlide;
        _input.OnCancelGlide += CancelGlide;
        _input.OnPunchInput += Punch;

        _cameraManager.OnChangePerspective += ChangePerspective;
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimb;
        _input.OnCrouchInput -= Crouch;
        _input.OnGlideInput -= StartGlide;
        _input.OnCancelGlide -= CancelGlide;
        _input.OnPunchInput -= Punch;

        _cameraManager.OnChangePerspective -= ChangePerspective;
    }

    // Update is called once per frame
    void Update()
    {
        CheckIsGrounded();
        CheckStep();

        Glide();
    }

    /* ============= UTILITIES ======= */
    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
        Debug.Log(_animator.GetBool("ChangePerspective"));
    }

    /* ============ BAGIAN PENGECEKAN ============ */
    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        _animator.SetBool("IsGrounded", _isGrounded);

        if (_isGrounded)
        {
            CancelGlide();
        }
    }

    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(
            _groundDetector.position,
            transform.forward,
            _stepCheckerDistance
        );

        bool isHitUpperStep = Physics.Raycast(
            _groundDetector.position + _upperStepOffset,
            transform.forward,
            _stepCheckerDistance
        );

        if (isHitLowerStep && !isHitUpperStep)
        {
            _rigidbody.AddForce(0, _stepForce, 0);
        }
    }

    /* ============ MAIN MOVEMENT ============ */
    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;

        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        bool isPlayerCrouch = _playerStance == PlayerStance.Crouch;
        bool isPlayerGliding = _playerStance == PlayerStance.Glide;

        if ((isPlayerStanding || isPlayerCrouch) && !_isPunching)
        {
            Vector3 velocity = new Vector3(
                _rigidbody.linearVelocity.x,
                0,
                _rigidbody.linearVelocity.z
            );

            _animator.SetFloat("Velocity", velocity.magnitude * axisDirection.magnitude);
            _animator.SetFloat("VelocityZ", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("VelocityX", velocity.magnitude * axisDirection.x);

            switch (_cameraManager.CameraState)
            {
                case CameraState.ThirdPerson:
                    if (axisDirection.magnitude >= 0.1)
                    {
                        float rotationAngle =
                            Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg
                            + _cameraTransform.eulerAngles.y;

                        float smoothAngle = Mathf.SmoothDampAngle(
                            transform.eulerAngles.y,
                            rotationAngle,
                            ref _rotationSmoothVelocity,
                            _rotationSmoothTime
                        );

                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        movementDirection =
                            Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;

                        _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime);
                    }
                    break;
                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                    Vector3 verticalDirection = axisDirection.y * transform.forward;
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    movementDirection = verticalDirection + horizontalDirection;

                    _rigidbody.AddForce(movementDirection * Time.deltaTime * _speed);
                    break;
                default:
                    break;
            }
        }
        else if (isPlayerClimbing)
        {
            Vector3 Horizontal = axisDirection.x * transform.right;
            Vector3 vertical = axisDirection.y * transform.up;

            movementDirection = Horizontal + vertical;
            _rigidbody.AddForce(movementDirection * Time.deltaTime * _climbSpeed);

            Vector3 velocity = new Vector3(
                _rigidbody.linearVelocity.x,
                _rigidbody.linearVelocity.y,
                0
            );
            _animator.SetFloat("ClimbVelocityY", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("ClimbVelocityX", velocity.magnitude * axisDirection.x);
        }
        else if (isPlayerGliding)
        {
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += _glideRotationSpeed.x * axisDirection.y * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(
                rotationDegree.x,
                _minGlideRotationX,
                _maxGlideRotationX
            );

            rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
            rotationDegree.y += _glideRotationSpeed.y * axisDirection.x * Time.deltaTime;

            transform.rotation = Quaternion.Euler(rotationDegree);
        }
    }

    private void Sprint(bool isSprint)
    {
        if (isSprint)
        {
            if (_speed < _sprintSpeed)
            {
                _speed = _speed + _walkSprintTransition * Time.deltaTime;
            }
        }
        else
        {
            if (_speed > _walkSpeed)
            {
                _speed = _speed - _walkSprintTransition * Time.deltaTime;
            }
        }
    }

    private void Jump()
    {
        if (_isGrounded)
        {
            _animator.SetTrigger("Jump");
            Vector3 jumpDirection = Vector3.up;
            _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
        }
    }

    private void StartClimb()
    {
        bool isInFrontClimbWall = Physics.Raycast(
            _climbDetector.position,
            transform.forward,
            out RaycastHit hit,
            _climbCheckDistance,
            _climbaleLayer
        );

        bool isNotClimbing = _playerStance != PlayerStance.Climb;

        if (isInFrontClimbWall && _isGrounded && isNotClimbing)
        {
            _collider.center = Vector3.up * 1.3f;
            _animator.SetBool("IsClimbing", true);

            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;
            _playerStance = PlayerStance.Climb;
            _rigidbody.useGravity = false;

            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(70);
        }
    }

    private void CancelClimb()
    {
        if (_playerStance == PlayerStance.Climb)
        {
            _collider.center = Vector3.up * 0.9f;
            _animator.SetBool("IsClimbing", false);

            _playerStance = PlayerStance.Stand;
            _rigidbody.useGravity = true;
            transform.position -= transform.forward * 1f;

            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOfView(40);
        }
    }

    private void Crouch()
    {
        if (_playerStance == PlayerStance.Stand)
        {
            _collider.height = 1.3f;
            _collider.center = Vector3.up * 0.66f;

            _playerStance = PlayerStance.Crouch;
            _animator.SetBool("IsCrouch", true);
            _speed = _crouchSpeed;
        }
        else if (_playerStance == PlayerStance.Crouch)
        {
            _collider.height = 1.8f;
            _collider.center = Vector3.up * 0.9f;

            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsCrouch", false);
            _speed = _walkSpeed;
        }
    }

    private void StartGlide()
    {
        if (_playerStance != PlayerStance.Glide && !_isGrounded)
        {
            _playerStance = PlayerStance.Glide;
            _animator.SetBool("IsGliding", true);
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
        }
    }

    private void Glide()
    {
        if (_playerStance == PlayerStance.Glide && !_isGrounded)
        {

            _playerAudioManager.PlayeGlideSfx();
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 upForce = transform.up * (lift + _airDrag);
            Vector3 forwardForce = transform.forward * _glideSpeed;
            Vector3 totalForce = upForce + forwardForce;
            _rigidbody.AddForce(totalForce * Time.deltaTime);
        }
    }

    private void CancelGlide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            _playerAudioManager.StopGlideSfx();
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsGliding", false);
            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
        }
    }

    private void Punch()
    {
        if (!_isPunching && _playerStance == PlayerStance.Stand)
        {
            _isPunching = true;
            if (_combo < 3)
            {
                _combo = _combo + 1;
            }
            else
            {
                _combo = 1;
            }

            Debug.Log(_isPunching);
            Debug.Log(_combo);

            _animator.SetInteger("Combo", _combo);
            _animator.SetBool("Punch", _isPunching);
        }
    }

    private void Hit()
    {
        Collider[] hitObjects = Physics.OverlapSphere(
            _hitDetector.position,
            _hitDetectoRadius,
            _hitLayer
        );

        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i].gameObject != null)
            {
                Destroy(hitObjects[i].gameObject);
            }
        }
    }

    private void EndPunch()
    {
        _isPunching = false;
        _animator.SetBool("Punch", _isPunching);

        if (_resetCombo != null)
        {
            StopCoroutine(_resetCombo);
        }

        _resetCombo = StartCoroutine(ResetCombo());

        Debug.Log(_isPunching);
    }

    private IEnumerator ResetCombo()
    {
        yield return new WaitForSeconds(_resetComboInterval);
        _combo = 0;
    }

    // private void OnCollisionEnter(Collision other)
    // {
    //     // Is Trigger harus non-aktif
    //     // Mendeteksi saat object mulai berbenturan dengan object lain
    // }

    // private void OnCollisionStay(Collision other)
    // {
    //     // Is Trigger harus non-aktif
    //     // Mendeteksi selama object berbenturan dengan object lain
    // }

    // private void OnCollisionExit(Collision other)
    // {
    //     // Is Trigger harus non-aktif
    //     // Mendeteksi saat object berhenti berbenturan dengan object lain
    // }

    // private void OnTriggerEnter(Collider other)
    // {
    //     // Is Trigger harus aktif
    //     // Mendeteksi saat object lain mulai masuk ke area trigger object
    // }

    // private void OnTriggerStay(Collider other)
    // {
    //     // Is Trigger harus aktif
    //     // Mendeteksi selama object lain berada dalam area trigger object
    // }

    // private void OnTriggerExit(Collider other)
    // {
    //     // Is Trigger harus aktif
    //     // Mendeteksi saat object lain sepenuhnya keluar dari area trigger object
    // }
}

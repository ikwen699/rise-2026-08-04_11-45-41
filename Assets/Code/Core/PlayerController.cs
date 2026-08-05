using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference sprintAction;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.8f;
        [SerializeField] private float rotationSmoothTime = 0.12f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;

        [Header("Camera")]
        [Tooltip("Pivot that orbits the camera around the player (Y for yaw, X for pitch).")]
        [SerializeField] private Transform cameraPivot;
        [Tooltip("The Cinemachine camera transform, used for camera-relative movement.")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float lookSensitivity = 1f;
        [SerializeField] private Vector2 pitchClamp = new Vector2(-30f, 60f);
        [SerializeField] private bool lockCursorOnStart = true;

        [Header("State")]
        [SerializeField] private bool isGrounded;

        private CharacterController _controller;
        private Vector2 _moveInput;
        private bool _jumpQueued;
        private bool _sprintHeld;
        private float _verticalVelocity;
        private float _yaw;
        private float _pitch;
        private float _targetRotation;
        private float _rotationVelocity;
        private Systems.WalkAnimation _walkAnim;

        public bool IsGrounded => isGrounded;

        public void ConfigureForSceneBuilder(
            InputActionReference move, InputActionReference look,
            InputActionReference jump, InputActionReference sprint,
            Transform pivot, Transform cameraTf)
        {
            moveAction = move;
            lookAction = look;
            jumpAction = jump;
            sprintAction = sprint;
            cameraPivot = pivot;
            cameraTransform = cameraTf;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _walkAnim = GetComponentInChildren<Systems.WalkAnimation>();
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.performed += OnMovePerformed;
            if (moveAction != null) moveAction.action.canceled += OnMoveCanceled;
            if (lookAction != null) lookAction.action.performed += OnLookPerformed;
            if (jumpAction != null) jumpAction.action.performed += OnJumpPerformed;
            if (sprintAction != null) sprintAction.action.performed += OnSprintPerformed;
            if (sprintAction != null) sprintAction.action.canceled += OnSprintCanceled;

            moveAction?.action.Enable();
            lookAction?.action.Enable();
            jumpAction?.action.Enable();
            sprintAction?.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.performed -= OnMovePerformed;
            if (moveAction != null) moveAction.action.canceled -= OnMoveCanceled;
            if (lookAction != null) lookAction.action.performed -= OnLookPerformed;
            if (jumpAction != null) jumpAction.action.performed -= OnJumpPerformed;
            if (sprintAction != null) sprintAction.action.performed -= OnSprintPerformed;
            if (sprintAction != null) sprintAction.action.canceled -= OnSprintCanceled;

            moveAction?.action.Disable();
            lookAction?.action.Disable();
            jumpAction?.action.Disable();
            sprintAction?.action.Disable();
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (cameraPivot != null)
            {
                _yaw = cameraPivot.eulerAngles.y;
                _pitch = cameraPivot.localEulerAngles.x;
                if (_pitch > 180f) _pitch -= 360f;
            }
        }

        private void Update()
        {
            UpdateCamera();
            ApplyGravity();
            Move();
        }

        private void LateUpdate()
        {
            if (cameraPivot != null)
            {
                cameraPivot.position = transform.position;
            }
        }

        private void UpdateCamera()
        {
            if (cameraPivot == null) return;

            Vector2 look = lookAction != null ? lookAction.action.ReadValue<Vector2>() * lookSensitivity : Vector2.zero;
            if (look == Vector2.zero) return;

            _yaw += look.x;
            _pitch = Mathf.Clamp(_pitch - look.y, pitchClamp.x, pitchClamp.y);
            cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void ApplyGravity()
        {
            isGrounded = _controller.isGrounded;
            if (isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;

            if (_jumpQueued && isGrounded)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _jumpQueued = false;
            }

            _verticalVelocity += gravity * Time.deltaTime;
        }

        private void Move()
        {
            float speed = _sprintHeld ? moveSpeed * sprintMultiplier : moveSpeed;
            Vector3 motion = Vector3.up * _verticalVelocity;

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

                Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
                forward.y = 0f;
                forward.Normalize();
                Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
                right.y = 0f;
                right.Normalize();

                Vector3 moveDir = (forward * inputDir.z + right * inputDir.x).normalized;
                float targetRotation = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref _rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, rotation, 0f);

                motion += moveDir * speed;
            }

            _controller.Move(motion * Time.deltaTime);

            float hSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
            if (_walkAnim != null) _walkAnim.SetSpeed(hSpeed);
        }

        private void OnMovePerformed(InputAction.CallbackContext context) => _moveInput = context.ReadValue<Vector2>();
        private void OnMoveCanceled(InputAction.CallbackContext context) => _moveInput = Vector2.zero;
        private void OnLookPerformed(InputAction.CallbackContext context) { }
        private void OnJumpPerformed(InputAction.CallbackContext context) => _jumpQueued = true;
        private void OnSprintPerformed(InputAction.CallbackContext context) => _sprintHeld = true;
        private void OnSprintCanceled(InputAction.CallbackContext context) => _sprintHeld = false;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    public class CarController : MonoBehaviour
    {
        [SerializeField] private float maxSpeed = 18f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float turnSpeed = 90f;
        [SerializeField] private float brakeForce = 8f;
        [SerializeField] private float coastDrag = 2f;
        [SerializeField] private float interactRadius = 4f;

        public string brandName = "Car";
        public Color brandColor = Color.white;
        public int minRep;
        public AudioClip engineClip;

        private bool _isDriving;
        private float _currentSpeed;
        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _playerInRange;
        private AudioSource _engineSource;

        private static InputAction s_moveAction;
        private static InputAction s_brakeAction;
        private static bool s_actionsCreated;
        private static bool s_eConsumedThisFrame;

        public bool IsDriving => _isDriving;
        public bool IsPlayerInRange => _playerInRange;
        public bool IsLocked(Core.GameManager gm) => gm != null && gm.Rep != null && gm.Rep.Reputation < minRep;

        public void Configure(Transform player, Core.GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
        }

        private static void EnsureActions()
        {
            if (s_actionsCreated) return;
            s_actionsCreated = true;
            s_moveAction = new InputAction("CarMove", InputActionType.Value);
            s_moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            s_brakeAction = new InputAction("CarBrake", InputActionType.Button, "<Keyboard>/space");
        }

        private void Update()
        {
            if (_player == null) return;

            _playerInRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;

            bool rawE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            bool ePressed = rawE && !s_eConsumedThisFrame;

            if (_isDriving)
            {
                if (_gameManager.ActiveCar != this)
                {
                    ForceStopDriving();
                    return;
                }
                if (ePressed)
                {
                    s_eConsumedThisFrame = true;
                    StopDriving();
                    return;
                }
                Drive();
            }
            else
            {
                if (ePressed && _playerInRange && !IsLocked(_gameManager) && _gameManager.ActiveCar == null)
                {
                    s_eConsumedThisFrame = true;
                    StartDriving();
                }
            }
        }

        private void LateUpdate()
        {
            s_eConsumedThisFrame = false;
        }

        private void StartDriving()
        {
            EnsureActions();
            _isDriving = true;
            _currentSpeed = 0f;
            s_moveAction.Enable();
            s_brakeAction.Enable();
            if (_gameManager.Audio != null)
                _engineSource = _gameManager.Audio.CreateCarEngine(transform);
            Debug.Log("[CarController] Started driving " + brandName);
            _gameManager.EnterCar(this);
        }

        public void StopDriving()
        {
            _isDriving = false;
            _currentSpeed = 0f;
            StopEngine();
            if (s_moveAction != null) s_moveAction.Disable();
            if (s_brakeAction != null) s_brakeAction.Disable();
            Debug.Log("[CarController] Stopped driving " + brandName);
            _gameManager.ExitCar();
        }

        public void ForceStopDriving()
        {
            if (!_isDriving) return;
            _isDriving = false;
            _currentSpeed = 0f;
            StopEngine();
            if (s_moveAction != null) s_moveAction.Disable();
            if (s_brakeAction != null) s_brakeAction.Disable();
            Debug.Log("[CarController] Force-stopped " + brandName);
        }

        private void StopEngine()
        {
            if (_engineSource != null)
            {
                _engineSource.Stop();
                Destroy(_engineSource.gameObject);
                _engineSource = null;
            }
        }

        private void Drive()
        {
            Vector2 input = s_moveAction.ReadValue<Vector2>();
            bool braking = s_brakeAction.ReadValue<float>() > 0.5f;

            if (braking)
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, brakeForce * Time.deltaTime);
            }
            else if (Mathf.Abs(input.y) > 0.1f)
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, input.y * maxSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, coastDrag * Time.deltaTime);
            }

            float steer = input.x * turnSpeed * Time.deltaTime * Mathf.Clamp01(Mathf.Abs(_currentSpeed) / 3f);
            transform.Rotate(0f, steer, 0f);
            transform.Translate(0f, 0f, _currentSpeed * Time.deltaTime);

            if (_gameManager.Audio != null)
                _gameManager.Audio.UpdateCarEngine(_engineSource, Mathf.Abs(_currentSpeed), engineClip);
        }

        private void OnDestroy()
        {
            StopEngine();
            if (s_moveAction != null) s_moveAction.Disable();
            if (s_brakeAction != null) s_brakeAction.Disable();
        }
    }
}

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

        private const float MaxFuel = 100f;
        private const float FuelDrainPerSecond = 0.8f;
        private float _fuel = MaxFuel;
        private bool _isDriving;
        private float _currentSpeed;
        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _playerInRange;
        private AudioSource _engineSource;
        private float _drivingXpAccumulator;

        private static InputAction s_moveAction;
        private static InputAction s_brakeAction;
        private static InputAction s_radioNext;
        private static InputAction s_radioPrev;
        private static bool s_actionsCreated;
        private static bool s_eConsumedThisFrame;

        public bool IsDriving => _isDriving;
        public bool IsPlayerInRange => _playerInRange;
        public float Fuel => _fuel;
        public float FuelPercent => _fuel / MaxFuel;
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
            s_radioNext = new InputAction("RadioNext", InputActionType.Button, "<Keyboard>/rightArrow");
            s_radioPrev = new InputAction("RadioPrev", InputActionType.Button, "<Keyboard>/leftArrow");
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
            if (_fuel <= 0f) return;
            EnsureActions();
            _isDriving = true;
            _currentSpeed = 0f;
            _drivingXpAccumulator = 0f;
            s_moveAction.Enable();
            s_brakeAction.Enable();
            s_radioNext.Enable();
            s_radioPrev.Enable();
            if (_gameManager.Audio != null)
            {
                _engineSource = _gameManager.Audio.CreateCarEngine(transform);
                _gameManager.Audio.StartRadio(transform);
            }
            Debug.Log("[CarController] Started driving " + brandName);
            _gameManager.EnterCar(this);
        }

        public void StopDriving()
        {
            _isDriving = false;
            _currentSpeed = 0f;
            StopEngine();
            if (_gameManager.Audio != null) _gameManager.Audio.StopRadio();
            if (s_moveAction != null) s_moveAction.Disable();
            if (s_brakeAction != null) s_brakeAction.Disable();
            if (s_radioNext != null) s_radioNext.Disable();
            if (s_radioPrev != null) s_radioPrev.Disable();
            Debug.Log("[CarController] Stopped driving " + brandName);
            _gameManager.ExitCar();
        }

        public void ForceStopDriving()
        {
            if (!_isDriving) return;
            _isDriving = false;
            _currentSpeed = 0f;
            StopEngine();
            if (_gameManager.Audio != null) _gameManager.Audio.StopRadio();
            if (s_moveAction != null) s_moveAction.Disable();
            if (s_brakeAction != null) s_brakeAction.Disable();
            if (s_radioNext != null) s_radioNext.Disable();
            if (s_radioPrev != null) s_radioPrev.Disable();
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

            float fuelMod = _gameManager.Skills != null ? _gameManager.Skills.GetDrivingFuelBonus() : 1f;
            float speedMod = _gameManager.Skills != null ? _gameManager.Skills.GetDrivingSpeedBonus() : 1f;

            if (braking)
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, brakeForce * Time.deltaTime);
            }
            else if (Mathf.Abs(input.y) > 0.1f && _fuel > 0f)
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, input.y * maxSpeed * speedMod, acceleration * Time.deltaTime);
                _fuel -= FuelDrainPerSecond * Time.deltaTime / fuelMod;
                if (_fuel < 0f) _fuel = 0f;
            }
            else
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, coastDrag * Time.deltaTime);
            }

            if (_fuel <= 0f && Mathf.Abs(_currentSpeed) > 0.5f)
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, brakeForce * 2f * Time.deltaTime);

            float speedFactor = Mathf.Clamp01(Mathf.Abs(_currentSpeed) / maxSpeed);
            float dynamicTurn = turnSpeed * Mathf.Lerp(1.2f, 0.6f, speedFactor);
            float steer = input.x * dynamicTurn * Time.deltaTime * Mathf.Clamp01(Mathf.Abs(_currentSpeed) / 3f);
            transform.Rotate(0f, steer, 0f);
            transform.Translate(0f, 0f, _currentSpeed * Time.deltaTime);

            if (_gameManager.Audio != null)
            {
                _gameManager.Audio.UpdateCarEngine(_engineSource, Mathf.Abs(_currentSpeed), engineClip);
                _gameManager.Audio.UpdateRadio(Mathf.Abs(_currentSpeed));
            }

            if (s_radioNext != null && s_radioNext.WasPressedThisFrame())
                _gameManager.Audio?.NextStation();
            if (s_radioPrev != null && s_radioPrev.WasPressedThisFrame())
                _gameManager.Audio?.PrevStation();

            if (Mathf.Abs(_currentSpeed) > 1f)
            {
                _drivingXpAccumulator += Time.deltaTime;
                if (_drivingXpAccumulator >= 3f)
                {
                    _gameManager.Skills?.AddXP(SkillName.Driving, 1);
                    _drivingXpAccumulator = 0f;
                }
            }
        }

        public void Refuel(float amount)
        {
            _fuel = Mathf.Min(_fuel + amount, MaxFuel);
        }

        private void OnDestroy()
        {
            StopEngine();
            if (s_moveAction != null) s_moveAction.Disable();
            if (s_brakeAction != null) s_brakeAction.Disable();
        }
    }
}

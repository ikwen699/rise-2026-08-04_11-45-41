using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    public enum NPCBehavior { Route, Wander, Stand, Guard }
    public class TownNPC : MonoBehaviour
    {
        public Material bodyMaterial;
        public Color bodyTint = new Color(0.6f, 0.5f, 0.5f);
        public Material skinMaterial;
        public string npcName = "Citizen";
        public string[] lines;
        public string marriedLine;

        [SerializeField] private Vector3[] waypoints = System.Array.Empty<Vector3>();
        [SerializeField] private float interactRadius = 3f;
        public float walkSpeed = 1.2f;
        [SerializeField] private float idleMin = 1f;
        [SerializeField] private float idleMax = 4f;
        public NPCBehavior behavior = NPCBehavior.Route;
        public float wanderRadius = 8f;
        public float standLookInterval = 3f;
        public string homeBuilding = "";
        public int homeHourEnter = 18;
        public int homeHourLeave = 6;

        private static readonly System.Collections.Generic.Dictionary<string, Vector3> BuildingPositions = new()
        {
            { "House_01", new Vector3(-40f, 0f, 10f) },
            { "House_02", new Vector3(-20f, 0f, 10f) },
            { "House_03", new Vector3(20f, 0f, 10f) },
            { "House_04", new Vector3(40f, 0f, 10f) },
            { "House_05", new Vector3(56f, 0f, 10f) },
            { "House_06", new Vector3(-40f, 0f, 52f) },
            { "House_07", new Vector3(-20f, 0f, 52f) },
            { "House_08", new Vector3(20f, 0f, 52f) },
            { "House_09", new Vector3(40f, 0f, 52f) },
            { "House_10", new Vector3(56f, 0f, 52f) },
            { "House_11", new Vector3(-40f, 0f, -55f) },
            { "House_12", new Vector3(-20f, 0f, -55f) },
            { "House_13", new Vector3(20f, 0f, -55f) },
            { "House_14", new Vector3(40f, 0f, -55f) },
            { "House_15", new Vector3(56f, 0f, -55f) },
            { "Shop_01", new Vector3(12f, 0f, 6f) },
            { "Shop_02", new Vector3(12f, 0f, -14f) },
            { "TownHall", new Vector3(0f, 0f, -30f) },
            { "Market_01", new Vector3(20f, 0f, 24f) },
            { "Market_02", new Vector3(-22f, 0f, 24f) },
            { "Church", new Vector3(0f, 0f, 42f) },
            { "School", new Vector3(-36f, 0f, 32f) },
            { "Bakery", new Vector3(-22f, 0f, 36f) },
            { "Bank", new Vector3(22f, 0f, 36f) },
            { "Restaurant", new Vector3(22f, 0f, -36f) },
            { "PostOffice", new Vector3(-22f, 0f, -36f) }
        };

        private int _index;
        private float _idleTimer;
        private bool _idling;
        private Vector3 _target;
        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _isOpen;
        private bool _playerInRange;
        private int _lineIndex;

        private Renderer _torsoRenderer;
        private Material _torsoMat;
        private Renderer _leftArmRenderer;
        private Material _leftArmMat;
        private Renderer _rightArmRenderer;
        private Material _rightArmMat;
        private Renderer _leftLegRenderer;
        private Material _leftLegMat;
        private Renderer _rightLegRenderer;
        private Material _rightLegMat;

        private float _outfitTimer;
        private float _outfitInterval = 300f;
        private Color[] _shirtPalette;
        private Color[] _pantsPalette;

        private CharacterController _cc;
        private WalkAnimation _walkAnim;
        private Vector3 _wanderOrigin;
        private float _standLookTimer;

        private bool _isHome;
        private Vector3 _lastRoutePosition;
        private float _homeCheckTimer;

        public bool IsOpen => _isOpen;
        public bool IsPlayerInRange => _playerInRange;

        public void Configure(Transform player, Core.GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
        }

        public void SetRoute(Vector3[] route)
        {
            waypoints = route;
            _index = 0;
            if (waypoints.Length > 0) _target = waypoints[0];
        }

        public string GetDialogueText()
        {
            string line = lines != null && _lineIndex < lines.Length ? lines[_lineIndex] : "...";
            if (_lineIndex == 0 && !string.IsNullOrEmpty(marriedLine) &&
                _gameManager != null && _gameManager.Partner != null && _gameManager.Partner.Married)
            {
                line = marriedLine;
            }
            return npcName + ":\n" + line + "\n\n(press E to continue, walk away to leave)";
        }

        private void Start()
        {
            InitRenderers();
            InitOutfitPalette();
            ApplyOutfitColor(bodyTint, GetPantsForShirt(bodyTint));
            _outfitTimer = Random.Range(60f, _outfitInterval);
            _cc = GetComponent<CharacterController>();
            _walkAnim = GetComponent<WalkAnimation>();
            _wanderOrigin = transform.position;
            _standLookTimer = Random.Range(1f, standLookInterval);

            if (behavior == NPCBehavior.Route && waypoints.Length > 0)
            {
                _target = waypoints[0];
                _index = 1;
            }
            else if (behavior == NPCBehavior.Wander)
            {
                _target = GetRandomWanderPoint();
            }
        }

        private void InitRenderers()
        {
            Transform torso = FindPart("Body_Torso");
            Transform armL = FindPart("Arm_L");
            Transform armR = FindPart("Arm_R");
            Transform legL = FindPart("Leg_L");
            Transform legR = FindPart("Leg_R");

            if (torso != null) { _torsoRenderer = torso.GetComponent<Renderer>(); _torsoMat = InstanceMat(_torsoRenderer); }
            if (armL != null) { _leftArmRenderer = armL.GetComponent<Renderer>(); _leftArmMat = InstanceMat(_leftArmRenderer); }
            if (armR != null) { _rightArmRenderer = armR.GetComponent<Renderer>(); _rightArmMat = InstanceMat(_rightArmRenderer); }
            if (legL != null) { _leftLegRenderer = legL.GetComponent<Renderer>(); _leftLegMat = InstanceMat(_leftLegRenderer); }
            if (legR != null) { _rightLegRenderer = legR.GetComponent<Renderer>(); _rightLegMat = InstanceMat(_rightLegRenderer); }
        }

        private void InitOutfitPalette()
        {
            _shirtPalette = new[]
            {
                new Color(0.85f, 0.40f, 0.45f), new Color(0.35f, 0.55f, 0.85f), new Color(0.40f, 0.70f, 0.45f),
                new Color(0.85f, 0.75f, 0.30f), new Color(0.60f, 0.45f, 0.75f), new Color(0.20f, 0.60f, 0.65f),
                new Color(0.90f, 0.55f, 0.20f), new Color(0.15f, 0.15f, 0.18f), new Color(0.55f, 0.20f, 0.70f),
                new Color(0.85f, 0.82f, 0.78f), new Color(0.85f, 0.72f, 0.30f), new Color(0.12f, 0.40f, 0.65f),
                new Color(0.70f, 0.25f, 0.25f), new Color(0.30f, 0.60f, 0.35f), new Color(0.75f, 0.50f, 0.30f)
            };
            _pantsPalette = new[]
            {
                new Color(0.20f, 0.20f, 0.25f), new Color(0.30f, 0.30f, 0.35f), new Color(0.25f, 0.25f, 0.20f),
                new Color(0.15f, 0.15f, 0.18f), new Color(0.35f, 0.25f, 0.18f), new Color(0.20f, 0.30f, 0.50f),
                new Color(0.10f, 0.10f, 0.12f)
            };
        }

        private void ApplyOutfitColor(Color shirt, Color pants)
        {
            if (_torsoMat != null) _torsoMat.color = shirt;
            if (_leftArmMat != null) _leftArmMat.color = shirt;
            if (_rightArmMat != null) _rightArmMat.color = shirt;
            if (_leftLegMat != null) _leftLegMat.color = pants;
            if (_rightLegMat != null) _rightLegMat.color = pants;
        }

        private Vector3 GetRandomWanderPoint()
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            return _wanderOrigin + new Vector3(offset.x, 0f, offset.y);
        }

        private Color GetPantsForShirt(Color shirt)
        {
            return _pantsPalette != null ? _pantsPalette[Random.Range(0, _pantsPalette.Length)] : new Color(0.2f, 0.2f, 0.25f);
        }

        private void ChangeOutfit()
        {
            if (_shirtPalette == null || _shirtPalette.Length == 0) return;
            Color newShirt = _shirtPalette[Random.Range(0, _shirtPalette.Length)];
            Color newPants = GetPantsForShirt(newShirt);
            ApplyOutfitColor(newShirt, newPants);
        }

        private Transform FindPart(string name)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        private Material InstanceMat(Renderer r)
        {
            if (r == null) return null;
            Material m = new Material(r.sharedMaterial);
            r.material = m;
            return m;
        }

        private void Update()
        {
            if (_gameManager == null || _player == null) return;
            if (_isHome) return;

            _playerInRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;

            if (!string.IsNullOrEmpty(homeBuilding) && _gameManager != null && _gameManager.Clock != null)
            {
                _homeCheckTimer -= Time.deltaTime;
                if (_homeCheckTimer <= 0f)
                {
                    _homeCheckTimer = 2f;
                    int hour = Mathf.FloorToInt(_gameManager.Clock.HourOfDay) % 24;
                    bool stormy = _gameManager.Weather != null && _gameManager.Weather.IsStormy();
                    if (hour >= homeHourEnter || hour < homeHourLeave || stormy)
                    {
                        if (!_isHome) { GoHome(); return; }
                    }
                    else
                    {
                        if (_isHome) { LeaveHome(); return; }
                    }
                }
            }

            if (_isOpen && !_playerInRange)
            {
                Close();
                return;
            }

            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (ePressed && _playerInRange)
            {
                if (_isOpen)
                {
                    if (lines == null || _lineIndex >= lines.Length - 1) Close();
                    else _lineIndex++;
                }
                else
                {
                    Open();
                }
            }

            if (_isOpen) return;

            _outfitTimer -= Time.deltaTime;
            if (_outfitTimer <= 0f)
            {
                ChangeOutfit();
                _outfitTimer = Random.Range(60f, _outfitInterval);
            }

            if (behavior == NPCBehavior.Stand || behavior == NPCBehavior.Guard)
            {
                _standLookTimer -= Time.deltaTime;
                if (_standLookTimer <= 0f)
                {
                    Vector3 lookDir = (behavior == NPCBehavior.Guard)
                        ? (Vector3.forward * Random.Range(-1f, 1f) + Vector3.right * Random.Range(-1f, 1f)).normalized
                        : new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                    if (lookDir.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 0.3f);
                    _standLookTimer = Random.Range(1.5f, standLookInterval);
                }
                if (_walkAnim != null) _walkAnim.SetSpeed(0f);
                return;
            }

            if (_idling)
            {
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f)
                {
                    _idling = false;
                    if (behavior == NPCBehavior.Wander)
                        _target = GetRandomWanderPoint();
                }
                if (_walkAnim != null) _walkAnim.SetSpeed(0f);
                return;
            }

            Vector3 to = _target - transform.position;
            to.y = 0f;

            if (to.magnitude <= 0.3f)
            {
                _idling = true;
                _idleTimer = Random.Range(idleMin, idleMax);
                if (behavior == NPCBehavior.Route && waypoints.Length > 0)
                {
                    _target = waypoints[_index];
                    _index = (_index + 1) % waypoints.Length;
                }
                return;
            }

            Vector3 motion = to.normalized * walkSpeed + Vector3.down * 2f;
            if (_cc != null && _cc.enabled) _cc.Move(motion * Time.deltaTime);
            else transform.position += to.normalized * walkSpeed * Time.deltaTime;
            if (to.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(to.normalized);
            }

            if (_walkAnim != null) _walkAnim.SetSpeed(walkSpeed);
        }

        private void Open()
        {
            _isOpen = true;
            _lineIndex = 0;
            _gameManager.ActiveTownNPC = this;
            _gameManager.ActiveShop = null;
            float repBonus = _gameManager.Skills != null ? _gameManager.Skills.GetReputationBonus() : 1f;
            _gameManager.Rep?.AddReputation(Mathf.RoundToInt(2 * repBonus));
            _gameManager.Skills?.AddXP(SkillName.Charisma, 3);
        }

        private void Close()
        {
            _isOpen = false;
            _lineIndex = 0;
            if (_gameManager.ActiveTownNPC == this) _gameManager.ActiveTownNPC = null;
        }

        private void GoHome()
        {
            if (string.IsNullOrEmpty(homeBuilding)) return;
            if (!BuildingPositions.TryGetValue(homeBuilding, out Vector3 homePos)) return;
            _lastRoutePosition = transform.position;
            _isHome = true;
            transform.position = homePos + Vector3.up * 0.1f;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
                r.enabled = false;
            if (_cc != null) _cc.enabled = false;
            if (_walkAnim != null) _walkAnim.SetSpeed(0f);
        }

        private void LeaveHome()
        {
            _isHome = false;
            transform.position = _lastRoutePosition;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
                r.enabled = true;
            if (_cc != null) _cc.enabled = true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}

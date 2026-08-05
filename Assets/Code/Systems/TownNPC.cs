using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
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

            if (waypoints.Length > 0)
            {
                _target = waypoints[0];
                _index = 1;
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

            _playerInRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;

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

            if (waypoints == null || waypoints.Length == 0) return;

            if (_idling)
            {
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0f) _idling = false;
                return;
            }

            Vector3 to = _target - transform.position;
            to.y = 0f;

            if (to.magnitude <= 0.2f)
            {
                _idling = true;
                _idleTimer = Random.Range(idleMin, idleMax);
                _target = waypoints[_index];
                _index = (_index + 1) % waypoints.Length;
                return;
            }

            Vector3 motion = to.normalized * walkSpeed + Vector3.down * 2f;
            if (_cc != null) _cc.Move(motion * Time.deltaTime);
            else transform.position += to.normalized * walkSpeed * Time.deltaTime;
            if (to.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(to.normalized);
            }

            bool moving = !_idling && !_isOpen && waypoints != null && waypoints.Length > 0;
            if (_walkAnim != null) _walkAnim.SetSpeed(moving ? walkSpeed : 0f);

            _outfitTimer -= Time.deltaTime;
            if (_outfitTimer <= 0f)
            {
                ChangeOutfit();
                _outfitTimer = Random.Range(60f, _outfitInterval);
            }
        }

        private void Open()
        {
            _isOpen = true;
            _lineIndex = 0;
            _gameManager.ActiveTownNPC = this;
            _gameManager.ActiveShop = null;
            _gameManager.Rep?.AddReputation(2);
        }

        private void Close()
        {
            _isOpen = false;
            _lineIndex = 0;
            if (_gameManager.ActiveTownNPC == this) _gameManager.ActiveTownNPC = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}

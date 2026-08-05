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
            Renderer bodyR = transform.Find("Body")?.GetComponent<Renderer>();
            if (bodyR != null && bodyMaterial != null)
            {
                bodyR.material = new Material(bodyMaterial);
                bodyR.material.color = bodyTint;
            }

            if (waypoints.Length > 0)
            {
                _target = waypoints[0];
                _index = 1;
            }
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

            transform.position += to.normalized * walkSpeed * Time.deltaTime;
            if (to.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(to.normalized);
            }
        }

        private void Open()
        {
            _isOpen = true;
            _lineIndex = 0;
            _gameManager.ActiveTownNPC = this;
            _gameManager.ActiveShop = null;
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

using UnityEngine;
using UnityEngine.InputSystem;
using Rise.Systems;

namespace Rise.Core
{
    public class WorkStation : MonoBehaviour
    {
        [SerializeField] private JobDefinition job;
        [SerializeField] private float interactRadius = 3f;

        private Transform _player;
        private GameManager _gameManager;
        private bool _wasPressed;
        private Renderer _renderer;
        private bool _lastUnlocked = true;

        public JobDefinition Job => job;
        public bool IsPlayerInRange { get; private set; }
        public bool IsUnlocked => _gameManager != null && _gameManager.Jobs != null && _gameManager.Jobs.IsUnlocked(job);

        public void Configure(Transform player, GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
        }

        public void SetJob(JobDefinition definition)
        {
            job = definition;
        }

        private void Start()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _renderer.material = new Material(_renderer.sharedMaterial);
            }
        }

        private void Update()
        {
            if (_gameManager == null || _player == null || job == null) return;

            bool inRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;
            IsPlayerInRange = inRange;

            if (_gameManager.Jobs.IsWorking && _gameManager.Jobs.CurrentStation == this && !inRange)
            {
                _gameManager.Jobs.StopWorking();
            }

            if (inRange &&
                Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame &&
                !_gameManager.Jobs.IsWorking)
            {
                _gameManager.ToggleWork(job, this);
            }

            if (_renderer != null)
            {
                bool unlocked = IsUnlocked;
                if (unlocked != _lastUnlocked)
                {
                    _lastUnlocked = unlocked;
                    _renderer.material.color = unlocked
                        ? new Color(1f, 0.8f, 0.1f)
                        : new Color(0.45f, 0.45f, 0.48f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
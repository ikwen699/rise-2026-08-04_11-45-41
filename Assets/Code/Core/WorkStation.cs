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

        public JobDefinition Job => job;

        public void Configure(Transform player, GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
        }

        public void SetJob(JobDefinition definition)
        {
            job = definition;
        }

        private void Update()
        {
            if (_gameManager == null || _player == null || job == null) return;

            bool inRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;

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
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
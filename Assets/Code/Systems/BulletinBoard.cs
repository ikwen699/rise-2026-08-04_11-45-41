using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    public class BulletinBoard : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 3f;

        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _playerInRange;
        private bool _isOpen;
        private float _updateTimer;
        private string _currentNews = "";

        public bool IsOpen => _isOpen;
        public bool IsPlayerInRange => _playerInRange;

        public void Configure(Transform player, Core.GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
            UpdateNews();
        }

        private void Update()
        {
            if (_player == null || _gameManager == null) return;

            _playerInRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;

            _updateTimer -= Time.deltaTime;
            if (_updateTimer <= 0f)
            {
                _updateTimer = 300f;
                UpdateNews();
            }

            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (ePressed && _playerInRange)
            {
                _isOpen = !_isOpen;
            }

            if (_isOpen && !_playerInRange)
            {
                _isOpen = false;
            }
        }

        public string GetBulletinText()
        {
            return "<b>Town Bulletin</b>\n\n" + _currentNews + "\n\n(press E to close)";
        }

        private void UpdateNews()
        {
            System.Collections.Generic.List<string> news = new();

            if (_gameManager.Clock != null)
            {
                int day = _gameManager.Clock.Day;
                if (day >= 10 && day < 15)
                    news.Add("Town Fair this week! Visit the park.");
                if (day >= 20 && day < 25)
                    news.Add("Holiday Market opening soon!");
                if (day >= 30 && day < 35)
                    news.Add("Charity Run happening — all welcome!");
                if (day >= 40)
                    news.Add("Grand Festival — biggest event of the year!");
                if (day < 5)
                    news.Add("Welcome to town! Try the work spots nearby.");
            }

            if (_gameManager.Weather != null)
            {
                if (_gameManager.Weather.IsStormy())
                    news.Add("Weather alert: Storms expected. Stay indoors!");
                else if (_gameManager.Weather.IsRaining())
                    news.Add("Rainy day — umbrella recommended.");
            }

            if (_gameManager.Properties != null && _gameManager.Properties.GetOwnedPropertyCount() >= 3)
                news.Add("Property values rising! Good time to invest.");

            if (_gameManager.Rival != null && !_gameManager.Rival.IsDefeated)
            {
                int diff = _gameManager.Wallet.Money - Mathf.RoundToInt(_gameManager.Rival.RivalMoney);
                if (diff < 0)
                    news.Add("Rival Marcus Blackwood is gaining ground!");
            }

            if (_gameManager.Jobs != null)
            {
                if (_gameManager.Jobs.TotalEarned >= 100 && _gameManager.Jobs.TotalEarned < 200)
                    news.Add("Delivery Driver position now available!");
                if (_gameManager.Jobs.TotalEarned >= 300 && _gameManager.Jobs.TotalEarned < 400)
                    news.Add("Baker position open at the Bakery!");
                if (_gameManager.Jobs.TotalEarned >= 500 && _gameManager.Jobs.TotalEarned < 600)
                    news.Add("Chef position open at the Restaurant!");
                if (_gameManager.Jobs.TotalEarned >= 750 && _gameManager.Jobs.TotalEarned < 850)
                    news.Add("Bank Teller position available at the Bank!");
            }

            if (news.Count == 0)
                news.Add("Everything is quiet in town today.");

            _currentNews = string.Join("\n\n", news);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}

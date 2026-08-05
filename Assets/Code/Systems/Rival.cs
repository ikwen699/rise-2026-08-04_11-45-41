using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    public class Rival : MonoBehaviour
    {
        [Header("Rival")]
        [SerializeField] private float rivalGrowthRate = 2.5f;
        [SerializeField] private float rivalRepGrowthRate = 0.15f;
        [SerializeField] private float interactRadius = 3f;
        [SerializeField] private string rivalName = "Marcus Blackwood";

        private Core.GameManager _gameManager;
        private bool _isOpen;
        private bool _playerInRange;
        private int _lineIndex;
        private bool _tauntedThisMeet;

        public float RivalMoney { get; private set; }
        public float RivalRep { get; private set; }
        public bool IsDefeated { get; private set; }

        public bool IsOpen => _isOpen;
        public bool IsPlayerInRange => _playerInRange;
        public bool PlayerAhead => _gameManager != null && _gameManager.Wallet.Money >= RivalMoney;
        public string RivalName => rivalName;

        private void Update()
        {
            if (_gameManager == null || IsDefeated) return;

            RivalMoney += rivalGrowthRate * Time.deltaTime / _gameManager.Clock.SecondsPerGameHour;
            RivalRep += rivalRepGrowthRate * Time.deltaTime / _gameManager.Clock.SecondsPerGameHour;

            if (RivalRep > 100f) RivalRep = 100f;
            if (RivalMoney > 15000f) RivalMoney = 15000f;

            Transform player = _gameManager != null ? GameObject.Find("Player")?.transform : null;
            if (player == null) return;
            _playerInRange = Vector3.Distance(transform.position, player.position) <= interactRadius;

            if (_isOpen && !_playerInRange)
            {
                Close();
                return;
            }

            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (ePressed && _playerInRange)
            {
                if (_isOpen) Close();
                else Open();
            }

            int playerMoney = _gameManager != null ? _gameManager.Wallet.Money : 0;
            int playerRep = _gameManager != null && _gameManager.Rep != null ? _gameManager.Rep.Reputation : 0;
            if (!IsDefeated && playerMoney > RivalMoney + 2000 && playerRep >= 80)
            {
                IsDefeated = true;
                _gameManager.Phone?.Push("Rival Defeated!", rivalName + " has left town. You are the success story!");
                _gameManager.Rep?.AddReputation(15);
            }
        }

        public void Configure(Core.GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void ApplySaved(float money, float rep, bool defeated)
        {
            RivalMoney = money;
            RivalRep = rep;
            IsDefeated = defeated;
        }

        private void Open()
        {
            _isOpen = true;
            _lineIndex = 0;
            _tauntedThisMeet = false;
            _gameManager.ActiveTownNPC = null;
            _gameManager.ActiveShop = null;
        }

        private void Close()
        {
            _isOpen = false;
        }

        public string GetDialogueText()
        {
            if (IsDefeated)
                return rivalName + ":\nYou win. I can't compete with you. Good luck.";

            int playerMoney = _gameManager != null ? _gameManager.Wallet.Money : 0;
            int playerRep = _gameManager != null && _gameManager.Rep != null ? _gameManager.Rep.Reputation : 0;

            string line;
            if (playerMoney > RivalMoney)
            {
                float margin = playerMoney - RivalMoney;
                line = "Not bad... $" + Mathf.RoundToInt(margin) + " ahead of me. Keep trying.";
                if (playerRep >= 80 && !_tauntedThisMeet)
                {
                    line = "You think you're better than me? I built my empire from nothing!";
                    _tauntedThisMeet = true;
                }
            }
            else
            {
                float gap = RivalMoney - playerMoney;
                line = "Ha! I'm $" + Mathf.RoundToInt(gap) + " ahead of you. You'll never catch up.";
                if (RivalRep > playerRep + 20)
                    line += " And my reputation? Legendary. You're nobody.";
            }

            if (_lineIndex == 0)
                return rivalName + ":\n" + line + "\n\n(E to close)";
            if (_lineIndex == 1)
                return rivalName + ":\nI've got a business empire to run. What do you want?";

            return rivalName + ":\nGet out of my sight.";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}

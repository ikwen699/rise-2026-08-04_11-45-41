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
        private bool _notified5k;
        private bool _notified10k;
        private bool _notifiedFirstAhead;

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

            if (!_notified5k && RivalMoney >= 5000f)
            {
                _notified5k = true;
                _gameManager.Phone?.Push("Rival Update", rivalName + " just hit $5k. The race is on.");
            }
            if (!_notified10k && RivalMoney >= 10000f)
            {
                _notified10k = true;
                _gameManager.Phone?.Push("Rival Alert", rivalName + " reached $10k. You need to step it up.");
            }

            int playerMoney = _gameManager != null ? _gameManager.Wallet.Money : 0;
            if (!_notifiedFirstAhead && playerMoney > RivalMoney && RivalMoney > 1000f)
            {
                _notifiedFirstAhead = true;
                _gameManager.Phone?.Push("You're Ahead!", "For the first time, you've out-earned " + rivalName + "!");
            }

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

            if (!IsDefeated && playerMoney > RivalMoney + 2000 && _gameManager.Rep != null && _gameManager.Rep.Reputation >= 80)
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
            _notified5k = money >= 5000f;
            _notified10k = money >= 10000f;
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
                return rivalName + ":\nYou win. I can't compete with you. Good luck. I'll... figure something out.";

            int playerMoney = _gameManager != null ? _gameManager.Wallet.Money : 0;
            int playerRep = _gameManager != null && _gameManager.Rep != null ? _gameManager.Rep.Reputation : 0;

            string line;
            if (_lineIndex == 0)
            {
                float diff = playerMoney - RivalMoney;
                if (diff > 2000 && playerRep >= 80)
                {
                    line = "Fine. You win. I underestimated you. Don't let it go to your head.";
                    _tauntedThisMeet = true;
                }
                else if (diff > 500)
                {
                    line = "Not bad... $" + Mathf.RoundToInt(diff) + " ahead of me. But I'm just getting started.";
                    if (playerRep >= 60 && !_tauntedThisMeet)
                    {
                        line = "You think you're better than me? I built my empire from nothing! You just got lucky.";
                        _tauntedThisMeet = true;
                    }
                }
                else if (diff > -500 && diff <= 500)
                {
                    line = "We're neck and neck. But I don't lose. Not ever.";
                }
                else if (diff > -5000)
                {
                    float gap = RivalMoney - playerMoney;
                    line = "Ha! I'm $" + Mathf.RoundToInt(gap) + " ahead of you. You'll never catch up.";
                    if (RivalRep > playerRep + 20)
                        line += " And my reputation? Legendary. You're nobody.";
                }
                else
                {
                    line = "You're wasting your time. Go back to the streets where you belong.";
                }
            }
            else if (_lineIndex == 1)
            {
                if (_tauntedThisMeet)
                    line = "I've said too much already. Leave me alone.";
                else
                    line = "I've got a business empire to run. What do you want?";
            }
            else
            {
                line = "Get out of my sight.";
            }

            return rivalName + ":\n" + line + "\n\n(E to close)";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}

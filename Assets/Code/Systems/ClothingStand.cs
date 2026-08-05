using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    [Serializable]
    public class ClothingItemData
    {
        public string itemName;
        public int price;
        public int outfitIndex;
        public int minReputation;
    }

    public class ClothingStand : MonoBehaviour
    {
        [SerializeField] private ClothingItemData[] items;
        [SerializeField] private float interactRadius = 3f;

        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _isOpen;
        private bool _playerInRange;
        private string _message = "";
        private float _messageTimer;

        public bool IsOpen => _isOpen;
        public bool IsPlayerInRange => _playerInRange;

        public void Configure(Transform player, Core.GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
        }

        public void SetItems(ClothingItemData[] shopItems)
        {
            items = shopItems;
        }

        private void Update()
        {
            if (_gameManager == null || _player == null || items == null || items.Length == 0) return;

            _playerInRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;

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

            if (_isOpen) HandleNumberKeys();

            if (_messageTimer > 0f) _messageTimer -= Time.deltaTime;
        }

        private void Open()
        {
            _isOpen = true;
            _gameManager.ActiveClothingShop = this;
            _message = "";
        }

        private void Close()
        {
            _isOpen = false;
            if (_gameManager.ActiveClothingShop == this) _gameManager.ActiveClothingShop = null;
            _message = "";
        }

        private void HandleNumberKeys()
        {
            if (Keyboard.current == null) return;

            int count = Mathf.Min(items.Length, 9);
            for (int i = 0; i < count; i++)
            {
                if (Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
                {
                    Buy(i);
                    break;
                }
            }
        }

        private void Buy(int index)
        {
            ClothingItemData item = items[index];
            int reputation = _gameManager.Rep != null ? _gameManager.Rep.Reputation : 0;

            if (reputation < item.minReputation)
            {
                SetMessage("Rep " + item.minReputation + " required for " + item.itemName);
                return;
            }

            PlayerAppearance appearance = _player.GetComponent<PlayerAppearance>();
            if (appearance == null)
            {
                SetMessage("Cannot change outfit right now.");
                return;
            }

            if (index == appearance.CurrentOutfitIndex)
            {
                SetMessage("Already wearing " + item.itemName);
                return;
            }

            int adjustedPrice = item.price;
            if (_gameManager.Rep != null && item.price > 0)
            {
                float discount = _gameManager.Rep.GetShopDiscount();
                adjustedPrice = Mathf.RoundToInt(item.price * (1f + discount));
                if (adjustedPrice < 0) adjustedPrice = 0;
            }

            if (adjustedPrice == 0 || _gameManager.Wallet.CanAfford(adjustedPrice))
            {
                if (adjustedPrice > 0) _gameManager.Wallet.Spend(adjustedPrice);
                appearance.TryBuyOutfit(index);
                _gameManager.Rep?.AddReputation(1);
                SetMessage("Wearing " + item.itemName + "!");
            }
            else
            {
                SetMessage("Not enough money for " + item.itemName + " ($" + adjustedPrice + ")");
            }
        }

        private void SetMessage(string text)
        {
            _message = text;
            _messageTimer = 2.5f;
        }

        public string GetMenuText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-- DESIGNER BOUTIQUE --");

            PlayerAppearance appearance = _player != null ? _player.GetComponent<PlayerAppearance>() : null;
            int currentOutfit = appearance != null ? appearance.CurrentOutfitIndex : 0;
            int reputation = _gameManager.Rep != null ? _gameManager.Rep.Reputation : 0;

            for (int i = 0; i < items.Length; i++)
            {
                string status = "";
                if (i == currentOutfit) status = "  (wearing)";
                else if (reputation < items[i].minReputation) status = "  [Rep " + items[i].minReputation + " required]";
                else status = "  $" + items[i].price;

                sb.AppendLine((i + 1) + ". " + items[i].itemName + status);
            }
            sb.Append("Press 1-" + Mathf.Min(items.Length, 9) + " to try   E to close");

            if (_messageTimer > 0f)
            {
                sb.AppendLine();
                sb.Append(_message);
            }
            return sb.ToString();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.5f, 0.9f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}

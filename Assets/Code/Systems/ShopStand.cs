using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    public enum ShopItemType
    {
        Item,
        Food,
        GiftFlower,
        GiftChocolate,
        GiftRing
    }

    [Serializable]
    public class ShopItemData
    {
        public string itemName = "Item";
        public int price = 10;
        public ShopItemType itemType = ShopItemType.Item;
    }

    public class ShopStand : MonoBehaviour
    {
        [SerializeField] private List<ShopItemData> items = new List<ShopItemData>();
        [SerializeField] private float interactRadius = 3f;

        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _isOpen;
        private bool _playerInRange;
        private string _message = "";
        private float _messageTimer;

        public bool IsOpen => _isOpen;
        public bool IsPlayerInRange => _playerInRange;
        public int ItemCount => items.Count;

        public void Configure(Transform player, Core.GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
        }

        public void SetItems(List<ShopItemData> shopItems)
        {
            items = shopItems;
        }

        private void Update()
        {
            if (_gameManager == null || _player == null || items.Count == 0) return;

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

            if (_isOpen)
            {
                HandleNumberKeys();
            }

            if (_messageTimer > 0f) _messageTimer -= Time.deltaTime;
        }

        private void Open()
        {
            _isOpen = true;
            _gameManager.ActiveShop = this;
            _message = "";
        }

        private void Close()
        {
            _isOpen = false;
            if (_gameManager.ActiveShop == this) _gameManager.ActiveShop = null;
            _message = "";
        }

        private void HandleNumberKeys()
        {
            if (Keyboard.current == null) return;

            int count = Mathf.Min(items.Count, 9);
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
            ShopItemData item = items[index];
            if (_gameManager.Wallet.CanAfford(item.price))
            {
                _gameManager.Wallet.Spend(item.price);
                switch (item.itemType)
                {
                    case ShopItemType.Food:
                        _gameManager.Needs.AddFood();
                        SetMessage("Bought " + item.itemName + " x1  (press Q to eat)");
                        break;
                    case ShopItemType.GiftFlower:
                        _gameManager.Needs.AddGift(ShopItemType.GiftFlower);
                        SetMessage("Bought " + item.itemName + " (a gift)");
                        break;
                    case ShopItemType.GiftChocolate:
                        _gameManager.Needs.AddGift(ShopItemType.GiftChocolate);
                        SetMessage("Bought " + item.itemName + " (a gift)");
                        break;
                    case ShopItemType.GiftRing:
                        _gameManager.Needs.AddGift(ShopItemType.GiftRing);
                        SetMessage("Bought " + item.itemName + " (a gift)");
                        break;
                    default:
                        SetMessage("Bought " + item.itemName + " for $" + item.price);
                        break;
                }
            }
            else
            {
                SetMessage("Not enough money for " + item.itemName + " ($" + item.price + ")");
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
            sb.AppendLine("-- SHOP --");
            for (int i = 0; i < items.Count; i++)
            {
                string tag = "";
                if (items[i].itemType == ShopItemType.Food) tag = "  (food)";
                else if (items[i].itemType != ShopItemType.Item) tag = "  (gift)";
                sb.AppendLine((i + 1) + ". " + items[i].itemName + "   $" + items[i].price + tag);
            }
            sb.Append("Press 1-" + Mathf.Min(items.Count, 9) + " to buy   E to close");

            if (_messageTimer > 0f)
            {
                sb.AppendLine();
                sb.Append(_message);
            }
            return sb.ToString();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.9f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
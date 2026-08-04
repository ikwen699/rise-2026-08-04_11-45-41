using System;
using UnityEngine;

namespace Rise.Systems
{
    public class Wallet : MonoBehaviour
    {
        [SerializeField] private int money = 0;

        public int Money => money;

        public event Action<int> OnMoneyChanged;

        public void SetMoney(int value)
        {
            value = Mathf.Max(0, value);
            if (money == value) return;
            money = value;
            OnMoneyChanged?.Invoke(money);
        }

        public void Add(int amount) => SetMoney(money + amount);
        public void Spend(int amount) => SetMoney(money - amount);
        public bool CanAfford(int amount) => money >= amount;
    }
}
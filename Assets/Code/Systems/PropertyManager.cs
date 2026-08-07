using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rise.Systems
{
    [Serializable]
    public class PropertyData
    {
        public string buildingName;
        public int cost;
        public float incomePerHour;
        public bool owned;
    }

    public class PropertyManager : MonoBehaviour
    {
        private Core.GameManager _gameManager;
        private readonly List<PropertyData> _properties = new List<PropertyData>();

        public int PropertyCount => _properties.Count;
        public event Action<string, bool> OnPropertyChanged;
        public event Action<int> OnIncomeCollected;

        public void Configure(Core.GameManager gameManager)
        {
            _gameManager = gameManager;
            if (_properties.Count == 0) InitProperties();
        }

        public void ApplySaved(string[] owned)
        {
            if (owned == null) return;
            foreach (string name in owned)
            {
                PropertyData p = GetProperty(name);
                if (p != null) p.owned = true;
            }
        }

        public string[] GetOwnedNames()
        {
            List<string> names = new List<string>();
            foreach (PropertyData p in _properties)
                if (p.owned) names.Add(p.buildingName);
            return names.ToArray();
        }

        public int GetOwnedPropertyCount()
        {
            int count = 0;
            foreach (PropertyData p in _properties)
                if (p.owned) count++;
            return count;
        }

        private void InitProperties()
        {
            _properties.Clear();
            AddProperty("House_01", 2000, 5f);
            AddProperty("House_02", 2000, 5f);
            AddProperty("House_03", 2000, 5f);
            AddProperty("House_04", 2000, 5f);
            AddProperty("House_05", 2000, 5f);
            AddProperty("House_06", 2000, 5f);
            AddProperty("House_07", 2000, 5f);
            AddProperty("Shop_01", 5000, 15f);
            AddProperty("Shop_02", 5000, 15f);
            AddProperty("Bakery", 5000, 15f);
            AddProperty("Restaurant", 5000, 15f);
            AddProperty("Market_01", 3000, 10f);
            AddProperty("Market_02", 3000, 10f);
            AddProperty("School", 10000, 25f);
            AddProperty("Bank", 10000, 25f);
            AddProperty("PostOffice", 10000, 25f);
            AddProperty("TownHall", 10000, 25f);
            AddProperty("Church", 15000, 10f);
        }

        private void AddProperty(string name, int cost, float income)
        {
            _properties.Add(new PropertyData { buildingName = name, cost = cost, incomePerHour = income, owned = false });
        }

        public PropertyData GetProperty(string name)
        {
            foreach (PropertyData p in _properties)
                if (p.buildingName == name) return p;
            return null;
        }

        public bool BuyProperty(string name)
        {
            PropertyData p = GetProperty(name);
            if (p == null || p.owned) return false;
            if (_gameManager == null || _gameManager.Wallet == null) return false;
            if (_gameManager.Wallet.Money < p.cost) return false;

            _gameManager.Wallet.Spend(p.cost);
            p.owned = true;
            _gameManager.Rep?.AddReputation(5);
            _gameManager.Phone?.Push("Property Bought!", "You now own " + name + ". Income: $" + p.incomePerHour + "/hr");
            OnPropertyChanged?.Invoke(name, true);
            return true;
        }

        public void CollectIncome(float gameHours)
        {
            if (_gameManager == null || _gameManager.Wallet == null) return;
            int totalIncome = 0;
            foreach (PropertyData p in _properties)
            {
                if (p.owned)
                {
                    int income = Mathf.FloorToInt(p.incomePerHour * gameHours);
                    if (income > 0) totalIncome += income;
                }
            }
            if (totalIncome > 0)
            {
                _gameManager.Wallet.Add(totalIncome);
                OnIncomeCollected?.Invoke(totalIncome);
            }
        }

        public int GetTotalIncome()
        {
            int total = 0;
            foreach (PropertyData p in _properties)
                if (p.owned) total += Mathf.RoundToInt(p.incomePerHour);
            return total;
        }
    }
}

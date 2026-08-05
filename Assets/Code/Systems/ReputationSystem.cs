using System;
using UnityEngine;

namespace Rise.Systems
{
    public class ReputationSystem : MonoBehaviour
    {
        [SerializeField] private int minReputation = -100;
        [SerializeField] private int maxReputation = 100;

        private Core.GameManager _gameManager;

        public int Reputation { get; private set; }
        public int MinReputation => minReputation;
        public int MaxReputation => maxReputation;

        public event Action<int> OnReputationChanged;

        public void Configure(Core.GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void ApplySaved(int rep)
        {
            Reputation = Mathf.Clamp(rep, minReputation, maxReputation);
        }

        public void AddReputation(int amount)
        {
            int prev = Reputation;
            Reputation = Mathf.Clamp(Reputation + amount, minReputation, maxReputation);
            if (Reputation != prev) OnReputationChanged?.Invoke(Reputation);
        }

        public float GetShopDiscount()
        {
            if (Reputation >= 100) return 0.20f;
            if (Reputation >= 50) return 0.10f;
            if (Reputation < 0) return -0.20f;
            return 0f;
        }

        public string GetRepTierText()
        {
            if (Reputation >= 100) return "Legendary";
            if (Reputation >= 80) return "Renowned";
            if (Reputation >= 50) return "Respected";
            if (Reputation >= 20) return "Known";
            if (Reputation >= 0) return "Neutral";
            if (Reputation >= -30) return "Disliked";
            return "Outcast";
        }
    }
}

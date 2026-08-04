using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    public class PlayerNeeds : MonoBehaviour
    {
        [Header("Energy")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float workDrainPerSecond = 1.2f;
        [SerializeField] private float regenPerSecond = 3f;

        [Header("Hunger")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float hungerPerGameHour = 4f;

        [Header("Food")]
        [SerializeField] private float foodHungerRestore = 35f;
        [SerializeField] private float foodEnergyRestore = 25f;

        private Core.GameManager _gameManager;
        private string _message = "";
        private float _messageTimer;

        public event Action OnExhausted;

        public float Energy { get; private set; } = 100f;
        public float Hunger { get; private set; } = 100f;
        public int FoodCount { get; private set; }

        public float MaxEnergy => maxEnergy;
        public float MaxHunger => maxHunger;
        public bool IsStarving => Hunger <= 0f;
        public bool HasMessage => _messageTimer > 0f;
        public string Message => _message;

        public string NeedsText =>
            "Energy " + Mathf.RoundToInt(Energy) + "/" + Mathf.RoundToInt(maxEnergy) +
            "   Hunger " + Mathf.RoundToInt(Hunger) + "/" + Mathf.RoundToInt(maxHunger) +
            "   Food x" + FoodCount;

        public void Configure(Core.GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void ApplySaved(float energy, float hunger, int food)
        {
            Energy = energy;
            Hunger = hunger;
            FoodCount = food;
        }

        public void AddFood()
        {
            FoodCount++;
        }

        public bool TryEat()
        {
            if (FoodCount <= 0) return false;
            FoodCount--;
            Hunger = Mathf.Min(maxHunger, Hunger + foodHungerRestore);
            Energy = Mathf.Min(maxEnergy, Energy + foodEnergyRestore);
            ShowMessage("Ate food. Feeling better!");
            return true;
        }

        public void ShowMessage(string text)
        {
            _message = text;
            _messageTimer = 2.5f;
        }

        private void Update()
        {
            if (_gameManager == null) return;

            float delta = Time.deltaTime;

            Hunger -= hungerPerGameHour * (delta / _gameManager.Clock.SecondsPerGameHour);
            if (Hunger < 0f) Hunger = 0f;

            if (_gameManager.Jobs.IsWorking)
            {
                Energy -= workDrainPerSecond * delta;
                if (Energy <= 0f)
                {
                    Energy = 0f;
                    OnExhausted?.Invoke();
                }
            }
            else if (!IsStarving)
            {
                Energy = Mathf.Min(maxEnergy, Energy + regenPerSecond * delta);
            }

            if (_messageTimer > 0f) _messageTimer -= delta;

            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame && FoodCount > 0)
            {
                TryEat();
            }
        }
    }
}

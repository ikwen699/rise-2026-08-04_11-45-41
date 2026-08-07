using UnityEngine;
using UnityEngine.InputSystem;

namespace Rise.Systems
{
    public class GasStation : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 4f;
        [SerializeField] private int fullTankCost = 300;

        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _playerInRange;

        public bool IsPlayerInRange => _playerInRange;
        public int FullTankCost => fullTankCost;

        public void Configure(Transform player, Core.GameManager gameManager)
        {
            _player = player;
            _gameManager = gameManager;
        }

        private void Update()
        {
            if (_player == null || _gameManager == null) return;

            _playerInRange = Vector3.Distance(transform.position, _player.position) <= interactRadius;

            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (ePressed && _playerInRange)
            {
                TryRefuel();
            }
        }

        private void TryRefuel()
        {
            CarController car = FindNearestCar();
            if (car == null)
            {
                _gameManager.Phone?.Push("Gas Station", "No car nearby to refuel.");
                return;
            }

            if (car.IsDriving)
            {
                _gameManager.Phone?.Push("Gas Station", "Exit the car first to refuel.");
                return;
            }

            if (car.FuelPercent >= 0.99f)
            {
                _gameManager.Phone?.Push("Gas Station", "Tank is already full.");
                return;
            }

            float missing = 1f - car.FuelPercent;
            int cost = Mathf.RoundToInt(missing * fullTankCost);
            if (cost < 1) cost = 1;

            if (_gameManager.Rep != null)
            {
                float discount = _gameManager.Rep.GetShopDiscount();
                cost = Mathf.RoundToInt(cost * discount);
            }

            if (!_gameManager.Wallet.CanAfford(cost))
            {
                _gameManager.Phone?.Push("Gas Station", "Need $" + cost + " to refuel. You have $" + _gameManager.Wallet.Money + ".");
                return;
            }

            _gameManager.Wallet.Spend(cost);
            car.Refuel(car.FuelCapacity - car.Fuel);
            _gameManager.Phone?.Push("Gas Station", "Refueled " + car.brandName + " for $" + cost + ".");
        }

        private CarController FindNearestCar()
        {
            CarController[] cars = FindObjectsByType<CarController>();
            CarController nearest = null;
            float minDist = 10f;

            foreach (CarController car in cars)
            {
                if (car == null || car.IsDriving) continue;
                float dist = Vector3.Distance(transform.position, car.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = car;
                }
            }

            return nearest;
        }

        public int GetRefuelCost(CarController car)
        {
            if (car == null) return 0;
            float missing = 1f - car.FuelPercent;
            int cost = Mathf.RoundToInt(missing * fullTankCost);
            if (cost < 1) cost = 1;
            if (_gameManager.Rep != null)
            {
                float discount = _gameManager.Rep.GetShopDiscount();
                cost = Mathf.RoundToInt(cost * discount);
            }
            return cost;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}

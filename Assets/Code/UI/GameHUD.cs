using UnityEngine;
using UnityEngine.UI;
using Rise.Core;
using Rise.Systems;

namespace Rise.UI
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private Text moneyText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text dayText;
        [SerializeField] private Text workText;
        [SerializeField] private Text needsText;
        [SerializeField] private Text shopText;
        [SerializeField] private GameManager gameManager;

        private void Start()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.OnMoneyChanged += HandleMoney;
            gameManager.OnDayChanged += HandleDay;
            gameManager.OnTimeAdvanced += HandleTime;

            HandleMoney(gameManager.Wallet.Money);
            HandleDay(gameManager.Clock.Day);
            HandleTime();
        }

        private void OnDestroy()
        {
            if (gameManager == null) return;
            gameManager.OnMoneyChanged -= HandleMoney;
            gameManager.OnDayChanged -= HandleDay;
            gameManager.OnTimeAdvanced -= HandleTime;
        }

        public void Configure(GameManager manager, Text money, Text time, Text day, Text work, Text needs, Text shop)
        {
            gameManager = manager;
            moneyText = money;
            timeText = time;
            dayText = day;
            workText = work;
            needsText = needs;
            shopText = shop;
        }

        private void Update()
        {
            if (gameManager == null) return;

            PlayerNeeds needs = gameManager.Needs;
            if (needs != null)
            {
                string line = needs.NeedsText;
                if (gameManager.Partner != null) line += "   " + gameManager.Partner.StatusText;
                line += "   Earned $" + gameManager.Jobs.TotalEarned;
                SetText(needsText, line);
            }

            ShopStand shop = gameManager.ActiveShop;
            Partner partner = gameManager.Partner;
            if (shop != null && shop.IsOpen)
            {
                SetText(shopText, shop.GetMenuText());
                if (workText != null) workText.text = "";
                return;
            }
            if (partner != null && partner.IsOpen)
            {
                SetText(shopText, partner.GetMenuText());
                if (workText != null) workText.text = "";
                return;
            }
            if (shopText != null && shopText.text.Length > 0) shopText.text = "";

            string hint;
            if (gameManager.Jobs.IsWorking && gameManager.Jobs.CurrentJob != null)
            {
                hint = "Working as " + gameManager.Jobs.CurrentJob.JobName + " ... (press E to stop)";
            }
            else if (needs != null && needs.FoodCount > 0 &&
                     (needs.Hunger < needs.MaxHunger || needs.Energy < needs.MaxEnergy))
            {
                hint = "Press Q to eat (Food x" + needs.FoodCount + ")";
            }
            else if (needs != null && needs.HasMessage)
            {
                hint = needs.Message;
            }
            else if (partner != null && partner.IsPlayerInRange)
            {
                hint = "Press E to talk to Maya";
            }
            else if (TryGetLockedStation(out WorkStation locked))
            {
                hint = "Locked: earn $" + locked.Job.UnlockEarned + " total to work as " + locked.Job.JobName;
            }
            else if (IsNearAnyShop())
            {
                hint = "Press E to shop";
            }
            else
            {
                hint = "Press E at a yellow work spot to work";
            }
            SetText(workText, hint);
        }

        private bool IsNearAnyShop()
        {
            for (int i = 0; i < gameManager.ShopCount; i++)
            {
                if (gameManager.GetShop(i).IsPlayerInRange) return true;
            }
            return false;
        }

        private bool TryGetLockedStation(out WorkStation locked)
        {
            locked = null;
            for (int i = 0; i < gameManager.StationCount; i++)
            {
                WorkStation station = gameManager.GetStation(i);
                if (station.IsPlayerInRange && !station.IsUnlocked)
                {
                    locked = station;
                    return true;
                }
            }
            return false;
        }

        private void HandleMoney(int money) => SetText(moneyText, "$" + money);
        private void HandleDay(int day) => SetText(dayText, "Day " + day);
        private void HandleTime() => SetText(timeText, gameManager != null ? gameManager.Clock.ClockText : "");

        private void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}

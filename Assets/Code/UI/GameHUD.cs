using System;
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
        [SerializeField] private Text shopText;
        [SerializeField] private GameManager gameManager;

        private void Start()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (gameManager == null) return;

            gameManager.OnMoneyChanged += HandleMoney;
            gameManager.OnDayChanged += HandleDay;
            gameManager.OnTimeAdvanced += HandleTime;
            gameManager.OnWorkingChanged += HandleWorking;

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (gameManager == null) return;
            gameManager.OnMoneyChanged -= HandleMoney;
            gameManager.OnDayChanged -= HandleDay;
            gameManager.OnTimeAdvanced -= HandleTime;
            gameManager.OnWorkingChanged -= HandleWorking;
        }

        public void Configure(GameManager manager, Text money, Text time, Text day, Text work, Text shop)
        {
            gameManager = manager;
            moneyText = money;
            timeText = time;
            dayText = day;
            workText = work;
            shopText = shop;
        }

        private void Update()
        {
            if (gameManager == null) return;

            ShopStand shop = gameManager.ActiveShop;
            if (shop != null && shop.IsOpen)
            {
                SetText(shopText, shop.GetMenuText());
            }
            else
            {
                if (shopText != null && shopText.text.Length > 0) shopText.text = "";

                if (!gameManager.Jobs.IsWorking && IsNearAnyShop())
                {
                    SetText(workText, "Press E to shop");
                }
            }
        }

        private bool IsNearAnyShop()
        {
            for (int i = 0; i < gameManager.ShopCount; i++)
            {
                if (gameManager.GetShop(i).IsPlayerInRange) return true;
            }
            return false;
        }

        private void RefreshAll()
        {
            HandleMoney(gameManager.Wallet.Money);
            HandleDay(gameManager.Clock.Day);
            HandleTime();
            HandleWorking(gameManager.Jobs.IsWorking ? gameManager.Jobs.CurrentJob : null, gameManager.Jobs.IsWorking);
        }

        private void HandleMoney(int money) => SetText(moneyText, "$" + money);
        private void HandleDay(int day) => SetText(dayText, "Day " + day);
        private void HandleTime() => SetText(timeText, gameManager != null ? gameManager.Clock.ClockText : "");

        private void HandleWorking(JobDefinition job, bool working)
        {
            if (working && job != null)
            {
                SetText(workText, "Working as " + job.JobName + " ... (press E to stop)");
            }
            else
            {
                SetText(workText, "Press E at a yellow work spot to work");
            }
        }

        private void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}
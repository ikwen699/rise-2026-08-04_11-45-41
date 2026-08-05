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
        [SerializeField] private Text phoneText;
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

        public void Configure(GameManager manager, Text money, Text time, Text day, Text work, Text needs, Text shop, Text phone)
        {
            gameManager = manager;
            moneyText = money;
            timeText = time;
            dayText = day;
            workText = work;
            needsText = needs;
            shopText = shop;
            phoneText = phone;
        }

        private void Update()
        {
            if (gameManager == null) return;

            PlayerNeeds needs = gameManager.Needs;
            if (needs != null)
            {
                string line = needs.NeedsText;
                if (gameManager.Partner != null) line += "   " + gameManager.Partner.StatusText;
                if (gameManager.Rep != null) line += "   Rep " + gameManager.Rep.Reputation + " " + gameManager.Rep.GetRepTierText();
                line += "   Earned $" + gameManager.Jobs.TotalEarned;
                if (gameManager.Properties != null && gameManager.Properties.GetTotalIncome() > 0)
                    line += "   Income $" + gameManager.Properties.GetTotalIncome() + "/hr";

                if (gameManager.Quests != null && !gameManager.Quests.AllComplete)
                {
                    QuestDefinition q = gameManager.Quests.GetCurrentQuest();
                    if (q != null)
                        line += "\n[Quest] " + q.QuestName + ": " + q.GetProgressText(gameManager);
                }

                if (gameManager.Rival != null && !gameManager.Rival.IsDefeated)
                {
                    int diff = gameManager.Wallet.Money - Mathf.RoundToInt(gameManager.Rival.RivalMoney);
                    string rivalStatus = diff >= 0 ? "Ahead by $" + diff : "Behind by $" + Mathf.Abs(diff);
                    line += "\n[Rival] " + gameManager.Rival.RivalName + " — " + rivalStatus;
                }

                SetText(needsText, line);
            }

            if (gameManager.Phone != null)
            {
                string phoneContent = gameManager.Phone.GetNotificationText();
                SetText(phoneText, phoneContent);
            }

            ShopStand shop = gameManager.ActiveShop;
            ClothingStand clothing = gameManager.ActiveClothingShop;
            Partner partner = gameManager.Partner;
            TownNPC townNPC = gameManager.ActiveTownNPC;
            CarController activeCar = gameManager.ActiveCar;

            if (activeCar != null && activeCar.IsDriving)
            {
                SetText(shopText, "Driving " + activeCar.brandName + "\nWASD: steer  Space: brake  E: exit");
                if (workText != null) workText.text = "";
                return;
            }
            if (shop != null && shop.IsOpen)
            {
                SetText(shopText, shop.GetMenuText());
                if (workText != null) workText.text = "";
                return;
            }
            if (clothing != null && clothing.IsOpen)
            {
                SetText(shopText, clothing.GetMenuText());
                if (workText != null) workText.text = "";
                return;
            }
            if (partner != null && partner.IsOpen)
            {
                SetText(shopText, partner.GetMenuText());
                if (workText != null) workText.text = "";
                return;
            }
            if (townNPC != null && townNPC.IsOpen)
            {
                SetText(shopText, townNPC.GetDialogueText());
                if (workText != null) workText.text = "";
                return;
            }

            if (gameManager.Rival != null && !gameManager.Rival.IsDefeated && gameManager.Rival.IsPlayerInRange && !gameManager.Rival.IsOpen)
            {
                SetText(shopText, gameManager.Rival.GetDialogueText());
                if (workText != null) workText.text = "";
                return;
            }
            if (gameManager.Rival != null && gameManager.Rival.IsOpen)
            {
                SetText(shopText, gameManager.Rival.GetDialogueText());
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
            else if (TryNearCar(out CarController nearCar))
            {
                if (nearCar.IsLocked(gameManager))
                    hint = nearCar.brandName + " — Requires Rep " + nearCar.minRep;
                else
                    hint = "Press E to drive " + nearCar.brandName;
            }
            else if (TryGetTalkableNPC(out TownNPC talkNPC))
            {
                hint = "Press E to talk to " + talkNPC.npcName;
            }
            else if (gameManager.Rival != null && !gameManager.Rival.IsDefeated && gameManager.Rival.IsPlayerInRange)
            {
                hint = "Press E to talk to " + gameManager.Rival.RivalName;
            }
            else if (TryGetLockedStation(out WorkStation locked))
            {
                hint = "Locked: earn $" + locked.Job.UnlockEarned + " total to work as " + locked.Job.JobName;
            }
            else if (IsNearAnyShop())
            {
                hint = "Press E to shop";
            }
            else if (IsNearClothingShop())
            {
                hint = "Press E to browse designer clothes";
            }
            else if (TryNearDoor(out DoorInteractable nearDoor))
            {
                PropertyData prop = gameManager.Properties != null ? gameManager.Properties.GetProperty(nearDoor.BuildingName) : null;
                if (prop != null && !prop.owned)
                    hint = "Press E to enter " + nearDoor.BuildingName + "  |  Press F to buy for $" + prop.cost;
                else
                    hint = "Press E to enter " + nearDoor.BuildingName;
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

        private bool IsNearClothingShop()
        {
            for (int i = 0; i < gameManager.ClothingShopCount; i++)
            {
                if (gameManager.GetClothingShop(i).IsPlayerInRange) return true;
            }
            return false;
        }

        private bool TryGetTalkableNPC(out TownNPC talkable)
        {
            talkable = null;
            for (int i = 0; i < gameManager.TownNPCCount; i++)
            {
                TownNPC npc = gameManager.GetTownNPC(i);
                if (npc != null && npc.IsPlayerInRange && !npc.IsOpen)
                {
                    talkable = npc;
                    return true;
                }
            }
            return false;
        }

        private bool TryNearCar(out CarController nearCar)
        {
            nearCar = null;
            for (int i = 0; i < gameManager.CarCount; i++)
            {
                CarController car = gameManager.GetCar(i);
                if (car != null && car.IsPlayerInRange && !car.IsDriving)
                {
                    nearCar = car;
                    return true;
                }
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

        private bool TryNearDoor(out DoorInteractable nearDoor)
        {
            nearDoor = null;
            DoorInteractable[] doors = Object.FindObjectsByType<DoorInteractable>();
            foreach (DoorInteractable door in doors)
            {
                if (door.isInteriorExit || door.IsInside) continue;
                if (door.IsPlayerInRange)
                {
                    nearDoor = door;
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

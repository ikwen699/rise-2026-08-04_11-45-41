using System;
using System.Collections.Generic;
using UnityEngine;
using Rise.SaveSystem;
using Rise.Systems;

namespace Rise.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Economy")]
        [SerializeField] private int startingMoney = 0;

        [Header("Time")]
        [SerializeField] private float secondsPerGameHour = 10f;
        [SerializeField] private int startingDay = 1;
        [SerializeField] private float startingHour = 8f;

        [Header("Environment")]
        [SerializeField] private float nightIntensity = 0.05f;
        [SerializeField] private float dayIntensity = 1.35f;

        public event Action<int> OnMoneyChanged;
        public event Action<int> OnDayChanged;
        public event Action OnTimeAdvanced;
        public event Action<JobDefinition, bool> OnWorkingChanged;

        public Wallet Wallet { get; private set; }
        public TimeSystem Clock { get; private set; }
        public JobSystem Jobs { get; private set; }
        public PlayerNeeds Needs { get; private set; }
        public ReputationSystem Rep { get; private set; }

        public void EnsureNeeds()
        {
            if (Needs != null) return;
            if (_player == null) return;
            Needs = _player.GetComponent<PlayerNeeds>();
            if (Needs == null) Needs = _player.gameObject.AddComponent<PlayerNeeds>();
            if (Needs != null) Needs.Configure(this);
        }
        public ShopStand ActiveShop { get; set; }
        public ClothingStand ActiveClothingShop { get; set; }
        public Partner Partner { get; private set; }
        public TownNPC ActiveTownNPC { get; set; }

        private readonly List<ShopStand> _shops = new List<ShopStand>();
        private readonly List<WorkStation> _stations = new List<WorkStation>();
        private readonly List<TownNPC> _townNPCs = new List<TownNPC>();
        private readonly List<ClothingStand> _clothingShops = new List<ClothingStand>();
        private float _jobEarnAccumulator;
        private Transform _player;
        private Light _sun;

        public int ShopCount => _shops.Count;
        public ShopStand GetShop(int index) => _shops[index];
        public int ClothingShopCount => _clothingShops.Count;
        public ClothingStand GetClothingShop(int index) => _clothingShops[index];
        public int StationCount => _stations.Count;
        public WorkStation GetStation(int index) => _stations[index];
        public int TownNPCCount => _townNPCs.Count;
        public TownNPC GetTownNPC(int index) => _townNPCs[index];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Wallet = gameObject.AddComponent<Wallet>();
            Clock = gameObject.AddComponent<TimeSystem>();
            Jobs = gameObject.AddComponent<JobSystem>();
            Rep = gameObject.AddComponent<ReputationSystem>();
            Rep.Configure(this);

            Wallet.OnMoneyChanged += value => OnMoneyChanged?.Invoke(value);
            Clock.OnDayChanged += day => OnDayChanged?.Invoke(day);
            Clock.OnTimeAdvanced += () => OnTimeAdvanced?.Invoke();
            Clock.OnDayChanged += _ => SaveNow();
            Jobs.OnWorkingChanged += (job, working) => OnWorkingChanged?.Invoke(job, working);

            Wallet.SetMoney(startingMoney);
            Clock.Configure(secondsPerGameHour, startingDay, startingHour);

            SaveData loaded = null;
            if (GameSave.TryLoad(out SaveData data))
            {
                loaded = data;
                Wallet.SetMoney(data.money);
                Clock.Configure(secondsPerGameHour, data.day, data.hourOfDay);
            }

            FindPlayer();
            FindSun();
            SetupWorkStations();
            SetupShops();
            SetupClothingShops();
            SetupPartner();
            SetupTownspeople();

            if (Needs != null && loaded != null)
            {
                Needs.ApplySaved(loaded.energy, loaded.hunger, loaded.food);
                Needs.ApplyGifts(loaded.giftFlowers, loaded.giftChocolate, loaded.giftRings);
            }
            if (Partner != null && loaded != null)
            {
                Partner.ApplySaved(loaded.affection, loaded.married, loaded.marriageDay, loaded.childSpawned);
            }
            if (Jobs != null && loaded != null)
            {
                Jobs.ApplyTotalEarned(loaded.totalEarned);
            }
            if (Rep != null && loaded != null)
            {
                Rep.ApplySaved(loaded.reputation);
            }
            if (_player != null)
            {
                PlayerAppearance appearance = _player.GetComponent<PlayerAppearance>();
                if (appearance != null)
                {
                    if (loaded != null) appearance.ApplySaved(loaded.outfitIndex);
                    else appearance.Init();
                }
            }
        }

        private void Update()
        {
            Clock.Tick(Time.deltaTime);
            UpdateWork(Time.deltaTime);
            UpdateSunlight();
        }

        private void FindPlayer()
        {
            GameObject playerGO = GameObject.Find("Player");
            _player = playerGO != null ? playerGO.transform : null;

            Needs = _player != null ? _player.GetComponent<PlayerNeeds>() : null;
            if (Needs == null && _player != null)
            {
                Needs = _player.gameObject.AddComponent<PlayerNeeds>();
            }
            if (Needs != null)
            {
                Needs.Configure(this);
                Needs.OnExhausted += HandleExhausted;
            }
            Debug.Log("Rise: FindPlayer - Player=" + (playerGO != null ? playerGO.name : "null") + " Needs=" + (Needs != null ? "OK" : "NULL"));
        }

        private void FindSun()
        {
            Light[] lights = FindObjectsByType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    _sun = light;
                    break;
                }
            }
        }

        private void SetupWorkStations()
        {
            WorkStation[] stations = FindObjectsByType<WorkStation>(FindObjectsInactive.Include);
            _stations.Clear();
            foreach (WorkStation station in stations)
            {
                station.Configure(_player, this);
                _stations.Add(station);
            }
        }

        private void SetupShops()
        {
            ShopStand[] stands = FindObjectsByType<ShopStand>(FindObjectsInactive.Include);
            _shops.Clear();
            foreach (ShopStand stand in stands)
            {
                stand.Configure(_player, this);
                _shops.Add(stand);
            }
        }

        private void SetupClothingShops()
        {
            ClothingStand[] stands = FindObjectsByType<ClothingStand>(FindObjectsInactive.Include);
            _clothingShops.Clear();
            foreach (ClothingStand stand in stands)
            {
                stand.Configure(_player, this);
                _clothingShops.Add(stand);
            }
        }

        private void SetupPartner()
        {
            Partner partner = FindAnyObjectByType<Partner>();
            if (partner != null)
            {
                partner.Configure(_player, this);
                Partner = partner;
            }
        }

        private void SetupTownspeople()
        {
            TownNPC[] citizens = FindObjectsByType<TownNPC>(FindObjectsInactive.Include);
            _townNPCs.Clear();
            foreach (TownNPC citizen in citizens)
            {
                citizen.Configure(_player, this);
                _townNPCs.Add(citizen);
            }
        }

        private void UpdateWork(float deltaTime)
        {
            if (!Jobs.IsWorking || Jobs.CurrentJob == null) return;

            float gameHours = deltaTime / Clock.SecondsPerGameHour;
            _jobEarnAccumulator += gameHours * Jobs.CurrentJob.HourlyPay;
            int earned = Mathf.FloorToInt(_jobEarnAccumulator);
            if (earned > 0)
            {
                _jobEarnAccumulator -= earned;
                Wallet.Add(earned);
                Jobs.AddEarned(earned);
                Rep?.AddReputation(1);
            }
        }

        private void UpdateSunlight()
        {
            if (_sun == null) return;

            float cycle = (Clock.HourOfDay - 6f) / 12f;
            float factor = Mathf.Clamp01(1f - Mathf.Abs(cycle - 0.5f) * 2f);
            _sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, factor);
        }

        public void ToggleWork(JobDefinition job, WorkStation station)
        {
            if (Jobs.IsWorking)
            {
                Jobs.StopWorking();
                return;
            }

            if (!Jobs.IsUnlocked(job))
            {
                if (Needs != null)
                {
                    Needs.ShowMessage("Locked. Earn $" + job.UnlockEarned + " total to work as " + job.JobName + ".");
                }
                return;
            }

            if (Needs != null && Needs.Energy <= 0f)
            {
                Needs.ShowMessage("Too tired to work. Eat food to recover.");
                return;
            }

            Jobs.StartWorking(job, station);
        }

        private void HandleExhausted()
        {
            if (Jobs.IsWorking)
            {
                Jobs.StopWorking();
                Needs.ShowMessage("Too tired to work. Eat food to recover energy.");
            }
        }

        public void SaveNow()
        {
            int outfitIdx = 0;
            if (_player != null)
            {
                PlayerAppearance app = _player.GetComponent<PlayerAppearance>();
                if (app != null) outfitIdx = app.CurrentOutfitIndex;
            }
            GameSave.Save(new SaveData
            {
                money = Wallet.Money,
                day = Clock.Day,
                hourOfDay = Clock.HourOfDay,
                energy = Needs != null ? Needs.Energy : 100f,
                hunger = Needs != null ? Needs.Hunger : 100f,
                food = Needs != null ? Needs.FoodCount : 0,
                giftFlowers = Needs != null ? Needs.GiftFlowers : 0,
                giftChocolate = Needs != null ? Needs.GiftChocolate : 0,
                giftRings = Needs != null ? Needs.GiftRings : 0,
                affection = Partner != null ? Partner.Affection : 0f,
                married = Partner != null && Partner.Married,
                marriageDay = Partner != null ? Partner.MarriageDay : 0,
                childSpawned = Partner != null && Partner.ChildSpawned,
                totalEarned = Jobs.TotalEarned,
                outfitIndex = outfitIdx,
                reputation = Rep != null ? Rep.Reputation : 0
            });
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
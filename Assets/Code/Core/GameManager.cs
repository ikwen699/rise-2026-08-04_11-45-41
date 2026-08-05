using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Rise.SaveSystem;
using Rise.Systems;
using Rise.UI;

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
        public QuestSystem Quests { get; private set; }
        public Rival Rival { get; private set; }
        public PhoneNotifier Phone { get; private set; }

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
        private readonly List<CarController> _cars = new List<CarController>();
        private readonly List<Light> _lampLights = new List<Light>();
        private readonly List<float> _lampBaseIntensities = new List<float>();
        private float _jobEarnAccumulator;
        private Transform _player;
        private Light _sun;
        private Light _moon;
        private CinemachineCamera _cmCamera;
        private Transform _cmFollow;
        private Transform _cmLookAt;

        public int ShopCount => _shops.Count;
        public ShopStand GetShop(int index) => _shops[index];
        public int ClothingShopCount => _clothingShops.Count;
        public ClothingStand GetClothingShop(int index) => _clothingShops[index];
        public int StationCount => _stations.Count;
        public WorkStation GetStation(int index) => _stations[index];
        public int TownNPCCount => _townNPCs.Count;
        public TownNPC GetTownNPC(int index) => _townNPCs[index];
        public int CarCount => _cars.Count;
        public CarController GetCar(int index) => _cars[index];
        public CarController ActiveCar { get; private set; }

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
            Quests = gameObject.AddComponent<QuestSystem>();
            Rival = gameObject.AddComponent<Rival>();
            Rival.Configure(this);
            Phone = gameObject.AddComponent<PhoneNotifier>();

            Wallet.OnMoneyChanged += value => OnMoneyChanged?.Invoke(value);
            Clock.OnDayChanged += day => OnDayChanged?.Invoke(day);
            Clock.OnTimeAdvanced += () => OnTimeAdvanced?.Invoke();
            Clock.OnDayChanged += _ => SaveNow();
            Jobs.OnWorkingChanged += (job, working) => OnWorkingChanged?.Invoke(job, working);

            Wallet.OnMoneyChanged += val => Phone?.Push("Money", "You now have $" + val);
            Clock.OnDayChanged += day => Phone?.Push("Day " + day, "A new day begins.");
            Rep.OnReputationChanged += rep => Phone?.Push("Reputation", "Rep is now " + rep + " — " + Rep.GetRepTierText());

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
            FindLamps();
            SetupWorkStations();
            SetupShops();
            SetupClothingShops();
            SetupPartner();
            SetupTownspeople();
            SetupCars();
            SetupRival();
            Quests.Configure(this);
            Phone.Configure(this);

            CinemachineCamera[] cms = FindObjectsByType<CinemachineCamera>();
            if (cms.Length > 0) _cmCamera = cms[0];

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
            if (Quests != null && loaded != null)
            {
                Quests.ApplySaved(loaded.questIndex, loaded.questProgress);
            }
            if (Rival != null && loaded != null)
            {
                Rival.ApplySaved(loaded.rivalMoney, loaded.rivalRep, loaded.rivalDefeated);
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
                    if (light.gameObject.name.Contains("Moon"))
                        _moon = light;
                    else
                        _sun = light;
                }
            }
        }

        private void FindLamps()
        {
            _lampLights.Clear();
            _lampBaseIntensities.Clear();
            Light[] allLights = FindObjectsByType<Light>();
            foreach (Light light in allLights)
            {
                if (light.type == LightType.Point && light.gameObject.name == "Lamp_Light")
                {
                    _lampLights.Add(light);
                    _lampBaseIntensities.Add(light.intensity);
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

        private void SetupCars()
        {
            CarController[] cars = FindObjectsByType<CarController>(FindObjectsInactive.Include);
            _cars.Clear();
            foreach (CarController car in cars)
            {
                car.Configure(_player, this);
                _cars.Add(car);
            }
        }

        private void SetupRival()
        {
            Rival rival = FindAnyObjectByType<Rival>();
            if (rival != null)
            {
                rival.Configure(this);
                Rival = rival;
            }
        }

        public void EnterCar(CarController car)
        {
            if (ActiveCar != null && ActiveCar != car && ActiveCar.IsDriving)
            {
                ActiveCar.ForceStopDriving();
            }
            ActiveCar = car;
            if (_player != null)
            {
                PlayerController pc = _player.GetComponent<PlayerController>();
                if (pc != null) pc.enabled = false;
                foreach (Renderer r in _player.GetComponentsInChildren<Renderer>())
                    r.enabled = false;
            }
            if (_cmCamera != null)
            {
                _cmFollow = _cmCamera.Follow;
                _cmLookAt = _cmCamera.LookAt;
                _cmCamera.Follow = car.transform;
                _cmCamera.LookAt = car.transform;
            }
        }

        public void ExitCar()
        {
            CarController prev = ActiveCar;
            ActiveCar = null;
            if (_player != null)
            {
                PlayerController pc = _player.GetComponent<PlayerController>();
                if (pc != null) pc.enabled = true;
                foreach (Renderer r in _player.GetComponentsInChildren<Renderer>())
                    r.enabled = true;
                if (prev != null)
                    _player.position = prev.transform.position + prev.transform.right * 2.5f;
            }
            if (_cmCamera != null)
            {
                _cmCamera.Follow = _cmFollow;
                _cmCamera.LookAt = _cmLookAt;
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

            float hour = Clock.HourOfDay;
            float cycle = (hour - 6f) / 12f;
            float dayFactor = Mathf.Clamp01(1f - Mathf.Abs(cycle - 0.5f) * 2f);

            float sunElevation = Mathf.Lerp(10f, 70f, dayFactor);
            _sun.transform.rotation = Quaternion.Euler(sunElevation, -30f, 0f);

            Color nightSun = new Color(0.2f, 0.25f, 0.35f);
            Color dawnSun = new Color(1f, 0.6f, 0.3f);
            Color daySun = new Color(1f, 0.95f, 0.85f);
            _sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, dayFactor);
            if (dayFactor > 0.1f && dayFactor < 0.5f)
                _sun.color = Color.Lerp(dawnSun, daySun, (dayFactor - 0.1f) / 0.4f);
            else if (dayFactor <= 0.1f)
                _sun.color = Color.Lerp(nightSun, dawnSun, dayFactor / 0.1f);
            else
                _sun.color = daySun;

            Material sky = RenderSettings.skybox;
            if (sky != null)
            {
                sky.SetColor("_SkyTint", Color.Lerp(new Color(0.1f, 0.15f, 0.3f), new Color(0.48f, 0.6f, 0.85f), dayFactor));
                sky.SetFloat("_Exposure", Mathf.Lerp(0.3f, 1.15f, dayFactor));
                sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(1.2f, 1.05f, dayFactor));
            }

            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.05f, 0.06f, 0.1f), new Color(0.72f, 0.78f, 0.9f), dayFactor);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.03f, 0.03f, 0.05f), new Color(0.62f, 0.63f, 0.64f), dayFactor);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.02f, 0.02f, 0.03f), new Color(0.5f, 0.48f, 0.44f), dayFactor);
            RenderSettings.fogColor = Color.Lerp(new Color(0.05f, 0.06f, 0.1f), new Color(0.82f, 0.86f, 0.92f), dayFactor);

            float nightFactor = 1f - dayFactor;
            for (int i = 0; i < _lampLights.Count; i++)
            {
                Light lamp = _lampLights[i];
                if (lamp == null) continue;
                float targetIntensity = nightFactor > 0.3f ? _lampBaseIntensities[i] : 0f;
                lamp.intensity = Mathf.Lerp(lamp.intensity, targetIntensity, Time.deltaTime * 3f);
                lamp.enabled = lamp.intensity > 0.01f;
            }

            if (_moon != null)
            {
                _moon.intensity = Mathf.Lerp(0.4f, 0f, dayFactor);
                _moon.enabled = nightFactor > 0.2f;
            }
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
                reputation = Rep != null ? Rep.Reputation : 0,
                questIndex = Quests != null ? Quests.CurrentIndex : 0,
                questProgress = Quests != null ? Quests.CurrentProgress : 0,
                rivalMoney = Rival != null ? Rival.RivalMoney : 0f,
                rivalRep = Rival != null ? Rival.RivalRep : 0f,
                rivalDefeated = Rival != null && Rival.IsDefeated
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
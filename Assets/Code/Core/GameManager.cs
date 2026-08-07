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
        public AudioManager Audio { get; private set; }
        public PropertyManager Properties { get; private set; }
        public SkillSystem Skills { get; private set; }
        public WeatherSystem Weather { get; private set; }
        public BulletinBoard Bulletin { get; private set; }
        public GasStation Gas { get; private set; }
        public PauseMenu PauseUI { get; private set; }
        public bool IsPaused => PauseUI != null && PauseUI.IsPaused;

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
        public Transform Player => _player;

        private readonly List<ShopStand> _shops = new List<ShopStand>();
        private readonly List<WorkStation> _stations = new List<WorkStation>();
        private readonly List<TownNPC> _townNPCs = new List<TownNPC>();
        private readonly List<ClothingStand> _clothingShops = new List<ClothingStand>();
        private readonly List<CarController> _cars = new List<CarController>();
        private readonly List<Light> _lampLights = new List<Light>();
        private readonly List<float> _lampBaseIntensities = new List<float>();
        private readonly List<Renderer> _windowRenderers = new List<Renderer>();
        private Material _windowLitMat;
        private Material _windowDarkMat;
        private float _jobEarnAccumulator;
        private Transform _player;
        private Light _sun;
        private Light _moon;
        private CinemachineCamera _cmCamera;
        private Transform _cmFollow;
        private Transform _cmLookAt;
        private WorkMinigame _activeMinigame;

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
            Audio = gameObject.AddComponent<AudioManager>();
            Properties = gameObject.AddComponent<PropertyManager>();
            Skills = gameObject.AddComponent<SkillSystem>();
            Skills.Init();
            Weather = gameObject.AddComponent<WeatherSystem>();
            Weather.Init();
            Weather.OnWeatherChanged += w => Phone?.Push("Weather", "The weather is now " + w);

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
            FindWindows();
            SetupWorkStations();
            SetupShops();
            SetupClothingShops();
            SetupPartner();
            SetupTownspeople();
            SetupCars();
            SetupDoors();
            SetupBulletin();
            SetupGasStation();
            SetupPauseMenu();
            SetupRival();
            Quests.Configure(this);
            Properties.Configure(this);
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
            if (Properties != null && loaded != null)
            {
                Properties.ApplySaved(loaded.ownedProperties);
            }
            if (Skills != null && loaded != null && loaded.skillXP != null)
            {
                Skills.ApplySaved(loaded.skillXP);
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
            UpdateAudio();
            UpdatePropertyIncome();
            UpdateStoryEvents();
            if (Weather != null && Clock != null)
                Weather.Tick(Time.deltaTime / Clock.SecondsPerGameHour);
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

        private void FindWindows()
        {
            _windowRenderers.Clear();
            _windowLitMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _windowLitMat.color = new Color(1f, 0.85f, 0.4f, 0.9f);
            _windowLitMat.EnableKeyword("_EMISSION");
            _windowLitMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.4f) * 2f);
            _windowDarkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _windowDarkMat.color = new Color(0.15f, 0.2f, 0.3f, 0.6f);
            Renderer[] allRenderers = FindObjectsByType<Renderer>();
            foreach (Renderer r in allRenderers)
            {
                if (r.gameObject.name.Contains("Window") && r.gameObject.name.Contains("Glass"))
                {
                    _windowRenderers.Add(r);
                    r.sharedMaterial = _windowDarkMat;
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

        private void SetupDoors()
        {
            DoorInteractable[] doors = FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include);
            GameObject hudGO = GameObject.Find("GameHUD CanvaWindow");
            CanvasGroup fade = hudGO != null ? hudGO.GetComponent<CanvasGroup>() : null;
            foreach (DoorInteractable door in doors)
            {
                door.Configure(_player, this, fade);
            }
        }

        private void SetupBulletin()
        {
            BulletinBoard board = FindAnyObjectByType<BulletinBoard>();
            if (board != null)
            {
                board.Configure(_player, this);
                Bulletin = board;
            }
        }

        private void SetupGasStation()
        {
            GasStation gas = FindAnyObjectByType<GasStation>();
            if (gas != null)
            {
                gas.Configure(_player, this);
                Gas = gas;
            }
        }

        private void SetupPauseMenu()
        {
            PauseUI = gameObject.AddComponent<PauseMenu>();
            PauseUI.Configure(this);
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
            float payBonus = Skills != null ? Skills.GetJobPayBonus() : 1f;
            float minigameMult = 1f;
            if (_activeMinigame != null && _activeMinigame.ResultReady)
            {
                minigameMult = _activeMinigame.Multiplier;
                _activeMinigame.Destroy();
                _activeMinigame = null;
                StartMinigame();
            }
            _jobEarnAccumulator += gameHours * Jobs.CurrentJob.HourlyPay * payBonus * minigameMult;
            int earned = Mathf.FloorToInt(_jobEarnAccumulator);
            if (earned > 0)
            {
                _jobEarnAccumulator -= earned;
                Wallet.Add(earned);
                Jobs.AddEarned(earned);
                Rep?.AddReputation(1);
                Skills?.AddXP(SkillName.Business, earned);
                if (Jobs.CurrentJob.JobName.Contains("Bakery") || Jobs.CurrentJob.JobName.Contains("Restaurant")
                    || Jobs.CurrentJob.JobName == "Baker" || Jobs.CurrentJob.JobName == "Chef")
                    Skills?.AddXP(SkillName.Cooking, earned / 2);
            }
        }

        public void StartMinigame()
        {
            if (_activeMinigame != null) return;
            _activeMinigame = gameObject.AddComponent<WorkMinigame>();
            _activeMinigame.Create(_player);
        }

        public void StopMinigame()
        {
            if (_activeMinigame != null)
            {
                _activeMinigame.Destroy();
                _activeMinigame = null;
            }
        }

        public string GetMinigameResult()
        {
            if (_activeMinigame != null && _activeMinigame.ResultReady)
                return "x" + _activeMinigame.Multiplier;
            return "";
        }

        private void UpdateSunlight()
        {
            if (_sun == null) return;

            float hour = Clock.HourOfDay;
            float cycle = (hour - 6f) / 12f;
            float dayFactor = Mathf.Clamp01(1f - Mathf.Abs(cycle - 0.5f) * 2f);

            float weatherMod = Weather != null ? Weather.GetAmbientIntensityModifier() : 1f;

            float sunElevation = Mathf.Lerp(10f, 70f, dayFactor);
            _sun.transform.rotation = Quaternion.Euler(sunElevation, -30f, 0f);

            Color nightSun = new Color(0.15f, 0.18f, 0.3f);
            Color dawnSun = new Color(1f, 0.55f, 0.25f);
            Color duskSun = new Color(1f, 0.4f, 0.2f);
            Color daySun = new Color(1f, 0.95f, 0.85f);

            float sunrisePoint = Mathf.InverseLerp(5f, 7f, hour);
            float sunsetPoint = Mathf.InverseLerp(17f, 19f, hour);
            float dawnDusk = 1f - Mathf.Max(sunrisePoint, sunsetPoint);
            float trueDayFactor = Mathf.Clamp01(dayFactor * (1f - dawnDusk * 0.6f));

            _sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, trueDayFactor) * weatherMod;

            if (sunrisePoint > 0f && sunrisePoint < 1f)
                _sun.color = Color.Lerp(dawnSun, daySun, sunrisePoint);
            else if (sunsetPoint > 0f && sunsetPoint < 1f)
                _sun.color = Color.Lerp(daySun, duskSun, sunsetPoint);
            else if (hour >= 19f || hour < 5f)
                _sun.color = Color.Lerp(duskSun, nightSun, Mathf.InverseLerp(19f, 22f, hour));
            else
                _sun.color = daySun;

            Material sky = RenderSettings.skybox;
            if (sky != null)
            {
                Color skyDay = new Color(0.48f, 0.6f, 0.85f);
                Color skyNight = new Color(0.08f, 0.1f, 0.2f);
                Color skyDawn = new Color(0.6f, 0.4f, 0.3f);
                Color skyTint = skyNight;
                if (sunrisePoint > 0f && sunrisePoint < 1f)
                    skyTint = Color.Lerp(skyDawn, skyDay, sunrisePoint);
                else if (sunsetPoint > 0f && sunsetPoint < 1f)
                    skyTint = Color.Lerp(skyDay, skyDawn, sunsetPoint);
                else if (hour >= 19f || hour < 5f)
                    skyTint = skyNight;
                else
                    skyTint = skyDay;

                if (Weather != null && Weather.IsRaining())
                    skyTint = Color.Lerp(skyTint, new Color(0.3f, 0.32f, 0.38f), 0.4f);

                sky.SetColor("_SkyTint", skyTint);
                sky.SetFloat("_Exposure", Mathf.Lerp(0.3f, 1.15f, trueDayFactor) * weatherMod);
                sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(1.2f, 1.05f, trueDayFactor));
            }

            Color ambientDay = new Color(0.72f, 0.78f, 0.9f);
            Color ambientNight = new Color(0.05f, 0.06f, 0.1f);
            Color ambientDawn = new Color(0.5f, 0.35f, 0.25f);
            Color ambient = ambientNight;
            if (sunrisePoint > 0f && sunrisePoint < 1f)
                ambient = Color.Lerp(ambientDawn, ambientDay, sunrisePoint);
            else if (sunsetPoint > 0f && sunsetPoint < 1f)
                ambient = Color.Lerp(ambientDay, ambientDawn, sunsetPoint);
            else if (hour >= 19f || hour < 5f)
                ambient = ambientNight;
            else
                ambient = ambientDay;

            ambient *= weatherMod;
            RenderSettings.ambientSkyColor = ambient;
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.03f, 0.03f, 0.05f), new Color(0.62f, 0.63f, 0.64f), trueDayFactor) * weatherMod;
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.02f, 0.02f, 0.03f), new Color(0.5f, 0.48f, 0.44f), trueDayFactor) * weatherMod;

            Color fogDay = new Color(0.82f, 0.86f, 0.92f);
            Color fogNight = new Color(0.05f, 0.06f, 0.1f);
            RenderSettings.fogColor = Weather != null
                ? Weather.GetFogColor(trueDayFactor)
                : Color.Lerp(fogNight, fogDay, trueDayFactor);
            RenderSettings.fogDensity = Weather != null
                ? Weather.GetFogDensity(0.008f)
                : Mathf.Lerp(0.008f, 0.004f, trueDayFactor);

            float nightFactor = 1f - trueDayFactor;
            for (int i = 0; i < _lampLights.Count; i++)
            {
                Light lamp = _lampLights[i];
                if (lamp == null) continue;
                float targetIntensity = nightFactor > 0.2f ? _lampBaseIntensities[i] : 0f;
                lamp.intensity = Mathf.Lerp(lamp.intensity, targetIntensity, Time.deltaTime * 3f);
                lamp.enabled = lamp.intensity > 0.01f;
            }

            if (_moon != null)
            {
                _moon.intensity = Mathf.Lerp(0.5f, 0f, trueDayFactor);
                _moon.enabled = nightFactor > 0.2f;
            }

            bool windowGlow = nightFactor > 0.25f;
            foreach (Renderer wr in _windowRenderers)
            {
                if (wr == null) continue;
                wr.sharedMaterial = windowGlow ? _windowLitMat : _windowDarkMat;
            }
        }

        private void UpdateAudio()
        {
            if (Audio == null || Clock == null) return;
            float hour = Clock.HourOfDay;
            float cycle = (hour - 6f) / 12f;
            float dayFactor = Mathf.Clamp01(1f - Mathf.Abs(cycle - 0.5f) * 2f);
            bool raining = Weather != null && Weather.IsRaining();
            bool stormy = Weather != null && Weather.IsStormy();
            Audio.UpdateCycle(dayFactor, raining, stormy);
        }

        private float _incomeAccumulator;
        private int _lastMilestoneMoney;
        private int _lastMilestoneRep;
        private float _townEventTimer;
        private bool _eventFair;
        private bool _eventHoliday;
        private bool _eventCharity;
        private bool _eventFestival;
        private void UpdatePropertyIncome()
        {
            if (Properties == null || Clock == null) return;
            float gameHours = Time.deltaTime / Clock.SecondsPerGameHour;
            _incomeAccumulator += gameHours;
            if (_incomeAccumulator >= 1f)
            {
                float incomeBonus = Skills != null ? Skills.GetPropertyIncomeBonus() : 1f;
                Properties.CollectIncome(_incomeAccumulator * incomeBonus);
                _incomeAccumulator = 0f;
            }
        }

        private void UpdateStoryEvents()
        {
            if (Wallet == null || Phone == null) return;

            int currentMoney = Wallet.Money;
            int moneyK = currentMoney / 1000;
            int lastK = _lastMilestoneMoney / 1000;
            if (moneyK > lastK && moneyK > 0)
            {
                _lastMilestoneMoney = moneyK * 1000;
                Phone.Push("Milestone!", "You've earned $" + _lastMilestoneMoney + " total. Keep rising.");
            }

            int currentRep = Rep != null ? Rep.Reputation : 0;
            int[] repTiers = { 10, 30, 50, 80 };
            foreach (int tier in repTiers)
            {
                if (currentRep >= tier && _lastMilestoneRep < tier)
                {
                    _lastMilestoneRep = tier;
                    string[] tierNames = { "", "Known", "Respected", "Renowned", "Legendary" };
                    string tierName = tier < tierNames.Length ? tierNames[tier] : "Elite";
                    Phone.Push("Rep " + tier, "You're now " + tierName + " in town.");
                }
            }

            if (Clock != null)
            {
                int day = Clock.Day;
                if (day >= 10 && !_eventFair)
                {
                    _eventFair = true;
                    Phone.Push("Town Fair!", "The town fair is happening! Visit the park.");
                    Wallet?.Add(200);
                    Rep?.AddReputation(10);
                }
                if (day >= 20 && !_eventHoliday)
                {
                    _eventHoliday = true;
                    Phone.Push("Holiday Market!", "The holiday market is open! Great deals everywhere.");
                    Wallet?.Add(300);
                    Rep?.AddReputation(15);
                }
                if (day >= 30 && !_eventCharity)
                {
                    _eventCharity = true;
                    Phone.Push("Charity Run!", "Join the charity run for fitness and town spirit!");
                    Rep?.AddReputation(20);
                    Skills?.AddXP(SkillName.Fitness, 50);
                }
                if (day >= 40 && !_eventFestival && Quests != null && Quests.AllComplete)
                {
                    _eventFestival = true;
                    Phone.Push("Grand Festival!", "The biggest event of the year! You've earned it.");
                    Wallet?.Add(500);
                    Rep?.AddReputation(25);
                }
            }

            _townEventTimer -= Time.deltaTime;
            if (_townEventTimer <= 0f)
            {
                _townEventTimer = 360f;
                int roll = UnityEngine.Random.Range(0, 4);
                if (roll == 0) Phone.Push("Town Event", "Market day! Visit the shops.");
                else if (roll == 1) Phone.Push("Town Event", "Festival tonight! NPC goodwill is high.");
                else if (roll == 2) Phone.Push("Town Event", "A new stranger arrived in town...");
                else Phone.Push("Town Event", "Clear skies. Perfect day to work.");
            }
        }

        public void ToggleWork(JobDefinition job, WorkStation station)
        {
            if (Jobs.IsWorking)
            {
                Jobs.StopWorking();
                StopMinigame();
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
            StartMinigame();
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
                rivalDefeated = Rival != null && Rival.IsDefeated,
                ownedProperties = Properties != null ? Properties.GetOwnedNames() : new string[0],
                skillXP = Skills != null ? Skills.GetXPArray() : new int[0]
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
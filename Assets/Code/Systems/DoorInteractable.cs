using UnityEngine;
using UnityEngine.InputSystem;
using Rise.Core;

namespace Rise.Systems
{
    public class DoorInteractable : MonoBehaviour
    {
        [Header("Door")]
        public string buildingName = "House_01";
        public Vector3 interiorOffset = new Vector3(0f, -200f, 0f);
        public bool isInteriorExit;

        [Header("Interior furnishing type")]
        public InteriorType interiorType = InteriorType.House;

        private Transform _player;
        private Core.GameManager _gameManager;
        private bool _playerInRange;
        private bool _isInside;
        private float _interactRadius = 3f;
        private float _fadeTimer;
        private bool _fading;
        private CanvasGroup _fadeOverlay;

        public bool IsInside => _isInside;
        public string BuildingName => buildingName;
        public bool IsPlayerInRange => _playerInRange;

        private static Transform _interiorRoot;
        private static Transform _currentInterior;

        public enum InteriorType { House, Shop, Public, Church }

        public void Configure(Transform player, Core.GameManager gameManager, CanvasGroup fadeOverlay)
        {
            _player = player;
            _gameManager = gameManager;
            _fadeOverlay = fadeOverlay;
        }

        private void Update()
        {
            if (_player == null || _gameManager == null) return;

            _playerInRange = Vector3.Distance(transform.position, _player.position) <= _interactRadius;

            if (_fading)
            {
                _fadeTimer -= Time.deltaTime;
                if (_fadeOverlay != null)
                    _fadeOverlay.alpha = 1f - Mathf.Clamp01(_fadeTimer / 0.4f);
                if (_fadeTimer <= 0f)
                {
                    _fading = false;
                    if (_fadeOverlay != null) _fadeOverlay.alpha = 0f;
                }
                return;
            }

            bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (ePressed && _playerInRange)
            {
                if (isInteriorExit && _isInside)
                {
                    ExitBuilding();
                }
                else if (!isInteriorExit && !_isInside)
                {
                    TryEnterBuilding();
                }
            }
        }

        private void TryEnterBuilding()
        {
            PropertyData property = _gameManager.Properties != null ? _gameManager.Properties.GetProperty(buildingName) : null;

            if (property != null && !property.owned)
            {
                int cost = property.cost;
                _gameManager.Needs?.ShowMessage("Press F to buy " + buildingName + " for $" + cost + "  (E to cancel)");
                bool fPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
                if (fPressed)
                {
                    if (_gameManager.Properties.BuyProperty(buildingName))
                    {
                        _gameManager.Audio?.PlayBuy();
                        EnterBuilding();
                    }
                    else
                    {
                        _gameManager.Needs?.ShowMessage("Not enough money to buy " + buildingName + " ($" + cost + " needed).");
                    }
                }
                return;
            }

            EnterBuilding();
        }

        private void EnterBuilding()
        {
            _gameManager.Audio?.PlayDoorOpen();
            _isInside = true;

            if (_player != null)
            {
                PlayerController pc = _player.GetComponent<PlayerController>();
                if (pc != null) pc.enabled = false;
                _player.position = interiorOffset + new Vector3(0f, 0f, 0f);
            }

            BuildInteriorIfNeeded();

            if (_fadeOverlay != null)
            {
                _fading = true;
                _fadeTimer = 0.4f;
                _fadeOverlay.alpha = 1f;
            }

            _gameManager.Phone?.Push("Entered", buildingName);
        }

        public void ExitBuilding()
        {
            _gameManager.Audio?.PlayDoorOpen();
            _isInside = false;

            Vector3 exitPos = transform.position + transform.forward * 2f;
            exitPos.y = transform.position.y;

            if (_player != null)
            {
                _player.position = exitPos;
                PlayerController pc = _player.GetComponent<PlayerController>();
                if (pc != null) pc.enabled = true;
            }

            if (_fadeOverlay != null)
            {
                _fading = true;
                _fadeTimer = 0.4f;
                _fadeOverlay.alpha = 1f;
            }
        }

        private void BuildInteriorIfNeeded()
        {
            if (_interiorRoot == null)
            {
                _interiorRoot = new GameObject("Interiors").transform;
                _interiorRoot.position = Vector3.zero;
            }

            if (_currentInterior != null)
                DestroyImmediate(_currentInterior.gameObject);

            string suffix = "_" + buildingName;
            GameObject interior = new GameObject("Interior" + suffix);
            interior.transform.SetParent(_interiorRoot);
            interior.transform.position = interiorOffset;
            _currentInterior = interior.transform;

            Material floorMat = CreateInteriorMat("M_IntFloor_" + suffix, new Color(0.45f, 0.35f, 0.25f));
            Material wallMat = CreateInteriorMat("M_IntWall_" + suffix, new Color(0.88f, 0.85f, 0.8f));
            Material ceilMat = CreateInteriorMat("M_IntCeil_" + suffix, new Color(0.92f, 0.9f, 0.88f));
            Material furnMat = CreateInteriorMat("M_IntFurn_" + suffix, new Color(0.35f, 0.25f, 0.15f));
            Material fabricMat = CreateInteriorMat("M_IntFabric_" + suffix, new Color(0.6f, 0.2f, 0.2f));
            Material metalMat = CreateInteriorMat("M_IntMetal_" + suffix, new Color(0.7f, 0.7f, 0.75f));
            Material glassMat = CreateInteriorMat("M_IntGlass_" + suffix, new Color(0.6f, 0.7f, 0.85f, 0.5f));

            float roomW = 8f;
            float roomD = 8f;
            float roomH = 3.5f;

            switch (interiorType)
            {
                case InteriorType.House:
                    BuildHouseInterior(interior.transform, suffix, roomW, roomD, roomH, floorMat, wallMat, ceilMat, furnMat, fabricMat, metalMat);
                    break;
                case InteriorType.Shop:
                    BuildShopInterior(interior.transform, suffix, roomW, roomD, roomH, floorMat, wallMat, ceilMat, furnMat, metalMat, glassMat);
                    break;
                case InteriorType.Public:
                    BuildPublicInterior(interior.transform, suffix, roomW, roomD, roomH, floorMat, wallMat, ceilMat, furnMat, metalMat);
                    break;
                case InteriorType.Church:
                    BuildChurchInterior(interior.transform, suffix, 12f, 16f, 6f, floorMat, wallMat, ceilMat, furnMat, fabricMat, metalMat, glassMat);
                    break;
            }

            GameObject exitTrigger = new GameObject("ExitDoor");
            exitTrigger.transform.SetParent(interior.transform);
            exitTrigger.transform.localPosition = new Vector3(0f, 1f, -roomD * 0.5f + 0.5f);
            BoxCollider exitCol = exitTrigger.AddComponent<BoxCollider>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector3(2f, 2.5f, 1f);
            DoorInteractable exitDoor = exitTrigger.AddComponent<DoorInteractable>();
            exitDoor.buildingName = buildingName;
            exitDoor.isInteriorExit = true;
            exitDoor.interiorType = interiorType;
            exitDoor.Configure(_player, _gameManager, _fadeOverlay);
        }

        private void BuildHouseInterior(Transform parent, string suffix, float w, float d, float h,
            Material floor, Material wall, Material ceil, Material furn, Material fabric, Material metal)
        {
            MakeBox(parent, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(w, 0.1f, d), floor);
            MakeBox(parent, "Ceiling", new Vector3(0f, h, 0f), new Vector3(w, 0.1f, d), ceil);
            MakeBox(parent, "WallBack", new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.2f), wall);
            MakeBox(parent, "WallLeft", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);
            MakeBox(parent, "WallRight", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);

            MakeBox(parent, "SofaBase", new Vector3(-w * 0.25f, 0.3f, -d * 0.2f), new Vector3(3f, 0.6f, 1.2f), furn);
            MakeBox(parent, "SofaBack", new Vector3(-w * 0.25f, 0.8f, -d * 0.2f - 0.5f), new Vector3(3f, 0.6f, 0.2f), fabric);
            MakeBox(parent, "SofaArmL", new Vector3(-w * 0.25f - 1.4f, 0.55f, -d * 0.2f), new Vector3(0.25f, 0.35f, 1.2f), fabric);
            MakeBox(parent, "SofaArmR", new Vector3(-w * 0.25f + 1.4f, 0.55f, -d * 0.2f), new Vector3(0.25f, 0.35f, 1.2f), fabric);

            MakeBox(parent, "CoffeeTable", new Vector3(-w * 0.25f, 0.25f, -d * 0.2f + 1.5f), new Vector3(1.8f, 0.05f, 0.9f), furn);
            MakeBox(parent, "TableLeg1", new Vector3(-w * 0.25f - 0.7f, 0.12f, -d * 0.2f + 1.1f), new Vector3(0.08f, 0.24f, 0.08f), metal);
            MakeBox(parent, "TableLeg2", new Vector3(-w * 0.25f + 0.7f, 0.12f, -d * 0.2f + 1.1f), new Vector3(0.08f, 0.24f, 0.08f), metal);
            MakeBox(parent, "TableLeg3", new Vector3(-w * 0.25f - 0.7f, 0.12f, -d * 0.2f + 1.9f), new Vector3(0.08f, 0.24f, 0.08f), metal);
            MakeBox(parent, "TableLeg4", new Vector3(-w * 0.25f + 0.7f, 0.12f, -d * 0.2f + 1.9f), new Vector3(0.08f, 0.24f, 0.08f), metal);

            MakeBox(parent, "BedFrame", new Vector3(w * 0.25f, 0.2f, -d * 0.15f), new Vector3(2.2f, 0.4f, 2.8f), furn);
            MakeBox(parent, "Mattress", new Vector3(w * 0.25f, 0.45f, -d * 0.15f), new Vector3(2f, 0.15f, 2.6f), fabric);
            MakeBox(parent, "Pillow1", new Vector3(w * 0.25f - 0.35f, 0.58f, -d * 0.15f - 0.9f), new Vector3(0.5f, 0.1f, 0.35f), fabric);
            MakeBox(parent, "Pillow2", new Vector3(w * 0.25f + 0.35f, 0.58f, -d * 0.15f - 0.9f), new Vector3(0.5f, 0.1f, 0.35f), fabric);
            MakeBox(parent, "Headboard", new Vector3(w * 0.25f, 0.9f, -d * 0.15f - 1.3f), new Vector3(2.2f, 1f, 0.15f), furn);

            MakeBox(parent, "Counter", new Vector3(w * 0.35f, 0.5f, d * 0.35f), new Vector3(2.5f, 1f, 0.8f), furn);
            MakeBox(parent, "Stove", new Vector3(w * 0.35f + 1.5f, 0.55f, d * 0.35f), new Vector3(0.8f, 0.9f, 0.8f), metal);
            MakeBox(parent, "Fridge", new Vector3(w * 0.35f - 1.5f, 0.75f, d * 0.35f), new Vector3(0.8f, 1.5f, 0.8f), metal);

            MakeBox(parent, "Chair1", new Vector3(0f, 0.25f, d * 0.3f), new Vector3(0.5f, 0.5f, 0.5f), furn);
            MakeBox(parent, "Chair2", new Vector3(0.8f, 0.25f, d * 0.3f), new Vector3(0.5f, 0.5f, 0.5f), furn);

            MakeBox(parent, "LampPost", new Vector3(-w * 0.4f, 1.5f, d * 0.3f), new Vector3(0.1f, 1.5f, 0.1f), metal);
            MakeSphere(parent, "LampShade", new Vector3(-w * 0.4f, 2.3f, d * 0.3f), 0.3f, fabric);

            MakeBox(parent, "Rug", new Vector3(-w * 0.1f, 0.02f, 0f), new Vector3(4f, 0.04f, 3f), fabric);
        }

        private void BuildShopInterior(Transform parent, string suffix, float w, float d, float h,
            Material floor, Material wall, Material ceil, Material furn, Material metal, Material glass)
        {
            MakeBox(parent, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(w, 0.1f, d), floor);
            MakeBox(parent, "Ceiling", new Vector3(0f, h, 0f), new Vector3(w, 0.1f, d), ceil);
            MakeBox(parent, "WallBack", new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.2f), wall);
            MakeBox(parent, "WallLeft", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);
            MakeBox(parent, "WallRight", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);

            MakeBox(parent, "Counter", new Vector3(0f, 0.5f, -d * 0.25f), new Vector3(4f, 1f, 1f), furn);
            MakeBox(parent, "Register", new Vector3(0f, 1.1f, -d * 0.25f), new Vector3(0.5f, 0.3f, 0.4f), metal);

            for (int i = -1; i <= 1; i += 2)
            {
                MakeBox(parent, "Shelf_" + i, new Vector3(i * w * 0.3f, 1f, -d * 0.4f), new Vector3(1.5f, 2f, 0.4f), furn);
                for (int shelf = 0; shelf < 3; shelf++)
                    MakeBox(parent, "ShelfBoard_" + i + "_" + shelf, new Vector3(i * w * 0.3f, 0.5f + shelf * 0.7f, -d * 0.4f), new Vector3(1.4f, 0.05f, 0.35f), furn);
            }

            MakeBox(parent, "DisplayCase", new Vector3(w * 0.3f, 0.5f, d * 0.1f), new Vector3(2f, 1f, 0.6f), glass);
            MakeBox(parent, "Sign", new Vector3(0f, h - 0.5f, -d * 0.49f), new Vector3(3f, 0.6f, 0.1f), metal);

            MakeBox(parent, "Rug", new Vector3(0f, 0.02f, d * 0.15f), new Vector3(3f, 0.04f, 2.5f), furn);
        }

        private void BuildPublicInterior(Transform parent, string suffix, float w, float d, float h,
            Material floor, Material wall, Material ceil, Material furn, Material metal)
        {
            MakeBox(parent, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(w, 0.1f, d), floor);
            MakeBox(parent, "Ceiling", new Vector3(0f, h, 0f), new Vector3(w, 0.1f, d), ceil);
            MakeBox(parent, "WallBack", new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.2f), wall);
            MakeBox(parent, "WallLeft", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);
            MakeBox(parent, "WallRight", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);

            MakeBox(parent, "ReceptionDesk", new Vector3(0f, 0.5f, -d * 0.2f), new Vector3(4f, 1f, 1.2f), furn);
            MakeBox(parent, "Computer", new Vector3(0f, 1.05f, -d * 0.2f), new Vector3(0.5f, 0.4f, 0.3f), metal);
            MakeBox(parent, "Monitor", new Vector3(0f, 1.35f, -d * 0.2f - 0.1f), new Vector3(0.6f, 0.4f, 0.05f), metal);

            for (int i = 0; i < 3; i++)
            {
                float x = -w * 0.3f + i * w * 0.3f;
                MakeBox(parent, "Chair_" + i, new Vector3(x, 0.25f, d * 0.2f), new Vector3(0.5f, 0.5f, 0.5f), furn);
                MakeBox(parent, "ChairBack_" + i, new Vector3(x, 0.55f, d * 0.2f - 0.25f), new Vector3(0.5f, 0.6f, 0.1f), furn);
            }

            MakeBox(parent, "Cabinet1", new Vector3(-w * 0.4f, 0.75f, -d * 0.4f), new Vector3(1.5f, 1.5f, 0.5f), furn);
            MakeBox(parent, "Cabinet2", new Vector3(-w * 0.4f + 1.7f, 0.75f, -d * 0.4f), new Vector3(1.5f, 1.5f, 0.5f), furn);

            MakeBox(parent, "OfficeDesk", new Vector3(w * 0.3f, 0.4f, -d * 0.3f), new Vector3(2f, 0.8f, 1f), furn);
            MakeBox(parent, "OfficeChair", new Vector3(w * 0.3f, 0.25f, -d * 0.1f), new Vector3(0.5f, 0.5f, 0.5f), metal);

            MakeBox(parent, "Plant", new Vector3(w * 0.4f, 0.3f, d * 0.35f), new Vector3(0.4f, 0.6f, 0.4f), furn);
            MakeSphere(parent, "PlantLeaves", new Vector3(w * 0.4f, 0.85f, d * 0.35f), 0.5f, furn);
        }

        private void BuildChurchInterior(Transform parent, string suffix, float w, float d, float h,
            Material floor, Material wall, Material ceil, Material furn, Material fabric, Material metal, Material glass)
        {
            MakeBox(parent, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(w, 0.1f, d), floor);
            MakeBox(parent, "Ceiling", new Vector3(0f, h, 0f), new Vector3(w, 0.1f, d), ceil);
            MakeBox(parent, "WallBack", new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, 0.2f), wall);
            MakeBox(parent, "WallLeft", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);
            MakeBox(parent, "WallRight", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.2f, h, d), wall);

            for (int row = 0; row < 4; row++)
            {
                float z = d * 0.1f + row * 1.8f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * w * 0.18f;
                    MakeBox(parent, "Pew_" + row + "_" + side, new Vector3(x, 0.25f, z), new Vector3(2.5f, 0.5f, 0.5f), furn);
                    MakeBox(parent, "PewBack_" + row + "_" + side, new Vector3(x, 0.6f, z - 0.22f), new Vector3(2.5f, 0.5f, 0.1f), furn);
                }
            }

            MakeBox(parent, "Altar", new Vector3(0f, 0.5f, -d * 0.35f), new Vector3(3f, 1f, 1.5f), fabric);
            MakeBox(parent, "Pulpit", new Vector3(0f, 0.6f, -d * 0.15f), new Vector3(0.8f, 1.2f, 0.8f), furn);

            for (int side = -1; side <= 1; side += 2)
            {
                MakeBox(parent, "StainedGlass_" + side, new Vector3(side * w * 0.48f, h * 0.55f, 0f), new Vector3(0.1f, 2.5f, 1.5f), glass);
            }

            MakeBox(parent, "Candle1", new Vector3(-0.5f, 1.1f, -d * 0.35f), new Vector3(0.1f, 0.3f, 0.1f), metal);
            MakeBox(parent, "Candle2", new Vector3(0.5f, 1.1f, -d * 0.35f), new Vector3(0.1f, 0.3f, 0.1f), metal);

            MakeBox(parent, "Organ", new Vector3(0f, 1f, -d * 0.45f), new Vector3(2.5f, 2f, 0.8f), furn);
        }

        private static void MakeBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.identity;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void MakeSphere(Transform parent, string name, Vector3 pos, float radius, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * radius;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static Material CreateInteriorMat(string name, Color color)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = name;
            m.color = color;
            return m;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isInteriorExit ? new Color(0.2f, 0.8f, 0.2f, 0.5f) : new Color(0.8f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
    }
}

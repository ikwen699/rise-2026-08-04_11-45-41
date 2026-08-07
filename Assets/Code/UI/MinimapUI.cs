using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Rise.Core;
using Rise.Systems;

namespace Rise.UI
{
    public class MinimapUI : MonoBehaviour
    {
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private RawImage minimapImage;
        [SerializeField] private RectTransform compassNeedle;
        [SerializeField] private Transform player;

        private RenderTexture _rt;
        private float _mapSize = 160f;
        private float _pixelRadius;
        private RectTransform _minimapRect;
        private Canvas _canvas;
        private List<MarkerEntry> _markers = new List<MarkerEntry>();
        private RectTransform _legendPanel;
        private bool _markersVisible = true;

        private struct MarkerEntry
        {
            public RectTransform dot;
            public Vector3 worldPos;
            public bool isDynamic;
            public Transform followTarget;
        }

        public void Configure(Transform playerTransform)
        {
            if (playerTransform != null)
                player = playerTransform;
            else
            {
                GameObject p = GameObject.Find("Player");
                if (p != null) player = p.transform;
            }
            CreateMinimapCamera();
            CreateMinimapUI();
            CreateCompassUI();
            CreateLegend();
            CreateMarkers();
        }

        public void SetMarkersVisible(bool visible)
        {
            _markersVisible = visible;
            foreach (MarkerEntry m in _markers)
            {
                if (m.dot != null) m.dot.gameObject.SetActive(visible);
            }
            if (_legendPanel != null) _legendPanel.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            if (minimapCamera == null || player == null) return;
            minimapCamera.transform.position = new Vector3(player.position.x, 80f, player.position.z);

            if (compassNeedle != null)
            {
                float angle = -player.eulerAngles.y;
                compassNeedle.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            UpdateMarkerPositions();
        }

        private void CreateMinimapCamera()
        {
            if (minimapCamera != null) return;

            GameObject camGO = new GameObject("MinimapCamera");
            camGO.transform.SetParent(transform);
            minimapCamera = camGO.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = 50f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.2f, 0.3f, 0.2f, 1f);
            minimapCamera.cullingMask = ~0;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _rt = new RenderTexture(256, 256, 16);
            minimapCamera.targetTexture = _rt;
        }

        private void CreateMinimapUI()
        {
            if (minimapImage != null) return;

            GameObject canvasGO = new GameObject("MinimapCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.transform.SetParent(transform);

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = new Vector2(1f, 1f);
            canvasRect.anchorMax = new Vector2(1f, 1f);
            canvasRect.pivot = new Vector2(1f, 1f);

            GameObject imgGO = new GameObject("MinimapImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            minimapImage = imgGO.AddComponent<RawImage>();
            minimapImage.texture = _rt;

            RectTransform imgRect = imgGO.GetComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(1f, 1f);
            imgRect.anchorMax = new Vector2(1f, 1f);
            imgRect.pivot = new Vector2(1f, 1f);
            imgRect.anchoredPosition = new Vector2(-_mapSize * 0.5f - 16f, -_mapSize * 0.5f - 16f);
            imgRect.sizeDelta = new Vector2(_mapSize, _mapSize);
            _minimapRect = imgRect;
            _pixelRadius = _mapSize * 0.5f;

            GameObject borderGO = new GameObject("MinimapBorder");
            borderGO.transform.SetParent(imgGO.transform, false);
            Image borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(0f, 0f, 0f, 0.6f);
            RectTransform borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3f, -3f);
            borderRect.offsetMax = new Vector2(3f, 3f);
            borderGO.transform.SetAsFirstSibling();
        }

        private void CreateCompassUI()
        {
            if (compassNeedle != null) return;

            Transform parent = minimapImage.transform.parent;
            if (parent == null) return;

            GameObject compassGO = new GameObject("CompassNeedle");
            compassGO.transform.SetParent(parent, false);
            Image needleImg = compassGO.AddComponent<Image>();
            needleImg.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);

            RectTransform needleRect = compassGO.GetComponent<RectTransform>();
            needleRect.anchorMin = new Vector2(1f, 1f);
            needleRect.anchorMax = new Vector2(1f, 1f);
            needleRect.pivot = new Vector2(0.5f, 1f);
            needleRect.anchoredPosition = new Vector2(-_mapSize * 0.5f - 16f, -4f);
            needleRect.sizeDelta = new Vector2(4f, 20f);

            compassNeedle = needleRect;

            GameObject compassBg = new GameObject("CompassBg");
            compassBg.transform.SetParent(parent, false);
            Image bgImg = compassBg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.5f);
            RectTransform bgRect = compassBg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(1f, 1f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.pivot = new Vector2(0.5f, 1f);
            bgRect.anchoredPosition = new Vector2(-_mapSize * 0.5f - 16f, -2f);
            bgRect.sizeDelta = new Vector3(30f, 26f);
            bgRect.SetAsFirstSibling();

            GameObject labelN = new GameObject("LabelN");
            labelN.transform.SetParent(compassBg.transform, false);
            Text textN = labelN.AddComponent<Text>();
            textN.text = "N";
            textN.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textN.fontSize = 14;
            textN.alignment = TextAnchor.MiddleCenter;
            textN.color = Color.white;
            RectTransform textNRect = labelN.GetComponent<RectTransform>();
            textNRect.anchorMin = Vector2.zero;
            textNRect.anchorMax = Vector2.one;
            textNRect.offsetMin = Vector2.zero;
            textNRect.offsetMax = Vector2.zero;
        }

        private void CreateLegend()
        {
            if (_canvas == null) return;
            Transform canvasT = _canvas.transform;

            _legendPanel = new GameObject("LegendPanel").AddComponent<RectTransform>();
            _legendPanel.SetParent(canvasT, false);
            _legendPanel.anchorMin = new Vector2(1f, 1f);
            _legendPanel.anchorMax = new Vector2(1f, 1f);
            _legendPanel.pivot = new Vector2(1f, 0f);
            _legendPanel.anchoredPosition = new Vector2(-16f, -_mapSize - 24f);
            _legendPanel.sizeDelta = new Vector2(_mapSize, 60f);

            Image bg = _legendPanel.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);

            string[][] entries = new string[][]
            {
                new[] { "Work", "FFD700" },
                new[] { "Shop", "00CED1" },
                new[] { "House$", "00FF7F" },
                new[] { "Owned", "FFFFFF" },
                new[] { "Gas", "FF8C00" },
                new[] { "Bulletin", "9370DB" },
                new[] { "Maya", "FF69B4" },
                new[] { "Rival", "FF4444" },
            };

            float startX = 6f;
            float y = -4f;
            float colW = _mapSize / 4f;
            float rowH = 28f;

            for (int i = 0; i < entries.Length; i++)
            {
                int col = i % 4;
                int row = i / 4;
                float x = startX + col * colW;
                float ey = y - row * rowH;

                GameObject dotGO = new GameObject("Legend_" + entries[i][0]);
                dotGO.transform.SetParent(_legendPanel, false);
                Image dotImg = dotGO.AddComponent<Image>();
                ColorUtility.TryParseHtmlString("#" + entries[i][1], out Color c);
                dotImg.color = c;
                RectTransform dotRt = dotGO.GetComponent<RectTransform>();
                dotRt.anchorMin = new Vector2(0f, 1f);
                dotRt.anchorMax = new Vector2(0f, 1f);
                dotRt.pivot = new Vector2(0f, 0.5f);
                dotRt.anchoredPosition = new Vector2(x, ey);
                dotRt.sizeDelta = new Vector2(10f, 10f);

                GameObject lblGO = new GameObject("Label_" + entries[i][0]);
                lblGO.transform.SetParent(_legendPanel, false);
                Text lbl = lblGO.AddComponent<Text>();
                lbl.text = entries[i][0];
                lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lbl.fontSize = 11;
                lbl.color = Color.white;
                lbl.alignment = TextAnchor.MiddleLeft;
                RectTransform lblRt = lblGO.GetComponent<RectTransform>();
                lblRt.anchorMin = new Vector2(0f, 1f);
                lblRt.anchorMax = new Vector2(0f, 1f);
                lblRt.pivot = new Vector2(0f, 0.5f);
                lblRt.anchoredPosition = new Vector2(x + 14f, ey);
                lblRt.sizeDelta = new Vector2(colW - 18f, 16f);
            }
        }

        private void CreateMarkers()
        {
            if (_minimapRect == null || player == null) return;
            Transform canvasT = _minimapRect.parent;

            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            Material[] mats = new Material[0];

            WorkStation[] workStations = FindObjectsByType<WorkStation>();
            foreach (WorkStation ws in workStations)
            {
                if (ws == null) continue;
                bool unlocked = gm.Jobs != null && gm.Jobs.IsUnlocked(ws.Job);
                AddMarker(canvasT, ws.transform.position, unlocked ? new Color(1f, 0.84f, 0f) : new Color(0.5f, 0.5f, 0.5f), 6f);
            }

            ShopStand[] shops = FindObjectsByType<ShopStand>();
            foreach (ShopStand s in shops)
            {
                if (s == null) continue;
                AddMarker(canvasT, s.transform.position, new Color(0f, 0.81f, 0.82f), 6f);
            }

            ClothingStand[] clothing = FindObjectsByType<ClothingStand>();
            foreach (ClothingStand c in clothing)
            {
                if (c == null) continue;
                AddMarker(canvasT, c.transform.position, new Color(0f, 0.81f, 0.82f), 6f);
            }

            PropertyManager props = gm.Properties;
            if (props != null)
            {
                for (int i = 0; i < props.PropertyCount; i++)
                {
                    PropertyData pd = props.GetPropertyByIndex(i);
                    if (pd == null) continue;
                    Vector3 pos = GetBuildingPosition(pd.buildingName);
                    if (pos == Vector3.zero && pd.buildingName.StartsWith("House"))
                        continue;
                    if (pos == Vector3.zero) continue;
                    Color c = pd.owned ? Color.white : new Color(0f, 1f, 0.5f);
                    AddMarker(canvasT, pos, c, pd.owned ? 5f : 6f);
                }
            }

            GasStation gas = FindAnyObjectByType<GasStation>();
            if (gas != null)
                AddMarker(canvasT, gas.transform.position, new Color(1f, 0.55f, 0f), 7f);

            BulletinBoard bulletin = FindAnyObjectByType<BulletinBoard>();
            if (bulletin != null)
                AddMarker(canvasT, bulletin.transform.position, new Color(0.58f, 0.44f, 0.86f), 5f);

            Partner partner = FindAnyObjectByType<Partner>();
            if (partner != null)
                AddDynamicMarker(canvasT, partner.transform, new Color(1f, 0.41f, 0.71f), 6f);

            Rival rival = FindAnyObjectByType<Rival>();
            if (rival != null)
                AddDynamicMarker(canvasT, rival.transform, new Color(1f, 0.27f, 0.27f), 6f);
        }

        private void AddMarker(Transform canvasT, Vector3 worldPos, Color color, float size)
        {
            GameObject dotGO = new GameObject("Marker_" + _markers.Count);
            dotGO.transform.SetParent(canvasT, false);
            Image img = dotGO.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            RectTransform rt = dotGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);

            _markers.Add(new MarkerEntry { dot = rt, worldPos = worldPos, isDynamic = false });
        }

        private void AddDynamicMarker(Transform canvasT, Transform followTarget, Color color, float size)
        {
            GameObject dotGO = new GameObject("Marker_" + _markers.Count);
            dotGO.transform.SetParent(canvasT, false);
            Image img = dotGO.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            RectTransform rt = dotGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);

            _markers.Add(new MarkerEntry { dot = rt, worldPos = followTarget != null ? followTarget.position : Vector3.zero, isDynamic = true, followTarget = followTarget });
        }

        private void UpdateMarkerPositions()
        {
            if (player == null || _minimapRect == null) return;

            Vector3 playerPos = player.position;

            for (int i = 0; i < _markers.Count; i++)
            {
                MarkerEntry m = _markers[i];
                if (m.dot == null) continue;

                Vector3 worldPos = m.isDynamic && m.followTarget != null ? m.followTarget.position : m.worldPos;
                if (m.isDynamic) m.worldPos = worldPos;

                Vector3 offset = worldPos - playerPos;
                float halfView = 50f;
                float px = (offset.x / halfView) * _pixelRadius;
                float py = (offset.z / halfView) * _pixelRadius;

                float dist = Mathf.Sqrt(px * px + py * py);
                if (dist > _pixelRadius - 4f)
                {
                    float scale = (_pixelRadius - 4f) / dist;
                    px *= scale;
                    py *= scale;
                }

                m.dot.anchoredPosition = new Vector2(px, -py);
            }
        }

        private Vector3 GetBuildingPosition(string name)
        {
            switch (name)
            {
                case "House_01": return new Vector3(-40f, 0f, 10f);
                case "House_02": return new Vector3(-20f, 0f, 10f);
                case "House_03": return new Vector3(20f, 0f, 10f);
                case "House_04": return new Vector3(40f, 0f, 10f);
                case "House_05": return new Vector3(56f, 0f, 10f);
                case "House_06": return new Vector3(-40f, 0f, 52f);
                case "House_07": return new Vector3(-20f, 0f, 52f);
                case "House_08": return new Vector3(20f, 0f, 52f);
                case "House_09": return new Vector3(40f, 0f, 52f);
                case "House_10": return new Vector3(56f, 0f, 52f);
                case "House_11": return new Vector3(-40f, 0f, -55f);
                case "House_12": return new Vector3(-20f, 0f, -55f);
                case "House_13": return new Vector3(20f, 0f, -55f);
                case "House_14": return new Vector3(40f, 0f, -55f);
                case "House_15": return new Vector3(56f, 0f, -55f);
                case "Shop_01": return new Vector3(12f, 0f, 6f);
                case "Shop_02": return new Vector3(12f, 0f, -14f);
                case "TownHall": return new Vector3(0f, 0f, -30f);
                case "Market_01": return new Vector3(20f, 0f, 24f);
                case "Market_02": return new Vector3(-22f, 0f, 24f);
                case "Church": return new Vector3(0f, 0f, 42f);
                case "School": return new Vector3(-36f, 0f, 32f);
                case "Bakery": return new Vector3(-22f, 0f, 36f);
                case "Bank": return new Vector3(22f, 0f, 36f);
                case "Restaurant": return new Vector3(22f, 0f, -36f);
                case "PostOffice": return new Vector3(-22f, 0f, -36f);
                default: return Vector3.zero;
            }
        }
    }
}

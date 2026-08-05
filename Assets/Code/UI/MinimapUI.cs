using UnityEngine;
using UnityEngine.UI;

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
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
            bgRect.sizeDelta = new Vector2(30f, 26f);
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
    }
}

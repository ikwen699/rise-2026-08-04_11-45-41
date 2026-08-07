using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Rise.Systems
{
    public class WorkMinigame : MonoBehaviour
    {
        private RectTransform _barBg;
        private RectTransform _targetZone;
        private RectTransform _cursor;
        private Text _resultText;

        private float _cursorSpeed = 2f;
        private float _cursorDir = 1f;
        private float _barWidth = 300f;
        private float _zoneWidth = 60f;
        private float _timer;
        private float _resultTimer;
        private string _lastResult;
        private bool _active;
        private bool _resultShown;

        public float Multiplier { get; private set; } = 1f;
        public bool ResultReady { get; private set; }

        public void Create(Transform parent)
        {
            GameObject canvasGO = new GameObject("MinigameCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.transform.SetParent(parent, false);

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = new Vector2(0.5f, 0f);
            canvasRect.anchorMax = new Vector2(0.5f, 0f);
            canvasRect.pivot = new Vector2(0.5f, 0f);
            canvasRect.anchoredPosition = new Vector2(0f, 80f);
            canvasRect.sizeDelta = new Vector2(400f, 80f);

            _barBg = CreateRect("BarBg", canvasGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f), new Vector2(_barWidth, 30f));
            Image bgImg = _barBg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            float zoneX = Random.Range(-_barWidth * 0.35f, _barWidth * 0.35f);
            _targetZone = CreateRect("TargetZone", _barBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(zoneX, 0f), new Vector2(_zoneWidth, 30f));
            Image zoneImg = _targetZone.gameObject.AddComponent<Image>();
            zoneImg.color = new Color(0.2f, 0.8f, 0.2f, 0.7f);

            _cursor = CreateRect("Cursor", _barBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-_barWidth * 0.5f, 0f), new Vector2(4f, 35f));
            Image cursorImg = _cursor.gameObject.AddComponent<Image>();
            cursorImg.color = Color.white;

            _resultText = CreateText("Result", canvasGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(200f, 30f), 20, TextAnchor.MiddleCenter);

            _cursorSpeed = Random.Range(1.5f, 3f);
            _active = true;
            _resultShown = false;
            ResultReady = false;
            Multiplier = 1f;
        }

        private void Update()
        {
            if (!_active) return;

            if (!_resultShown)
            {
                _cursor.anchoredPosition += new Vector2(_cursorSpeed * _cursorDir * 60f * Time.deltaTime, 0f);

                float x = _cursor.anchoredPosition.x;
                if (x > _barWidth * 0.5f) { _cursorDir = -1f; _cursor.anchoredPosition = new Vector2(_barWidth * 0.5f, 0f); }
                if (x < -_barWidth * 0.5f) { _cursorDir = 1f; _cursor.anchoredPosition = new Vector2(-_barWidth * 0.5f, 0f); }

                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    CheckHit();
                }
            }
            else
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer <= 0f)
                {
                    ResultReady = true;
                    _active = false;
                }
            }
        }

        private void CheckHit()
        {
            float cursorX = _cursor.anchoredPosition.x;
            float zoneX = _targetZone.anchoredPosition.x;
            float dist = Mathf.Abs(cursorX - zoneX);

            if (dist < _zoneWidth * 0.3f)
            {
                Multiplier = 2f;
                _lastResult = "PERFECT! x2";
                _resultText.color = new Color(1f, 0.9f, 0.1f);
            }
            else if (dist < _zoneWidth * 0.6f)
            {
                Multiplier = 1.5f;
                _lastResult = "GOOD! x1.5";
                _resultText.color = new Color(0.3f, 0.9f, 0.3f);
            }
            else if (dist < _zoneWidth)
            {
                Multiplier = 1f;
                _lastResult = "OK x1";
                _resultText.color = Color.white;
            }
            else
            {
                Multiplier = 0.5f;
                _lastResult = "MISS x0.5";
                _resultText.color = new Color(0.9f, 0.3f, 0.3f);
            }

            _resultText.text = _lastResult;
            _resultShown = true;
            _resultTimer = 0.8f;
        }

        public void Destroy()
        {
            if (_barBg != null) Destroy(_barBg.gameObject.transform.parent.gameObject);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            return text;
        }
    }
}

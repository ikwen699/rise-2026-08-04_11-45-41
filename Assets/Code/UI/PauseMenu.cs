using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rise.SaveSystem;

namespace Rise.UI
{
    public class PauseMenu : MonoBehaviour
    {
        private GameManager _gameManager;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private bool _isPaused;
        private float _previousTimeScale = 1f;

        public bool IsPaused => _isPaused;

        public void Configure(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        private void Update()
        {
            if (_gameManager == null) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_isPaused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            if (_isPaused) return;
            _isPaused = true;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            PlayerController pc = _gameManager.Player != null
                ? _gameManager.Player.GetComponent<PlayerController>()
                : null;
            if (pc != null) pc.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            CreateUI();
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused = false;
            Time.timeScale = _previousTimeScale;

            PlayerController pc = _gameManager.Player != null
                ? _gameManager.Player.GetComponent<PlayerController>()
                : null;
            if (pc != null) pc.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            DestroyUI();
        }

        private void CreateUI()
        {
            if (_canvas != null) return;

            GameObject canvasGO = new GameObject("PauseMenuCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();

            GameObject overlay = new GameObject("Overlay");
            overlay.transform.SetParent(canvasGO.transform, false);
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            Image overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.6f);

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGO.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400f, 360f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

            CreateText("Title", panel.transform, "PAUSED", 48, new Vector2(0f, 130f));
            CreateButton("ResumeBtn", panel.transform, "Resume", new Vector2(0f, 60f), OnResumeClicked);
            CreateButton("SaveBtn", panel.transform, "Save Game", new Vector2(0f, 0f), OnSaveClicked);
            CreateButton("MenuBtn", panel.transform, "Main Menu", new Vector2(0f, -60f), OnMenuClicked);
            CreateButton("QuitBtn", panel.transform, "Quit", new Vector2(0f, -120f), OnQuitClicked);
        }

        private void DestroyUI()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
                _canvas = null;
                _canvasGroup = null;
            }
        }

        private void OnResumeClicked() => Resume();

        private void OnSaveClicked()
        {
            if (_gameManager != null)
            {
                _gameManager.SaveNow();
                _gameManager.Phone?.Push("Save", "Game saved successfully.");
            }
        }

        private void OnMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnQuitClicked()
        {
            if (_gameManager != null) _gameManager.SaveNow();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void CreateText(string name, Transform parent, string content, int fontSize, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(350f, 60f);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
        }

        private void CreateButton(string name, Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(280f, 45f);

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.25f, 0.55f, 0.85f, 0.9f);

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            GameObject textGO = new GameObject("Label");
            textGO.transform.SetParent(btnGO.transform, false);
            RectTransform textRt = textGO.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            Text text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
        }
    }
}

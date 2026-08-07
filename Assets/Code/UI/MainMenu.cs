using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rise.SaveSystem;

namespace Rise.UI
{
    public class MainMenu : MonoBehaviour
    {
        private Canvas _canvas;
        private Button _continueBtn;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            CreateUI();
        }

        private void CreateUI()
        {
            GameObject canvasGO = new GameObject("MainMenuCanvas");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(canvasGO.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.12f, 0.2f, 1f);

            CreateText("Title", canvasGO.transform, "RISE", 90, new Vector2(0f, 160f), new Color(1f, 0.85f, 0.3f));
            CreateText("Subtitle", canvasGO.transform, "A Life Simulation", 28, new Vector2(0f, 100f), new Color(0.7f, 0.7f, 0.75f));

            bool hasSave = GameSave.HasSave();
            _continueBtn = CreateButton("ContinueBtn", canvasGO.transform, "Continue", new Vector2(0f, 20f), OnContinue);
            _continueBtn.interactable = hasSave;

            CreateButton("NewGameBtn", canvasGO.transform, "New Game", new Vector2(0f, -40f), OnNewGame);
            CreateButton("QuitBtn", canvasGO.transform, "Quit", new Vector2(0f, -100f), OnQuit);

            if (!hasSave)
            {
                CreateText("NoSave", canvasGO.transform, "No save data found", 18, new Vector2(0f, -150f), new Color(0.5f, 0.5f, 0.55f));
            }
        }

        private void OnContinue()
        {
            SceneManager.LoadScene("OpenWorld");
        }

        private void OnNewGame()
        {
            GameSave.Delete();
            SceneManager.LoadScene("OpenWorld");
        }

        private void OnQuit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void CreateText(string name, Transform parent, string content, int fontSize, Vector2 pos, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(600f, 100f);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 50f);

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.75f, 0.9f);

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
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;

            return btn;
        }
    }
}

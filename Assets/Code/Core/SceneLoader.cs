using System;
using UnityEngine.SceneManagement;

namespace Rise.Core
{
    public static class SceneLoader
    {
        public const string OpenWorldSceneName = "OpenWorld";

        public static void LoadAdditive(string sceneName, Action onLoaded = null)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                onLoaded?.Invoke();
                return;
            }

            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).completed += _ => onLoaded?.Invoke();
        }

        public static void Unload(string sceneName, Action onUnloaded = null)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                onUnloaded?.Invoke();
                return;
            }

            SceneManager.UnloadSceneAsync(sceneName).completed += _ => onUnloaded?.Invoke();
        }

        public static void ReturnToOpenWorld()
        {
            if (!SceneManager.GetSceneByName(OpenWorldSceneName).isLoaded)
            {
                SceneManager.LoadSceneAsync(OpenWorldSceneName, LoadSceneMode.Additive);
            }

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != OpenWorldSceneName && scene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }
    }
}

using System;
using UnityEngine;

namespace Rise.Systems
{
    public enum WeatherState { Clear, Cloudy, Rainy, Stormy }

    public class WeatherSystem : MonoBehaviour
    {
        [SerializeField] private float minStateDuration = 4f;
        [SerializeField] private float maxStateDuration = 8f;

        public WeatherState CurrentWeather { get; private set; } = WeatherState.Clear;
        private float _timer;
        private float _transitionProgress;
        private WeatherState _previousWeather;
        private bool _isTransitioning;

        public event Action<WeatherState> OnWeatherChanged;

        private readonly Color _clearFog = new Color(0.5f, 0.6f, 0.7f);
        private readonly Color _cloudyFog = new Color(0.45f, 0.48f, 0.52f);
        private readonly Color _rainyFog = new Color(0.35f, 0.38f, 0.42f);
        private readonly Color _stormyFog = new Color(0.2f, 0.22f, 0.25f);

        public void Init()
        {
            CurrentWeather = WeatherState.Clear;
            _timer = UnityEngine.Random.Range(minStateDuration, maxStateDuration);
        }

        public void Tick(float gameDeltaTime)
        {
            _timer -= gameDeltaTime;
            if (_timer <= 0f)
            {
                CycleWeather();
                _timer = UnityEngine.Random.Range(minStateDuration, maxStateDuration);
            }

            if (_isTransitioning)
            {
                _transitionProgress += gameDeltaTime * 0.5f;
                if (_transitionProgress >= 1f)
                {
                    _transitionProgress = 1f;
                    _isTransitioning = false;
                }
            }
        }

        private void CycleWeather()
        {
            _previousWeather = CurrentWeather;
            WeatherState next;
            int roll = UnityEngine.Random.Range(0, 100);
            if (roll < 40) next = WeatherState.Clear;
            else if (roll < 70) next = WeatherState.Cloudy;
            else if (roll < 90) next = WeatherState.Rainy;
            else next = WeatherState.Stormy;

            if (next == _previousWeather)
                next = next == WeatherState.Stormy ? WeatherState.Cloudy : WeatherState.Clear;

            CurrentWeather = next;
            _isTransitioning = true;
            _transitionProgress = 0f;
            OnWeatherChanged?.Invoke(CurrentWeather);
        }

        public float GetFogDensity(float baseFog)
        {
            float target = CurrentWeather switch
            {
                WeatherState.Clear => 0.008f,
                WeatherState.Cloudy => 0.012f,
                WeatherState.Rainy => 0.02f,
                WeatherState.Stormy => 0.035f,
                _ => 0.008f
            };
            return Mathf.Lerp(baseFog, target, GetCurrentBlend());
        }

        public Color GetFogColor(float dayFactor)
        {
            Color baseColor = Color.Lerp(_clearFog, new Color(0.05f, 0.05f, 0.1f), 1f - dayFactor);
            Color weatherColor = CurrentWeather switch
            {
                WeatherState.Clear => _clearFog,
                WeatherState.Cloudy => _cloudyFog,
                WeatherState.Rainy => _rainyFog,
                WeatherState.Stormy => _stormyFog,
                _ => _clearFog
            };
            return Color.Lerp(baseColor, weatherColor, GetCurrentBlend());
        }

        public float GetAmbientIntensityModifier()
        {
            return CurrentWeather switch
            {
                WeatherState.Clear => 1f,
                WeatherState.Cloudy => 0.85f,
                WeatherState.Rainy => 0.7f,
                WeatherState.Stormy => 0.5f,
                _ => 1f
            };
        }

        public bool IsRaining() => CurrentWeather == WeatherState.Rainy || CurrentWeather == WeatherState.Stormy;
        public bool IsStormy() => CurrentWeather == WeatherState.Stormy;

        private float GetCurrentBlend()
        {
            if (!_isTransitioning) return 1f;
            return Mathf.SmoothStep(0f, 1f, _transitionProgress);
        }

        public string GetWeatherEmoji()
        {
            return CurrentWeather switch
            {
                WeatherState.Clear => "Clear",
                WeatherState.Cloudy => "Cloudy",
                WeatherState.Rainy => "Rainy",
                WeatherState.Stormy => "Stormy",
                _ => "Clear"
            };
        }
    }
}

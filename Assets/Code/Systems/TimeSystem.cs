using System;
using UnityEngine;

namespace Rise.Systems
{
    public class TimeSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float secondsPerGameHour = 10f;

        public event Action<int> OnDayChanged;
        public event Action OnTimeAdvanced;

        public int Day { get; private set; } = 1;
        public float HourOfDay { get; private set; } = 8f;

        public float SecondsPerGameHour => secondsPerGameHour;

        public int Hour => Mathf.FloorToInt(HourOfDay);
        public int Minute => Mathf.FloorToInt((HourOfDay - Mathf.Floor(HourOfDay)) * 60f);
        public string ClockText => string.Format("{0:00}:{1:00}", Hour, Minute);

        public void Configure(float secondsPerHour, int day, float hour)
        {
            secondsPerGameHour = secondsPerHour;
            Day = day;
            HourOfDay = hour;
        }

        public void Tick(float deltaTime)
        {
            HourOfDay += deltaTime / secondsPerGameHour;
            OnTimeAdvanced?.Invoke();

            if (HourOfDay >= 24f)
            {
                HourOfDay -= 24f;
                Day++;
                OnDayChanged?.Invoke(Day);
            }
        }
    }
}
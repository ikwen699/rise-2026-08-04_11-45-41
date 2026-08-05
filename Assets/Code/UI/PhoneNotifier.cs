using System.Collections.Generic;
using UnityEngine;

namespace Rise.UI
{
    public class PhoneNotifier : MonoBehaviour
    {
        private class Notification
        {
            public string title;
            public string message;
            public float timer;
            public float alpha = 1f;
        }

        private readonly List<Notification> _notifications = new List<Notification>();
        private readonly float _displayDuration = 6f;
        private readonly float _fadeDuration = 1.5f;
        private Rise.Core.GameManager _gameManager;

        public void Configure(Rise.Core.GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Push(string title, string message)
        {
            _notifications.Add(new Notification { title = title, message = message, timer = _displayDuration });
            if (_notifications.Count > 5)
                _notifications.RemoveAt(0);
        }

        private void Update()
        {
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                _notifications[i].timer -= Time.deltaTime;
                if (_notifications[i].timer < _fadeDuration)
                    _notifications[i].alpha = Mathf.Clamp01(_notifications[i].timer / _fadeDuration);
                if (_notifications[i].timer <= 0f)
                    _notifications.RemoveAt(i);
            }
        }

        public string GetNotificationText()
        {
            if (_notifications.Count == 0) return null;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                Notification n = _notifications[i];
                string prefix = n.alpha < 0.5f ? "<color=#ffffff80>" : "<color=#ffffffff>";
                sb.AppendLine(prefix + "<b>" + n.title + "</b></color>");
                sb.AppendLine(prefix + n.message + "</color>");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}

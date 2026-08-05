using System;
using UnityEngine;

namespace Rise.Systems
{
    public class QuestSystem : MonoBehaviour
    {
        private Core.GameManager _gameManager;
        private QuestDefinition[] _quests;
        private int _currentIndex;
        private int _currentProgress;
        private bool _initialized;
        private bool _almostNotified;

        public int CurrentIndex => _currentIndex;
        public int CurrentProgress => _currentProgress;
        public int QuestCount => _quests != null ? _quests.Length : 0;
        public bool AllComplete => _initialized && _currentIndex >= (_quests != null ? _quests.Length : 0);

        public event Action<int, string> OnQuestStarted;
        public event Action<int, string> OnQuestCompleted;
        public event Action OnAllQuestsComplete;

        public void Configure(Core.GameManager gameManager)
        {
            _gameManager = gameManager;
            if (!_initialized) InitQuests();
        }

        public void ApplySaved(int index, int progress)
        {
            _currentIndex = index;
            _currentProgress = progress;
            _initialized = true;
        }

        private void InitQuests()
        {
            _quests = new QuestDefinition[]
            {
                CreateQuest("Welcome to Town", "Work your first job and earn $50.", QuestObjective.EarnTotal, 50, 100, 5, "You've taken your first step!"),
                CreateQuest("Make a Name", "Reach reputation 30.", QuestObjective.ReachReputation, 30, 150, 8, "The town is starting to know you."),
                CreateQuest("Climb the Ladder", "Earn $500 total to unlock Manager.", QuestObjective.EarnTotal, 500, 200, 10, "You've unlocked the Manager job!"),
                CreateQuest("Renowned", "Reach reputation 50.", QuestObjective.ReachReputation, 50, 300, 12, "You're becoming respected."),
                CreateQuest("Settle Down", "Reach 40 affection with Maya.", QuestObjective.MarryPartner, 1, 250, 10, "Maya is falling for you!"),
                CreateQuest("Out-Your Rival", "Out-earn Marcus Blackwood.", QuestObjective.OutEarnRival, 1, 400, 15, "Marcus can't believe it."),
                CreateQuest("Get a Ride", "Own a luxury car (reach $3000 total earned).", QuestObjective.EarnTotal, 3000, 500, 12, "You're driving in style."),
                CreateQuest("A Successful Life", "Reach $10,000 total earned.", QuestObjective.EarnTotal, 10000, 1000, 20, "You've made it from nothing to everything. The town is yours."),
            };
            _initialized = true;
            if (_currentIndex < _quests.Length)
                OnQuestStarted?.Invoke(_currentIndex, _quests[_currentIndex].QuestName);
        }

        private QuestDefinition CreateQuest(string name, string desc, QuestObjective obj, int target, int money, int rep, string complete)
        {
            QuestDefinition q = ScriptableObject.CreateInstance<QuestDefinition>();
            q.Configure(name, desc, obj, target, money, rep, complete);
            return q;
        }

        private void Update()
        {
            if (_gameManager == null || !_initialized || AllComplete) return;
            if (_quests == null || _currentIndex >= _quests.Length) return;

            QuestDefinition current = _quests[_currentIndex];
            if (current == null) return;

            int progress = current.GetProgress(_gameManager);
            _currentProgress = progress;

            if (!_almostNotified && progress >= current.TargetValue * 0.8f && current.TargetValue > 1)
            {
                _almostNotified = true;
                _gameManager.Phone?.Push("Almost There!", current.QuestName + " — " + current.GetProgressText(_gameManager));
            }

            if (current.IsComplete(_gameManager))
            {
                CompleteQuest();
            }
        }

        private void CompleteQuest()
        {
            if (_quests == null || _currentIndex >= _quests.Length) return;
            QuestDefinition completed = _quests[_currentIndex];

            if (_gameManager.Wallet != null)
                _gameManager.Wallet.Add(completed.RewardMoney);
            if (_gameManager.Rep != null)
                _gameManager.Rep.AddReputation(completed.RewardReputation);

            _gameManager.Phone?.Push("Quest Complete!", completed.CompleteText + " (+$" + completed.RewardMoney + " +Rep " + completed.RewardReputation + ")");
            OnQuestCompleted?.Invoke(_currentIndex, completed.QuestName);

            _currentIndex++;
            _currentProgress = 0;
            _almostNotified = false;

            if (_currentIndex < _quests.Length)
            {
                OnQuestStarted?.Invoke(_currentIndex, _quests[_currentIndex].QuestName);
                _gameManager.Phone?.Push("New Quest", _quests[_currentIndex].QuestName + ": " + _quests[_currentIndex].Description);
            }
            else
            {
                OnAllQuestsComplete?.Invoke();
                _gameManager.Phone?.Push("All Quests Complete!", "You've beaten the game! Enjoy your success.");
            }
        }

        public QuestDefinition GetCurrentQuest()
        {
            if (_quests == null || _currentIndex >= _quests.Length) return null;
            return _quests[_currentIndex];
        }
    }
}

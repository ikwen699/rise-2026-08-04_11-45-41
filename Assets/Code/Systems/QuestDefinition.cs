using UnityEngine;

namespace Rise.Systems
{
    public enum QuestObjective { EarnTotal, ReachMoney, ReachReputation, TotalDays, MarryPartner, OutEarnRival }

    [CreateAssetMenu(fileName = "Quest", menuName = "Rise/Quests/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string questName = "Quest";
        [SerializeField, TextArea] private string description = "";
        [SerializeField] private QuestObjective objective = QuestObjective.EarnTotal;
        [SerializeField] private int targetValue = 100;
        [SerializeField] private int rewardMoney = 50;
        [SerializeField] private int rewardReputation = 5;
        [SerializeField] private string completeText = "Quest complete!";

        public string QuestName => questName;
        public string Description => description;
        public QuestObjective Objective => objective;
        public int TargetValue => targetValue;
        public int RewardMoney => rewardMoney;
        public int RewardReputation => rewardReputation;
        public string CompleteText => completeText;

        public void Configure(string name, string desc, QuestObjective obj, int target, int reward, int repReward, string complete)
        {
            questName = name;
            description = desc;
            objective = obj;
            targetValue = target;
            rewardMoney = reward;
            rewardReputation = repReward;
            completeText = complete;
        }

        public int GetProgress(Core.GameManager gm)
        {
            if (gm == null) return 0;
            switch (objective)
            {
                case QuestObjective.EarnTotal: return gm.Jobs != null ? gm.Jobs.TotalEarned : 0;
                case QuestObjective.ReachMoney: return gm.Wallet != null ? gm.Wallet.Money : 0;
                case QuestObjective.ReachReputation: return gm.Rep != null ? gm.Rep.Reputation : 0;
                case QuestObjective.TotalDays: return gm.Clock != null ? gm.Clock.Day : 0;
                case QuestObjective.MarryPartner: return (gm.Partner != null && gm.Partner.Married) ? 1 : 0;
                case QuestObjective.OutEarnRival: return (gm.Rival != null && gm.Rival.PlayerAhead) ? 1 : 0;
                default: return 0;
            }
        }

        public bool IsComplete(Core.GameManager gm)
        {
            return GetProgress(gm) >= targetValue;
        }

        public string GetProgressText(Core.GameManager gm)
        {
            int progress = GetProgress(gm);
            switch (objective)
            {
                case QuestObjective.EarnTotal: return "Earned $" + progress + " / $" + targetValue;
                case QuestObjective.ReachMoney: return "Have $" + progress + " / $" + targetValue;
                case QuestObjective.ReachReputation: return "Reputation " + progress + " / " + targetValue;
                case QuestObjective.TotalDays: return "Day " + progress + " / " + targetValue;
                case QuestObjective.MarryPartner: return progress >= 1 ? "Married!" : "Not yet married";
                case QuestObjective.OutEarnRival: return progress >= 1 ? "Out-earning rival!" : "Still behind rival";
                default: return "";
            }
        }
    }
}

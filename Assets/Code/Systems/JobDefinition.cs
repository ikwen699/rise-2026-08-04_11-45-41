using UnityEngine;

namespace Rise.Systems
{
    [CreateAssetMenu(fileName = "JobDefinition", menuName = "Rise/Jobs/Job Definition")]
    public class JobDefinition : ScriptableObject
    {
        [SerializeField] private string jobName = "Job";
        [SerializeField] private int hourlyPay = 10;
        [SerializeField] private int unlockEarned = 0;
        [SerializeField, TextArea] private string description = "";

        public string JobName => jobName;
        public int HourlyPay => hourlyPay;
        public int UnlockEarned => unlockEarned;
        public string Description => description;

        public void Configure(string name, int pay, int unlockAmount, string desc)
        {
            jobName = name;
            hourlyPay = pay;
            unlockEarned = unlockAmount;
            description = desc;
        }
    }
}
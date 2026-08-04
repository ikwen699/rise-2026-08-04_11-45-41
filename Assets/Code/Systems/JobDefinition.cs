using UnityEngine;

namespace Rise.Systems
{
    [CreateAssetMenu(fileName = "JobDefinition", menuName = "Rise/Jobs/Job Definition")]
    public class JobDefinition : ScriptableObject
    {
        [SerializeField] private string jobName = "Job";
        [SerializeField] private int hourlyPay = 10;
        [SerializeField, TextArea] private string description = "";

        public string JobName => jobName;
        public int HourlyPay => hourlyPay;
        public string Description => description;
    }
}
using System;
using UnityEngine;

namespace Rise.Systems
{
    public class JobSystem : MonoBehaviour
    {
        public bool IsWorking { get; private set; }
        public JobDefinition CurrentJob { get; private set; }
        public Core.WorkStation CurrentStation { get; private set; }
        public int TotalEarned { get; private set; }

        public event Action<JobDefinition, bool> OnWorkingChanged;

        public void AddEarned(int amount) => TotalEarned += amount;

        public void ApplyTotalEarned(int amount) => TotalEarned = amount;

        public bool IsUnlocked(JobDefinition job) =>
            job != null && (job.UnlockEarned <= 0 || TotalEarned >= job.UnlockEarned);

        public void StartWorking(JobDefinition job, Core.WorkStation station)
        {
            CurrentJob = job;
            CurrentStation = station;
            IsWorking = true;
            OnWorkingChanged?.Invoke(job, true);
        }

        public void StopWorking()
        {
            if (!IsWorking) return;
            IsWorking = false;
            CurrentJob = null;
            CurrentStation = null;
            OnWorkingChanged?.Invoke(null, false);
        }
    }
}
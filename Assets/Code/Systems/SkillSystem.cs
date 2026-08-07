using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rise.Systems
{
    public enum SkillName { Cooking, Driving, Charisma, Fitness, Business }

    public class SkillSystem : MonoBehaviour
    {
        private static readonly int[] XpThresholds = { 0, 100, 300, 600, 1000 };
        private const int MaxLevel = 5;

        private readonly Dictionary<SkillName, int> _xp = new();
        private readonly Dictionary<SkillName, int> _level = new();

        public event Action<SkillName, int, int> OnSkillChanged;

        public void Init()
        {
            foreach (SkillName s in Enum.GetValues(typeof(SkillName)))
            {
                _xp[s] = 0;
                _level[s] = 0;
            }
        }

        public int GetLevel(SkillName skill) => _level.TryGetValue(skill, out int lv) ? lv : 0;
        public int GetXP(SkillName skill) => _xp.TryGetValue(skill, out int x) ? x : 0;

        public int GetXpForNextLevel(SkillName skill)
        {
            int lv = GetLevel(skill);
            if (lv >= MaxLevel) return 0;
            return XpThresholds[lv + 1] - XpThresholds[lv];
        }

        public int GetXpIntoCurrentLevel(SkillName skill)
        {
            int lv = GetLevel(skill);
            if (lv >= MaxLevel) return 0;
            return _xp[skill] - XpThresholds[lv];
        }

        public float GetBonus(SkillName skill)
        {
            return GetLevel(skill) * 0.05f;
        }

        public void AddXP(SkillName skill, int amount)
        {
            if (amount <= 0) return;
            if (!_xp.ContainsKey(skill)) return;

            int oldLevel = _level[skill];
            _xp[skill] += amount;

            int newLevel = oldLevel;
            for (int i = oldLevel + 1; i <= MaxLevel; i++)
            {
                if (_xp[skill] >= XpThresholds[i])
                    newLevel = i;
                else
                    break;
            }

            if (newLevel != oldLevel)
            {
                _level[skill] = newLevel;
                OnSkillChanged?.Invoke(skill, oldLevel, newLevel);
                Debug.Log($"[Skill] {skill} leveled up to {newLevel}!");
            }
        }

        public string GetSkillInfo(SkillName skill)
        {
            int lv = GetLevel(skill);
            if (lv >= MaxLevel)
                return $"{skill} Lv.{lv} (MAX)";
            int into = GetXpIntoCurrentLevel(skill);
            int needed = GetXpForNextLevel(skill);
            return $"{skill} Lv.{lv} ({into}/{needed} XP)";
        }

        public float GetCookingBonus() => 1f + GetBonus(SkillName.Cooking);
        public float GetDrivingSpeedBonus() => 1f + GetBonus(SkillName.Driving) * 2f;
        public float GetDrivingFuelBonus() => 1f + GetBonus(SkillName.Driving);
        public float GetReputationBonus() => 1f + GetBonus(SkillName.Charisma) * 2f;
        public float GetMaxEnergyBonus() => GetLevel(SkillName.Fitness) * 10f;
        public float GetSprintBonus() => 1f + GetBonus(SkillName.Fitness) * 2f;
        public float GetJobPayBonus() => 1f + GetBonus(SkillName.Business) * 2f;
        public float GetPropertyIncomeBonus() => 1f + GetBonus(SkillName.Business);

        public void ApplySaved(int[] xpValues)
        {
            if (xpValues == null) return;
            SkillName[] skills = (SkillName[])Enum.GetValues(typeof(SkillName));
            for (int i = 0; i < skills.Length && i < xpValues.Length; i++)
            {
                _xp[skills[i]] = xpValues[i];
                int lv = 0;
                for (int l = 1; l <= MaxLevel; l++)
                {
                    if (xpValues[i] >= XpThresholds[l]) lv = l;
                    else break;
                }
                _level[skills[i]] = lv;
            }
        }

        public int[] GetXPArray()
        {
            SkillName[] skills = (SkillName[])Enum.GetValues(typeof(SkillName));
            int[] arr = new int[skills.Length];
            for (int i = 0; i < skills.Length; i++)
                arr[i] = _xp[skills[i]];
            return arr;
        }
    }
}

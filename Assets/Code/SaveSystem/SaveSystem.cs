using System;
using System.IO;
using UnityEngine;

namespace Rise.SaveSystem
{
    [Serializable]
    public class SaveData
    {
        public int money;
        public int day;
        public float hourOfDay;
        public float energy = 100f;
        public float hunger = 100f;
        public int food;
        public int giftFlowers;
        public int giftChocolate;
        public int giftRings;
        public float affection;
        public bool married;
        public int marriageDay;
        public bool childSpawned;
        public int totalEarned;
        public int outfitIndex;
        public int reputation;
        public int questIndex;
        public int questProgress;
        public float rivalMoney;
        public float rivalRep;
        public bool rivalDefeated;
        public string[] ownedProperties;
        public int[] skillXP;
    }

    public static class GameSave
    {
        private const string FileName = "rise_save.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public static bool TryLoad(out SaveData data)
        {
            data = null;
            if (!File.Exists(SavePath)) return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Rise: Failed to load save: " + e.Message);
                return false;
            }
        }

        public static void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
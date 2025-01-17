﻿using Kopio.JsonContracts;
using System.Collections.Generic;

namespace Zealot.Repository
{
    public static class LevelRepo
    {
        public static Dictionary<int, LevelJson> mIdMap;
        public static Dictionary<string, int> mNameMap;

        static LevelRepo()
        {
            mNameMap = new Dictionary<string, int>();
        }

        public static void Init(GameDBRepo gameData)
        {
            mIdMap = gameData.Level;
            mNameMap.Clear();

            foreach (KeyValuePair<int, LevelJson> entry in gameData.Level)
            {
                mNameMap.Add(entry.Value.unityscene, entry.Key);
            }
        }

        public static LevelJson GetInfoByName(string name)
        {
            if (mNameMap.ContainsKey(name))
                return mIdMap[mNameMap[name]];
            return null;
        }

        public static LevelJson GetInfoById(int id)
        {
            LevelJson levelJson;
            mIdMap.TryGetValue(id, out levelJson);
            return levelJson;
        }

        public static string GetSceneById(int id)
        {
            LevelJson levelJson;
            if (mIdMap.TryGetValue(id, out levelJson))
                return levelJson.unityscene;
            return "";
        }

        public static string GetLevelDataString()
        {
            string ret = "";
            foreach (var level in mIdMap)
            {
                ret += level.Value.excelname.ToString() + " " + level.Key + "|";
            }
            return ret;
        }

        /// <summary>
        /// Get unity scenes manifest for downloading of asset bundles.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllScenes()
        {
            List<string> result = new List<string>();
            foreach(KeyValuePair<int, LevelJson> kvp in mIdMap)
            {
                result.Add(kvp.Value.unityscene);
            }
            return result;
        }
    }
}


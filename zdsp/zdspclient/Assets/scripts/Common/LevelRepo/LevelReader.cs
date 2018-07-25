﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Zealot.Repository;
using Zealot.Common;

namespace Zealot.Entities
{
    public static class LevelReader
    {
        public static Dictionary<string, LevelInfo> levels;
        public static Dictionary<int, BossLocationData> mSpecialBossLocationDataMap;
        public static Dictionary<BossCategory, Dictionary<int, int>> mSpecialBossByCategory;

        static LevelReader()
        {
            levels = new Dictionary<string, LevelInfo>();
            mSpecialBossLocationDataMap = new Dictionary<int, BossLocationData>();
            mSpecialBossByCategory = new Dictionary<BossCategory, Dictionary<int, int>>();
            var boss_categories = Enum.GetValues(typeof(BossCategory));
            foreach (BossCategory entry in boss_categories)
                mSpecialBossByCategory.Add(entry, new Dictionary<int, int>());
        }

        public static void InitClient(Dictionary<string, string> levelAssets)
        {
            PortalInfos.Clear();
            SafeZoneInfo.Clear();
            NPCPosMap.Clear();
            levels.Clear();
            mSpecialBossLocationDataMap.Clear();
            foreach (var entry in mSpecialBossByCategory)
                entry.Value.Clear();
            foreach (KeyValuePair<string, string> kvp in levelAssets)
            {
                string levelname = kvp.Key;
                JsonSerializerSettings jsonSetting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                jsonSetting.Converters.Add(new Vector3Converter());
                try
                {
                    LevelInfo linfo = JsonConvert.DeserializeObject<LevelInfo>(kvp.Value, jsonSetting);
                    PortalInfos.AddPortal(levelname, linfo);
                    SafeZoneInfo.AddSafeZone(levelname, linfo);
                    NPCPosMap.AddNPCInfo(levelname, linfo);
                    AddSpecialBossLocationData(levelname, linfo);
                    levels[levelname] = linfo;
                }catch (Exception e)
                {
                    UnityEngine.Debug.Log("Level json out of data, please remove unused stuff. " + levelname);
                }
               
            }
            IsClientInited = true;
        }
        public static bool IsClientInited = false;
        
        public static void InitServer(string assemblypath)
        {
            string prefix = "../levels/";
            string curdir = System.IO.Path.Combine(assemblypath, prefix);
            string[] files = Directory.GetFiles(curdir, "*.json");
            foreach (string path in files)
            {
                string levelname = Path.GetFileNameWithoutExtension(path);
                using (StreamReader file = File.OpenText(path))
                {
                    string content = file.ReadToEnd();
                    content = content.Replace("Assembly-CSharp", "Common");
                    JsonSerializerSettings jsonSetting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                    jsonSetting.Converters.Add(new Vector3Converter());

                    try
                    {
                        LevelInfo linfo = JsonConvert.DeserializeObject<LevelInfo>(content, jsonSetting);
                        PortalInfos.AddPortal(levelname, linfo);
                        SafeZoneInfo.AddSafeZone(levelname, linfo);
                        AddSpecialBossLocationData(levelname, linfo);
                        levels[levelname] = linfo;
                    }
                    catch (Exception e)
                    {                      
                    }                   
                }
            }
        }

        public static void AddSpecialBossLocationData(string level, LevelInfo info)
        {
            Dictionary<int, ServerEntityJson> aSpecialBossSpawnerJson;
            if (info.mEntities.TryGetValue("SpecialBossSpawnerJson", out aSpecialBossSpawnerJson))
            {
                string entryName;
                foreach (SpecialBossSpawnerJson entry in aSpecialBossSpawnerJson.Values)
                {
                    entryName = entry.archetype;                    
                    if (string.IsNullOrEmpty(entryName))
                        continue;                   
                    var boss_info = SpecialBossRepo.GetInfoByName(entryName);                    
                    if (boss_info != null)
                    {
                        mSpecialBossByCategory[boss_info.category].Add(boss_info.sequence, boss_info.id);
                        mSpecialBossLocationDataMap.Add(boss_info.id, new BossLocationData(entry.position, level, boss_info.archetypeid));
                    }
                }
            }
        }

        public static Dictionary<int, int> GetSpecialBossByCategory(BossCategory category)
        {
            return mSpecialBossByCategory[category];
        }

        public static BossLocationData GetSpecialBossLocationData(int id)
        {
            if (mSpecialBossLocationDataMap.ContainsKey(id))
                return mSpecialBossLocationDataMap[id];
            return null;
        }

        public static LevelInfo GetLevel(string name)
        {
            if (levels.ContainsKey(name))
                return levels[name];
            return null;
        }
    }
}
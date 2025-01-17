﻿using System.Collections.Generic;
using Kopio.JsonContracts;

namespace Zealot.Repository
{
    public static class ScenesModelRepo
    {
        static Dictionary<string, ScenesModelJson> scenesModelMap;

        static ScenesModelRepo()
        {
            scenesModelMap = new Dictionary<string, ScenesModelJson>();
        }

        public static void Init(GameDBRepo gameData)
        {
            foreach (KeyValuePair<int, ScenesModelJson> entry in gameData.ScenesModel)
            {
                string modelName = entry.Value.archetype;
                if (!scenesModelMap.ContainsKey(modelName))
                {
                    scenesModelMap.Add(modelName, entry.Value);
                }
            }
        }

        public static ScenesModelJson GetScenesModelJson(string modelName)
        {
            if (scenesModelMap.ContainsKey(modelName))
            {
                return scenesModelMap[modelName];
            }

            return null;
        }

        public static string GetScenesModelPath(string archetype)
        {
            if (scenesModelMap.ContainsKey(archetype))
            {
                return scenesModelMap[archetype].modelprefabpath;
            }

            return null;
        }

        public static UnityEngine.Vector3 GetScenesModelScale(string modelName)
        {
            if (scenesModelMap.ContainsKey(modelName))
            {
                ScenesModelJson modelJson = GetScenesModelJson(modelName);
                return new UnityEngine.Vector3(modelJson.modelscalex, modelJson.modelscaley, modelJson.modelscalez);
            }

            return new UnityEngine.Vector3(1, 1, 1);
        }
    }
}
